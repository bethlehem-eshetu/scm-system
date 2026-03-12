using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Retailer")]
    public class MarketplaceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MarketplaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Marketplace
        public async Task<IActionResult> Index(string searchString, int? categoryId, string availabilityFilter, string locationFilter, int? minRating, decimal? maxPrice)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventory)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.ProductName.Contains(searchString) 
                                      || p.Description.Contains(searchString)
                                      || p.Supplier.CompanyName.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(locationFilter))
            {
                query = query.Where(p => p.Supplier.City.Contains(locationFilter) || p.Supplier.Country.Contains(locationFilter));
            }

            if (minRating.HasValue)
            {
                // Assuming Rating is a decimal property on the User model
                // Not yet implemented in the schema directly as a numeric average, but adding for future support
                // query = query.Where(p => p.Supplier.Rating >= minRating.Value); 
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice <= maxPrice.Value);
            }
            
            // Handle Availability filter (In Stock / Out of Stock)
            if (!string.IsNullOrEmpty(availabilityFilter))
            {
                if (availabilityFilter == "InStock")
                {
                    query = query.Where(p => p.Inventory != null && p.Inventory.QuantityAvailable > 0);
                }
                else if (availabilityFilter == "OutOfStock")
                {
                    query = query.Where(p => p.Inventory == null || p.Inventory.QuantityAvailable <= 0);
                }
            }

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            
            // Pass back current filters for the UI
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentAvailability"] = availabilityFilter;
            ViewData["CurrentLocation"] = locationFilter;
            ViewData["CurrentPrice"] = maxPrice;

            return View(products);
        }

        // GET: Marketplace/SupplierProfile/5
        public async Task<IActionResult> SupplierProfile(int id)
        {
            var supplier = await _context.Users
                .Include(u => u.Supplier)
                    .ThenInclude(s => s.Products)
                        .ThenInclude(p => p.Category)
                .Include(u => u.Supplier)
                    .ThenInclude(s => s.Products)
                        .ThenInclude(p => p.Inventory)
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "Supplier");

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }
    }
}
