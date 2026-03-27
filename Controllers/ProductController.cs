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

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            return supplier?.Id;
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
                .Where(p => p.SupplierId == supplierId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt);

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

            var model = new ProductViewModel
            {
                CategoryList = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName"),
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

            ModelState.Remove("CategoryList");
            ModelState.Remove("SupplierList");
            ModelState.Remove("GalleryImages");

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
                    Quantity = model.StockQuantity,
                    Unit = model.Unit,
                    IsAvailable = model.IsActive,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,

                    Barcode = model.Barcode,
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
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
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
                
                TempData["SuccessMessage"] = "Product created successfully!";
                
                if (Request.Query["addAnother"] == "true")
                {
                    return RedirectToAction(nameof(Create));
                }
                
                return RedirectToAction(nameof(MyProducts));
            }
            else
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                Console.WriteLine("❌ PRODUCT CREATE INVALID ModelState: " + errors);
                ModelState.AddModelError(string.Empty, "Validation errors: " + errors);
            }

            model.CategoryList = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName", model.CategoryId);
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

            ViewBag.Categories = new SelectList(_context.ProductCategories, "Id", "CategoryName", product.CategoryId);
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

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle Image Upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
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

                    product.IsAvailable = product.Quantity > 0;

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
            
            ViewBag.Categories = new SelectList(_context.ProductCategories, "Id", "CategoryName", product.CategoryId);
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

        // POST: Product/UpdateStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int stockChange, string action)
        {
            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null)
            {
                return Unauthorized();
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            
            if (product == null || product.IsDeleted)
            {
                return NotFound();
            }

            try
            {
                if (action == "increase")
                {
                    product.Quantity += stockChange;
                }
                else if (action == "decrease")
                {
                    product.Quantity -= stockChange;
                    if (product.Quantity < 0) product.Quantity = 0;
                }
                else if (action == "set")
                {
                    product.Quantity = stockChange;
                    if (product.Quantity < 0) product.Quantity = 0;
                }

                // Auto update availability
                product.IsAvailable = product.Quantity > 0;

                _context.Update(product);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Stock updated successfully. New quantity: {product.Quantity}";
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
