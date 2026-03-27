using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Controllers
{
    public class RetailerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RetailerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to check if user is retailer
        private bool IsRetailer()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            var user = _context.Users.Find(userId);
            return user != null && user.Role == "Retailer" && user.IsApproved;
        }

        // GET: /Retailer/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsRetailer())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers
                .Include(r => r.User)
                .Include(r => r.PurchaseOrders)
                .Include(r => r.Tenders)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (retailer == null)
            {
                return NotFound();
            }

            ViewBag.TotalOrders = retailer.PurchaseOrders?.Count ?? 0;
            ViewBag.PendingOrders = retailer.PurchaseOrders?.Count(po => po.Status == "Pending") ?? 0;
            ViewBag.TotalTenders = retailer.Tenders?.Count ?? 0;
            ViewBag.ActiveTenders = retailer.Tenders?.Count(t => t.Status == "Open") ?? 0;

            return View(retailer);
        }

        // GET: /Retailer/AccountSettings
        public async Task<IActionResult> AccountSettings()
        {
            if (!IsRetailer())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (retailer == null)
            {
                return NotFound();
            }

            return View(retailer);
        }

        // GET: /Retailer/BrowseCatalog
        public async Task<IActionResult> BrowseCatalog(string searchTerm, int? categoryId)
        {
            if (!IsRetailer())
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => !p.IsDeleted && p.IsAvailable);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            ViewData["Categories"] = await _context.ProductCategories.ToListAsync();
            
            return View(products);
        }

        // GET: /Retailer/ProductDetails/5
        public async Task<IActionResult> ProductDetails(int id)
        {
            if (!IsRetailer())
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}