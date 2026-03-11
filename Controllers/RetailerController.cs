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
    }
}