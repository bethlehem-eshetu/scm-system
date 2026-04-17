using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class RatingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRatingService _ratingService;

        public RatingController(ApplicationDbContext context, IRatingService ratingService)
        {
            _context = context;
            _ratingService = ratingService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // GET: /Rating/SupplierRatings
        public async Task<IActionResult> SupplierRatings()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            // Get the supplier ID from the logged-in user
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (supplier == null)
            {
                TempData["ErrorMessage"] = "Supplier not found.";
                return RedirectToAction("Dashboard", "Supplier");
            }

            var ratings = await _ratingService.GetSupplierRatingsAsync(supplier.Id);
            var summary = await _ratingService.GetSupplierRatingSummaryAsync(supplier.Id);

            ViewBag.Summary = summary;
            return View(ratings);
        }

        // GET: /Rating/SupplierRatings/{supplierId} - For public viewing
        public async Task<IActionResult> PublicRatings(int supplierId)
        {
            var ratings = await _ratingService.GetSupplierRatingsAsync(supplierId);
            var summary = await _ratingService.GetSupplierRatingSummaryAsync(supplierId);

            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == supplierId);

            ViewBag.Summary = summary;
            ViewBag.Supplier = supplier;
            return View(ratings);
        }

        // GET: /Rating/RateOrder/{purchaseOrderId}
        public async Task<IActionResult> RateOrder(int purchaseOrderId)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var retailer = await _context.Retailers
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (retailer == null)
                return RedirectToAction("Login", "Account");

            var canRate = await _ratingService.CanRateAsync(purchaseOrderId, retailer.Id);
            if (!canRate)
            {
                TempData["ErrorMessage"] = "You cannot rate this order. Either it's not delivered or you've already rated it.";
                return RedirectToAction("MyPurchaseOrders", "Retailer");
            }

            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Order)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId && po.RetailerId == retailer.Id);

            if (purchaseOrder == null)
                return NotFound();

            ViewBag.PurchaseOrder = purchaseOrder;
            return View();
        }

        // GET: /Rating/MyRatings (For Retailer)
        public async Task<IActionResult> MyRatings()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var retailer = await _context.Retailers
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (retailer == null)
                return RedirectToAction("Login", "Account");

            var ratings = await _context.Ratings
                .Include(r => r.Supplier)
                .Include(r => r.Order)
                .Where(r => r.RetailerId == retailer.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(ratings);
        }

        // POST: /Rating/SubmitRating
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRating(int purchaseOrderId, int ratingValue, string? comment, string? category)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var retailer = await _context.Retailers
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (retailer == null)
                return RedirectToAction("Login", "Account");

            var canRate = await _ratingService.CanRateAsync(purchaseOrderId, retailer.Id);
            if (!canRate)
            {
                TempData["ErrorMessage"] = "You cannot rate this order.";
                return RedirectToAction("MyPurchaseOrders", "Retailer");  // ❌ This is causing the error
            }

            await _ratingService.CreateRatingAsync(purchaseOrderId, ratingValue, comment, category);

            TempData["SuccessMessage"] = "Thank you for your rating!";

            // ✅ Change this to your actual Purchase Orders view path
            return RedirectToAction("RetailerIndex", "PurchaseOrder");
        }

        // POST: /Rating/MarkHelpful
        [HttpPost]
        public async Task<IActionResult> MarkHelpful(int ratingId, bool isHelpful)
        {
            await _ratingService.MarkHelpfulAsync(ratingId, isHelpful);
            return Json(new { success = true });
        }
    }
}