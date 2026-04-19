using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Supplier")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Helper method to get current SupplierId
        private async Task<int?> GetCurrentSupplierIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return null;
            }

            var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            
            // Centralized Fayda check: Only verified suppliers can access product management
            if (supplier == null || !supplier.User.IsFaydaVerified)
            {
                return null;
            }

            return supplier.Id;
        }

        // GET: Product/MyProducts
        public async Task<IActionResult> MyProducts(int? pageNumber)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null)
            {
                return Unauthorized();
            }

            // Simple pagination (assuming 10 items per page)
            int pageSize = 10;
            int pageIndex = pageNumber ?? 1;

            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories) // Added to support aggregate stock calculation in view
                .Where(p => p.SupplierId == supplierId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt);

            // Fetch active warehouses for the stock update modal
            var warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplierId && w.Status == SCM_System.Models.Enums.WarehouseStatus.Active)
                .ToListAsync();
            ViewBag.Warehouses = new SelectList(warehouses, "Id", "Name");

            var totalItems = await productsQuery.CountAsync();
            var products = await productsQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = pageIndex;

            return View(products);
        }

        // GET: Product/Create
        public async Task<IActionResult> Create()
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            var supplier = await _context.Suppliers.FindAsync(supplierId);

            var supplierCategories = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.Category)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var model = new ProductViewModel
            {
                CategoryList = new SelectList(supplierCategories, "Id", "CategoryName"),
                SupplierList = supplier != null ? new SelectList(new[] { supplier }, "Id", "CompanyName", supplierId) : null,
                SupplierId = supplierId ?? 0
            };
            return View(model);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            model.SupplierId = supplierId.Value;

            ModelState.Remove("Supplier");
            ModelState.Remove("Category");
            ModelState.Remove("Inventory");
            ModelState.Remove("AttributeValues");
            ModelState.Remove("PurchaseOrderItems");
            ModelState.Remove("OrderItems");
            ModelState.Remove("imageFile");
            ModelState.Remove("CategoryList");
            ModelState.Remove("SupplierList");
            ModelState.Remove("GalleryImages");

            // Server-side Category Check
            var isCategoryValid = await _context.SupplierCategories
                .AnyAsync(sc => sc.SupplierId == supplierId && sc.CategoryId == model.CategoryId);

            if (!isCategoryValid)
            {
                ModelState.AddModelError("CategoryId", "You are not registered to sell products in this category.");
            }

            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    SupplierId = model.SupplierId,
                    CategoryId = model.CategoryId,
                    ProductName = model.ProductName,
                    BasePrice = model.BasePrice,
                    Description = model.Description,
                    SKU = model.SKU,
                    Unit = model.Unit,
                    IsAvailable = model.IsActive,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,

                    SubCategoryId = model.SubCategoryId,
                    Brand = model.Brand,
                    ShortDescription = model.ShortDescription,
                    Tags = model.Tags,
                    IsFeatured = model.IsFeatured,
                    CostPrice = model.CostPrice,
                    WholesalePrice = model.WholesalePrice,
                    DiscountPercentage = model.DiscountPercentage,
                    TaxRate = model.TaxRate,
                    MinimumOrderQuantity = model.MinimumOrderQuantity,
                    MaximumStockLevel = model.MaximumStockLevel,
                    ReorderLevel = model.ReorderLevel,
                    ReorderQuantity = model.ReorderQuantity,
                    LeadTimeDays = model.LeadTimeDays,
                    ShippingWeight = model.ShippingWeight,
                    ShippingLength = model.ShippingLength,
                    ShippingWidth = model.ShippingWidth,
                    ShippingHeight = model.ShippingHeight,
                    HSCode = model.HSCode,
                    IsHazardous = model.IsHazardous,
                    MetaTitle = model.MetaTitle,
                    MetaDescription = model.MetaDescription,
                    MetaKeywords = model.MetaKeywords,
                    Slug = model.Slug ?? model.ProductName?.ToLower().Replace(" ", "-")
                };

                // Handle Main Image Upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string webRootPath = _webHostEnvironment.WebRootPath;
                    if (string.IsNullOrEmpty(webRootPath))
                    {
                        webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    }

                    string uploadsFolder = Path.Combine(webRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    string safeFileName = model.ImageFile.FileName.Replace(",", "_");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + safeFileName;
                    using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                
                // Handle dynamic attribute saving
                if (model.DynamicAttributes != null && model.DynamicAttributes.Any())
                {
                    foreach (var attr in model.DynamicAttributes)
                    {
                        if (!string.IsNullOrEmpty(attr.Value))
                        {
                            var pav = new ProductAttributeValue
                            {
                                ProductId = product.Id,
                                AttributeId = attr.Key,
                                Value = attr.Value
                            };
                            _context.ProductAttributeValues.Add(pav);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Handle brand new crowdsourced attributes created by the supplier
                if (model.NewAttributes != null && model.NewAttributes.Any())
                {
                    foreach (var newAttr in model.NewAttributes)
                    {
                        if (string.IsNullOrWhiteSpace(newAttr.Name) || string.IsNullOrWhiteSpace(newAttr.Value)) 
                            continue;

                        // Check if another supplier already created an attribute with this name for this category
                        var existingDef = await _context.ProductAttributeDefinitions
                            .FirstOrDefaultAsync(a => a.CategoryId == model.CategoryId && 
                                                      a.AttributeName.ToLower() == newAttr.Name.ToLower());

                        int definitionId;

                        if (existingDef != null)
                        {
                            definitionId = existingDef.Id;
                        }
                        else
                        {
                            // Create a new globally available Category Attribute Definition
                            var newDef = new ProductAttributeDefinition
                            {
                                CategoryId = model.CategoryId,
                                AttributeName = newAttr.Name.Trim(),
                                DataType = decimal.TryParse(newAttr.Value, out _) ? "Number" : "Text",
                                Unit = newAttr.Unit?.Trim(),
                                IsRequired = false
                            };
                            
                            _context.ProductAttributeDefinitions.Add(newDef);
                            await _context.SaveChangesAsync();
                            definitionId = newDef.Id;
                        }

                        // Now map the value to this product using the new or existing definition
                        var pav = new ProductAttributeValue
                        {
                            ProductId = product.Id,
                            AttributeId = definitionId,
                            Value = newAttr.Value.Trim()
                        };
                        _context.ProductAttributeValues.Add(pav);
                    }
                    await _context.SaveChangesAsync();
                }
                
                TempData["SuccessMessage"] = "Product created successfully! Now assign initial stock to a warehouse.";
                return RedirectToAction(nameof(AddStock), new { id = product.Id });
            }
            else
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                Console.WriteLine("❌ PRODUCT CREATE INVALID ModelState: " + errors);
                ModelState.AddModelError(string.Empty, "Validation errors: " + errors);
            }

            var supplierCategories = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.Category)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            model.CategoryList = new SelectList(supplierCategories, "Id", "CategoryName", model.CategoryId);
            var supplierObj = await _context.Suppliers.FindAsync(supplierId);
            model.SupplierList = supplierObj != null ? new SelectList(new[] { supplierObj }, "Id", "CompanyName", supplierId) : null;
            
            return View(model);
        }

        [HttpGet]
        public IActionResult GenerateSku(string productName, int categoryId)
        {
            if (string.IsNullOrEmpty(productName)) return Json(new { sku = "" });
            var prefix = productName.Length >= 3 ? productName.Substring(0, 3).ToUpper() : productName.ToUpper();
            var sku = $"{prefix}-{categoryId}-{DateTime.Now.Ticks.ToString().Substring(10, 4)}";
            return Json(new { sku });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryAttributes(int categoryId)
        {
            var attributes = await _context.ProductAttributeDefinitions
                .Where(a => a.CategoryId == categoryId)
                .OrderBy(a => a.AttributeName)
                .Select(a => new {
                    id = a.Id,
                    attributeName = a.AttributeName,
                    dataType = a.DataType,
                    unit = a.Unit,
                    isRequired = a.IsRequired
                })
                .ToListAsync();
            return Json(attributes);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null)
            {
                return Unauthorized();
            }

            var product = await _context.Products.FindAsync(id);
            
            if (product == null || product.SupplierId != supplierId || product.IsDeleted)
            {
                return NotFound();
            }

            var supplierCategories = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.Category)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(supplierCategories, "Id", "CategoryName", product.CategoryId);
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductName,CategoryId,BasePrice,Description,Unit,Quantity,SKU,ImageUrl,CreatedAt")] Product product, IFormFile imageFile)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null)
            {
                return Unauthorized();
            }

            // Ensure the user owns this product
            var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            if (existingProduct == null || existingProduct.IsDeleted)
            {
                return NotFound();
            }

            product.SupplierId = supplierId.Value;

            ModelState.Remove("SupplierId");
            ModelState.Remove("Supplier");
            ModelState.Remove("Category");
            ModelState.Remove("Inventory");
            ModelState.Remove("AttributeValues");
            ModelState.Remove("PurchaseOrderItems");
            ModelState.Remove("OrderItems");

            // Server-side Category Check
            var isCategoryValid = await _context.SupplierCategories
                .AnyAsync(sc => sc.SupplierId == supplierId && sc.CategoryId == product.CategoryId);

            if (!isCategoryValid)
            {
                ModelState.AddModelError("CategoryId", "You are not registered to sell products in this category.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle Image Upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string webRootPath = _webHostEnvironment.WebRootPath;
                        if (string.IsNullOrEmpty(webRootPath))
                        {
                            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        }

                        string uploadsFolder = Path.Combine(webRootPath, "images", "products");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        product.ImageUrl = "/images/products/" + uniqueFileName;
                        
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                    }
                    else
                    {
                        // Keep existing image
                        product.ImageUrl = existingProduct.ImageUrl;
                    }

                    // Availability is now determined by sum of all inventories
                    var totalStock = await _context.Inventories
                        .Where(inv => inv.ProductId == product.Id)
                        .SumAsync(inv => inv.QuantityOnHand - inv.QuantityReserved);

                    product.IsAvailable = totalStock > 0;

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Product updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error updating product: " + ex.Message;
                    ViewBag.Categories = new SelectList(_context.ProductCategories, "Id", "CategoryName", product.CategoryId);
                    return View(product);
                }
                return RedirectToAction(nameof(MyProducts));
            }
            
            var supplierCategories = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.Category)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(supplierCategories, "Id", "CategoryName", product.CategoryId);
            return View(product);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null)
            {
                return Unauthorized();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id && m.SupplierId == supplierId && !m.IsDeleted);
                
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/AddStock/5
        public async Task<IActionResult> AddStock(int id)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);

            if (product == null) return NotFound();

            var warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplierId && w.Status == SCM_System.Models.Enums.WarehouseStatus.Active)
                .ToListAsync();

            ViewBag.Warehouses = new SelectList(warehouses, "Id", "Name");
            ViewBag.ProductName = product.ProductName;
            ViewBag.Unit = product.Unit;

            return View();
        }

        // POST: Product/AddStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(int id, int warehouseId, int quantity)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            if (product == null) return NotFound();

            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == id && i.WarehouseId == warehouseId);

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    ProductId = id,
                    WarehouseId = warehouseId,
                    QuantityOnHand = quantity,
                    QuantityReserved = 0,
                    WarehouseLocation = "Main Section", // Default or user input
                    LastUpdated = DateTime.Now
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                inventory.QuantityOnHand += quantity;
                inventory.LastUpdated = DateTime.Now;
                _context.Update(inventory);
            }

            await _context.SaveChangesAsync();

            // Refresh product availability
            var totalStock = await _context.Inventories.Where(i => i.ProductId == id).SumAsync(i => i.QuantityOnHand - i.QuantityReserved);
            product.IsAvailable = totalStock > 0;
            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Stock added successfully! Total available: {totalStock}";
            return RedirectToAction(nameof(MyProducts));
        }

        // POST: Product/UpdateStock (Refactored for Inventory)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int warehouseId, int stockChange, string action)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            if (product == null || product.IsDeleted) return NotFound();

            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == id && i.WarehouseId == warehouseId);

            try
            {
                if (inventory == null)
                {
                    // If inventory record doesn't exist, only allow increase or set
                    if (action == "increase" || action == "set")
                    {
                        inventory = new Inventory
                        {
                            ProductId = id,
                            WarehouseId = warehouseId,
                            QuantityOnHand = 0,
                            QuantityReserved = 0,
                            WarehouseLocation = "Main Section",
                            LastUpdated = DateTime.Now
                        };
                        _context.Inventories.Add(inventory);
                        
                        // Apply stock change to the newly created record (starting from 0)
                        inventory.QuantityOnHand = stockChange;
                    }
                    else
                    {
                        // For 'decrease', if it doesn't exist, we reject it
                        TempData["ErrorMessage"] = "Cannot decrease stock: No inventory record exists for this warehouse.";
                        return RedirectToAction(nameof(MyProducts));
                    }
                }
                else
                {
                    if (action == "increase")
                    {
                        inventory.QuantityOnHand += stockChange;
                    }
                    else if (action == "decrease")
                    {
                        if (inventory.QuantityOnHand < stockChange)
                        {
                            TempData["ErrorMessage"] = "Cannot decrease stock below zero.";
                            return RedirectToAction(nameof(MyProducts));
                        }
                        inventory.QuantityOnHand -= stockChange;
                    }
                    else if (action == "set")
                    {
                        inventory.QuantityOnHand = stockChange;
                    }

                    if (inventory.QuantityOnHand < 0) inventory.QuantityOnHand = 0;
                    inventory.LastUpdated = DateTime.Now;
                    _context.Update(inventory);
                }

                await _context.SaveChangesAsync();

                // Auto update availability
                var totalStock = await _context.Inventories.Where(inv => inv.ProductId == id).SumAsync(inv => inv.QuantityOnHand - inv.QuantityReserved);
                product.IsAvailable = totalStock > 0;
                _context.Update(product);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Stock updated successfully. New warehouse quantity: {inventory.QuantityOnHand}";
            }
            catch (Exception ex)
            {
                 TempData["ErrorMessage"] = "Error updating stock: " + ex.Message;
            }

            return RedirectToAction(nameof(MyProducts));
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null)
            {
                return Unauthorized();
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            if (product != null)
            {
                // Soft delete
                product.IsDeleted = true;
                product.IsAvailable = false;
                _context.Update(product);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            
            return RedirectToAction(nameof(MyProducts));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
