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
                
                // Handle new crowdsourced attributes generated by supplier
                if (model.NewAttributes != null && model.NewAttributes.Any())
                {
                    foreach (var newAttr in model.NewAttributes)
                    {
                        if (string.IsNullOrWhiteSpace(newAttr.Name) || string.IsNullOrWhiteSpace(newAttr.Value)) continue;
                        
                        var existingDef = await _context.ProductAttributeDefinitions
                            .FirstOrDefaultAsync(d => d.CategoryId == model.CategoryId && d.AttributeName.ToLower() == newAttr.Name.ToLower());
                            
                        int defId;
                        if (existingDef != null)
                        {
                            defId = existingDef.Id;
                        }
                        else
                        {
                            var newDef = new SCM_System.Models.Entities.ProductAttributeDefinition
                            {
                                CategoryId = model.CategoryId,
                                AttributeName = newAttr.Name,
                                DataType = decimal.TryParse(newAttr.Value, out _) ? "Number" : "Text",
                                Unit = newAttr.Unit ?? string.Empty,
                                IsRequired = false
                            };
                            _context.ProductAttributeDefinitions.Add(newDef);
                            await _context.SaveChangesAsync();
                            defId = newDef.Id;
                        }
                        
                        _context.ProductAttributeValues.Add(new SCM_System.Models.Entities.ProductAttributeValue
                        {
                            ProductId = product.Id,
                            AttributeId = defId,
                            Value = newAttr.Value
                        });
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
            if (id == null) return NotFound();

            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            var product = await _context.Products
                .Include(p => p.AttributeValues)
                .FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
            
            if (product == null || product.IsDeleted) return NotFound();

            var model = new SCM_System.Models.ViewModels.ProductViewModel
            {
                Id = product.Id,
                ProductName = product.ProductName,
                CategoryId = product.CategoryId,
                BasePrice = product.BasePrice,
                CostPrice = product.CostPrice,
                WholesalePrice = product.WholesalePrice,
                DiscountPercentage = product.DiscountPercentage,
                TaxRate = product.TaxRate,
                StockQuantity = product.Quantity,
                MinimumOrderQuantity = product.MinimumOrderQuantity,
                MaximumStockLevel = product.MaximumStockLevel,
                ReorderLevel = product.ReorderLevel,
                ReorderQuantity = product.ReorderQuantity,
                LeadTimeDays = product.LeadTimeDays,
                Unit = product.Unit,
                Description = product.Description,
                SKU = product.SKU,
                ExistingImageUrl = product.ImageUrl,
                IsActive = product.IsAvailable,
                IsFeatured = product.IsFeatured,
                Brand = product.Brand,
                ShortDescription = product.ShortDescription,
                Tags = product.Tags,
                ShippingWeight = product.ShippingWeight,
                ShippingLength = product.ShippingLength,
                ShippingWidth = product.ShippingWidth,
                ShippingHeight = product.ShippingHeight,
                HSCode = product.HSCode,
                IsHazardous = product.IsHazardous,
                MetaTitle = product.MetaTitle,
                MetaDescription = product.MetaDescription,
                MetaKeywords = product.MetaKeywords,
                Slug = product.Slug,
                DynamicAttributes = product.AttributeValues.ToDictionary(a => a.AttributeId, a => a.Value),
                SupplierId = product.SupplierId
            };

            model.CategoryList = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName", product.CategoryId);
            
            var supplierObj = await _context.Suppliers.FindAsync(product.SupplierId);
            model.SupplierList = supplierObj != null ? new SelectList(new[] { supplierObj }, "Id", "CompanyName", product.SupplierId) : null;
            
            return View(model);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SCM_System.Models.ViewModels.ProductViewModel model)
        {
            if (id != model.Id) return NotFound();

            var supplierId = await GetCurrentSupplierIdAsync();
            if (supplierId == null) return Unauthorized();

            var existingProduct = await _context.Products
                .Include(p => p.AttributeValues)
                .FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == supplierId);
                
            if (existingProduct == null || existingProduct.IsDeleted) return NotFound();

            ModelState.Remove("CategoryList");
            ModelState.Remove("SupplierList");
            ModelState.Remove("ImageFile");
            ModelState.Remove("SupplierId");

            if (ModelState.IsValid)
            {
                try
                {
                    existingProduct.ProductName = model.ProductName;
                    existingProduct.CategoryId = model.CategoryId;
                    existingProduct.BasePrice = model.BasePrice;
                    existingProduct.CostPrice = model.CostPrice;
                    existingProduct.WholesalePrice = model.WholesalePrice;
                    existingProduct.DiscountPercentage = model.DiscountPercentage;
                    existingProduct.TaxRate = model.TaxRate;
                    existingProduct.Quantity = model.StockQuantity;
                    existingProduct.MinimumOrderQuantity = model.MinimumOrderQuantity;
                    existingProduct.MaximumStockLevel = model.MaximumStockLevel;
                    existingProduct.ReorderLevel = model.ReorderLevel;
                    existingProduct.ReorderQuantity = model.ReorderQuantity;
                    existingProduct.LeadTimeDays = model.LeadTimeDays;
                    existingProduct.Unit = model.Unit;
                    existingProduct.Description = model.Description;
                    existingProduct.SKU = model.SKU;
                    existingProduct.IsAvailable = model.IsActive;
                    existingProduct.IsFeatured = model.IsFeatured;
                    existingProduct.Brand = model.Brand;
                    existingProduct.ShortDescription = model.ShortDescription;
                    existingProduct.Tags = model.Tags;
                    existingProduct.ShippingWeight = model.ShippingWeight;
                    existingProduct.ShippingLength = model.ShippingLength;
                    existingProduct.ShippingWidth = model.ShippingWidth;
                    existingProduct.ShippingHeight = model.ShippingHeight;
                    existingProduct.HSCode = model.HSCode;
                    existingProduct.IsHazardous = model.IsHazardous;
                    existingProduct.MetaTitle = model.MetaTitle;
                    existingProduct.MetaDescription = model.MetaDescription;
                    existingProduct.MetaKeywords = model.MetaKeywords;
                    existingProduct.Slug = model.Slug ?? model.ProductName?.ToLower().Replace(" ", "-");

                    // Handle Image Upload
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
                        
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                        }
                        existingProduct.ImageUrl = "/images/products/" + uniqueFileName;
                    }

                    // Handle dynamic attribute saving
                    // Remove all old dynamically generated traits to replace with new state
                    _context.ProductAttributeValues.RemoveRange(existingProduct.AttributeValues);
                    
                    if (model.DynamicAttributes != null && model.DynamicAttributes.Any())
                    {
                        foreach (var attr in model.DynamicAttributes)
                        {
                            if (!string.IsNullOrEmpty(attr.Value))
                            {
                                _context.ProductAttributeValues.Add(new SCM_System.Models.Entities.ProductAttributeValue
                                {
                                    ProductId = existingProduct.Id,
                                    AttributeId = attr.Key,
                                    Value = attr.Value
                                });
                            }
                        }
                    }
                    
                    // Handle new crowdsourced attributes generated by supplier
                    if (model.NewAttributes != null && model.NewAttributes.Any())
                    {
                        foreach (var newAttr in model.NewAttributes)
                        {
                            if (string.IsNullOrWhiteSpace(newAttr.Name) || string.IsNullOrWhiteSpace(newAttr.Value)) continue;
                            
                            var existingDef = await _context.ProductAttributeDefinitions
                                .FirstOrDefaultAsync(d => d.CategoryId == model.CategoryId && d.AttributeName.ToLower() == newAttr.Name.ToLower());
                                
                            int defId;
                            if (existingDef != null)
                            {
                                defId = existingDef.Id;
                            }
                            else
                            {
                                var newDef = new SCM_System.Models.Entities.ProductAttributeDefinition
                                {
                                    CategoryId = model.CategoryId,
                                    AttributeName = newAttr.Name,
                                    DataType = decimal.TryParse(newAttr.Value, out _) ? "Number" : "Text",
                                    Unit = newAttr.Unit ?? string.Empty,
                                    IsRequired = false
                                };
                                _context.ProductAttributeDefinitions.Add(newDef);
                                await _context.SaveChangesAsync();
                                defId = newDef.Id;
                            }
                            
                            _context.ProductAttributeValues.Add(new SCM_System.Models.Entities.ProductAttributeValue
                            {
                                ProductId = existingProduct.Id,
                                AttributeId = defId,
                                Value = newAttr.Value
                            });
                        }
                    }

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(MyProducts));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error updating product: " + ex.Message;
                    model.CategoryList = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName", model.CategoryId);
                    var suppObj = await _context.Suppliers.FindAsync(existingProduct.SupplierId);
                    model.SupplierList = suppObj != null ? new SelectList(new[] { suppObj }, "Id", "CompanyName", existingProduct.SupplierId) : null;
                    return View(model);
                }
            }

            model.CategoryList = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName", model.CategoryId);
            var fallbackSupp = await _context.Suppliers.FindAsync(model.SupplierId);
            model.SupplierList = fallbackSupp != null ? new SelectList(new[] { fallbackSupp }, "Id", "CompanyName", model.SupplierId) : null;
            return View(model);
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
                .Include(p => p.AttributeValues)
                    .ThenInclude(av => av.AttributeDefinition)
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
