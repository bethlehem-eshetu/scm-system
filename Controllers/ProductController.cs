using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Supplier")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IInventoryService _inventoryService;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IInventoryService inventoryService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _inventoryService = inventoryService;
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

            // Get all category IDs the supplier is registered for
            var supplierCategoryIds = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.CategoryId)
                .ToListAsync();

            // Find all Parent Categories that match the supplier's selected categories or their parents
            var parentCategories = await _context.ProductCategories
                .Where(c => c.ParentCategoryId == null && 
                           (supplierCategoryIds.Contains(c.Id) || 
                            _context.ProductCategories.Any(sc => sc.ParentCategoryId == c.Id && supplierCategoryIds.Contains(sc.Id))))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var model = new ProductViewModel
            {
                CategoryList = new SelectList(parentCategories, "Id", "CategoryName"),
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

            // Server-side Sub-Category Validation
            if (model.SubCategoryId.HasValue)
            {
                var isSubCategoryValid = await _context.ProductCategories
                    .AnyAsync(c => c.Id == model.SubCategoryId && c.ParentCategoryId == model.CategoryId);
                if (!isSubCategoryValid)
                {
                    ModelState.AddModelError("SubCategoryId", "Invalid sub-category for selected category.");
                }
            }

            // Check for duplicate product name
            bool isDuplicateName = await _context.Products
                .AnyAsync(p => p.SupplierId == model.SupplierId && p.ProductName == model.ProductName);
            
            if (isDuplicateName)
            {
                ModelState.AddModelError("ProductName", "A product with this name already exists in your catalog.");
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

                // Handle Gallery Images Upload
                if (model.GalleryImages != null && model.GalleryImages.Any())
                {
                    string webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string galleryFolder = Path.Combine(webRoot, "images", "products", "gallery");
                    if (!Directory.Exists(galleryFolder)) Directory.CreateDirectory(galleryFolder);

                    int order = 0;
                    foreach (var galleryFile in model.GalleryImages.Take(10))
                    {
                        if (galleryFile.Length > 0)
                        {
                            string safeName = galleryFile.FileName.Replace(",", "_");
                            string uniqueName = Guid.NewGuid().ToString() + "_" + safeName;
                            using (var fs = new FileStream(Path.Combine(galleryFolder, uniqueName), FileMode.Create))
                            {
                                await galleryFile.CopyToAsync(fs);
                            }
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = "/images/products/gallery/" + uniqueName,
                                DisplayOrder = order++
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                
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

            // Get all category IDs the supplier is registered for
            var supplierCategoryIds = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.CategoryId)
                .ToListAsync();

            // Find all Parent Categories that match the supplier's selected categories or their parents
            var parentCategories = await _context.ProductCategories
                .Where(c => c.ParentCategoryId == null && 
                           (supplierCategoryIds.Contains(c.Id) || 
                            _context.ProductCategories.Any(sc => sc.ParentCategoryId == c.Id && supplierCategoryIds.Contains(sc.Id))))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            model.CategoryList = new SelectList(parentCategories, "Id", "CategoryName", model.CategoryId);
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

        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            var subCategories = await _context.ProductCategories
                .Where(c => c.ParentCategoryId == categoryId)
                .OrderBy(c => c.CategoryName)
                .Select(c => new { 
                    id = c.Id, 
                    categoryName = c.CategoryName 
                })
                .ToListAsync();
            return Json(subCategories);
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

            var product = await _context.Products
                .Include(p => p.GalleryImages.OrderBy(gi => gi.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (product == null || product.SupplierId != supplierId || product.IsDeleted)
            {
                return NotFound();
            }

            // Load gallery images for the view
            ViewBag.GalleryImages = product.GalleryImages.Select(gi => new GalleryImageViewModel
            {
                Id = gi.Id,
                ImageUrl = gi.ImageUrl,
                DisplayOrder = gi.DisplayOrder
            }).ToList();

            // Get all category IDs the supplier is registered for
            var supplierCategoryIds = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.CategoryId)
                .ToListAsync();

            // Find all Parent Categories
            var parentCategories = await _context.ProductCategories
                .Where(c => c.ParentCategoryId == null && 
                           (supplierCategoryIds.Contains(c.Id) || 
                            _context.ProductCategories.Any(sc => sc.ParentCategoryId == c.Id && supplierCategoryIds.Contains(sc.Id))))
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(parentCategories, "Id", "CategoryName", product.CategoryId);
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile imageFile)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null)
            {
                return Unauthorized();
            }

            // Fetch current state from DB
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            if (product == null || product.IsDeleted)
            {
                return NotFound();
            }

            ModelState.Remove("SupplierId");
            ModelState.Remove("Supplier");
            ModelState.Remove("Category");
            ModelState.Remove("Inventories");
            ModelState.Remove("AttributeValues");
            ModelState.Remove("PurchaseOrderItems");
            ModelState.Remove("OrderItems");
            ModelState.Remove("imageFile");
            
            // To ensure save succeeds when missing non-critical binding fields
            var keysToRemove = ModelState.Keys.Where(k => ModelState[k].Errors.Count > 0).ToList();
            foreach (var key in keysToRemove)
            {
                if (key != "ProductName" && key != "BasePrice" && key != "Unit" && key != "CategoryId")
                {
                    ModelState.Remove(key);
                }
            }

            // Server-side Category Check
            var isCategoryValid = await _context.SupplierCategories
                .AnyAsync(sc => sc.SupplierId == supplierId && sc.CategoryId == model.CategoryId);

            if (!isCategoryValid)
            {
                ModelState.AddModelError("CategoryId", "You are not registered to sell products in this category.");
            }

            // Server-side Sub-Category Validation
            if (model.SubCategoryId.HasValue)
            {
                var isSubCategoryValid = await _context.ProductCategories
                    .AnyAsync(c => c.Id == model.SubCategoryId && c.ParentCategoryId == model.CategoryId);
                if (!isSubCategoryValid)
                {
                    ModelState.AddModelError("SubCategoryId", "Invalid sub-category for selected category.");
                }
            }

            // Check for duplicate product name
            bool isDuplicateName = await _context.Products
                .AnyAsync(p => p.SupplierId == supplierId && p.ProductName == model.ProductName && p.Id != id);
            
            if (isDuplicateName)
            {
                ModelState.AddModelError("ProductName", "Another product with this name already exists in your catalog.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update main fields
                    product.ProductName = model.ProductName;
                    product.CategoryId = model.CategoryId;
                    product.BasePrice = model.BasePrice;
                    product.Description = model.Description;
                    product.Unit = model.Unit;
                    product.SKU = model.SKU;
                    product.IsAvailable = model.IsAvailable;
                    
                    // Advanced Fields
                    product.SubCategoryId = model.SubCategoryId;
                    product.Brand = model.Brand;
                    product.ShortDescription = model.ShortDescription;
                    product.Tags = model.Tags;
                    product.IsFeatured = model.IsFeatured;
                    product.CostPrice = model.CostPrice;
                    product.WholesalePrice = model.WholesalePrice;
                    product.DiscountPercentage = model.DiscountPercentage;
                    product.TaxRate = model.TaxRate;
                    product.MinimumOrderQuantity = model.MinimumOrderQuantity;
                    product.MaximumStockLevel = model.MaximumStockLevel;
                    product.ReorderLevel = model.ReorderLevel;
                    product.ReorderQuantity = model.ReorderQuantity;
                    product.LeadTimeDays = model.LeadTimeDays;
                    product.ShippingWeight = model.ShippingWeight;
                    product.ShippingLength = model.ShippingLength;
                    product.ShippingWidth = model.ShippingWidth;
                    product.ShippingHeight = model.ShippingHeight;
                    product.HSCode = model.HSCode;
                    product.IsHazardous = model.IsHazardous;
                    product.MetaTitle = model.MetaTitle;
                    product.MetaDescription = model.MetaDescription;
                    product.MetaKeywords = model.MetaKeywords;
                    product.Slug = model.Slug ?? model.ProductName?.ToLower().Replace(" ", "-");

                    // Handle Image Upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string webRootPath = _webHostEnvironment.WebRootPath;
                        if (string.IsNullOrEmpty(webRootPath))
                        {
                            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        }

                        string uploadsFolder = Path.Combine(webRootPath, "images", "products");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(webRootPath, product.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                        }

                        product.ImageUrl = "/images/products/" + uniqueFileName;
                    }

                    // Auto-update availability based on stock
                    var totalStock = await _context.Inventories
                        .Where(inv => inv.ProductId == product.Id)
                        .SumAsync(inv => inv.QuantityOnHand - inv.QuantityReserved);

                    product.IsAvailable = totalStock > 0;

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    // Handle new gallery images in Edit
                    var galleryFiles = HttpContext.Request.Form.Files.GetFiles("GalleryImages");
                    if (galleryFiles != null && galleryFiles.Any())
                    {
                        string webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        string galleryFolder = Path.Combine(webRoot, "images", "products", "gallery");
                        if (!Directory.Exists(galleryFolder)) Directory.CreateDirectory(galleryFolder);

                        int maxOrder = await _context.ProductImages.Where(pi => pi.ProductId == product.Id).Select(pi => pi.DisplayOrder).DefaultIfEmpty(0).MaxAsync();
                        foreach (var gf in galleryFiles.Take(10))
                        {
                            if (gf.Length > 0)
                            {
                                string uniqueName = Guid.NewGuid().ToString() + "_" + gf.FileName.Replace(",", "_");
                                using (var fs = new FileStream(Path.Combine(galleryFolder, uniqueName), FileMode.Create))
                                {
                                    await gf.CopyToAsync(fs);
                                }
                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImageUrl = "/images/products/gallery/" + uniqueName,
                                    DisplayOrder = ++maxOrder
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(MyProducts));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error updating product: " + ex.Message;
                }
            }
            
            var supplierCategories = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.Category)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = new SelectList(supplierCategories, "Id", "CategoryName", model.CategoryId);
            return View(model);
        }

        // POST: Product/DeleteGalleryImage (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteGalleryImage(int imageId)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            var image = await _context.ProductImages
                .Include(pi => pi.Product)
                .FirstOrDefaultAsync(pi => pi.Id == imageId);

            if (image == null || image.Product.SupplierId != supplierId)
                return NotFound();

            // Delete the physical file
            string webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, image.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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
                .Include(p => p.GalleryImages.OrderBy(gi => gi.DisplayOrder))
                .Include(p => p.AttributeValues)
                    .ThenInclude(av => av.AttributeDefinition)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(int id, int warehouseId, int quantity)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            if (product == null) return NotFound();

            var adjustment = new InventoryAdjustment
            {
                ProductId = id,
                WarehouseId = warehouseId,
                QuantityChange = quantity,
                AdjustmentType = "Initial Load",
                Reason = "Supplier added initial stock through product management.",
                DocumentReference = $"INIT-{id}-{DateTime.Now:yyyyMMdd}",
                PerformedById = userId,
                CreatedAt = DateTime.Now
            };

            try
            {
                await _inventoryService.AdjustInventoryAsync(adjustment);
                
                // Consistency check: Recalculate if needed
                await _inventoryService.RecalculateInventoryAsync(warehouseId, id);

                TempData["SuccessMessage"] = $"Stock added successfully! quantity: {quantity}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding stock: " + ex.Message;
            }

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
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var adjustment = new InventoryAdjustment
                {
                    ProductId = id,
                    WarehouseId = warehouseId,
                    QuantityChange = action == "increase" ? stockChange : (action == "decrease" ? -stockChange : 0),
                    AdjustmentType = "Correction",
                    Reason = $"Supplier manual stock update: {action} by {stockChange}",
                    DocumentReference = $"MAN-{id}-{DateTime.Now:yyyyMMdd}",
                    CreatedAt = DateTime.Now
                };

                // If 'set', we need to calculate the delta
                if (action == "set")
                {
                    int currentQty = inventory?.QuantityOnHand ?? 0;
                    adjustment.QuantityChange = stockChange - currentQty;
                }

                await _inventoryService.AdjustInventoryAsync(adjustment);
                
                // Force sync aggregate availability
                await _inventoryService.RecalculateInventoryAsync(warehouseId, id);
                
                TempData["SuccessMessage"] = $"Stock updated successfully. Action: {action}";
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
