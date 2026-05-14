using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
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

        // --- Unified Rating System ---

        [HttpGet]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> RateDelivery(int purchaseOrderId)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.DeliveryAgent)
                    .ThenInclude(da => da.User)
                .Include(p => p.Vehicle)
                .Include(p => p.Order)
                    .ThenInclude(o => o.Supplier)
                .Include(p => p.Order)
                    .ThenInclude(o => o.Retailer)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

            if (po == null) return NotFound();

            // Check if already rated (Delivery specifically)
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.PurchaseOrderId == purchaseOrderId && r.RatingType == "Delivery");
            
            var model = new RatingViewModel
            {
                PurchaseOrderId = po.Id,
                OrderNumber = po.Order.OrderNumber,
                PONumber = po.PONumber,
                
                DeliveryAgentId = po.DeliveryAgentId,
                DeliveryAgentName = po.DeliveryAgent?.User?.FullName ?? "Not Assigned",
                VehiclePlate = po.Vehicle?.LicensePlate ?? "Not Assigned",
                
                SupplierId = po.Order.SupplierId,
                SupplierName = po.Order.Supplier?.CompanyName ?? "Unknown Supplier",
                
                DeliveredDate = po.DeliveredAt ?? DateTime.Now,
                IsRated = existingRating != null
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRating(RatingViewModel model)
        {
            var retailerId = await GetRetailerIdAsync();
            if (retailerId == 0) return RedirectToAction("Login", "Account");
            
            // Save Delivery Rating
            var deliveryRating = new Rating
            {
                PurchaseOrderId = model.PurchaseOrderId,
                OrderId = (await _context.PurchaseOrders.FindAsync(model.PurchaseOrderId))?.OrderId ?? 0,
                RetailerId = retailerId,
                RatingType = "Delivery",
                DeliveryAgentId = model.DeliveryAgentId,
                Timeliness = model.DeliveryTimeliness,
                Professionalism = model.DeliveryProfessionalism,
                VehicleCondition = model.VehicleCondition,
                Communication = model.Communication,
                RatingValue = (model.DeliveryTimeliness + model.DeliveryProfessionalism + model.VehicleCondition + model.Communication) / 4,
                Comment = model.DeliveryComments,
                CreatedAt = DateTime.Now
            };
            _context.Ratings.Add(deliveryRating);

            // Save Supplier Rating
            var supplierRating = new Rating
            {
                PurchaseOrderId = model.PurchaseOrderId,
                OrderId = deliveryRating.OrderId,
                RetailerId = retailerId,
                SupplierId = model.SupplierId,
                RatingType = "Supplier",
                ProductQuality = model.ProductQuality,
                PackagingQuality = model.PackagingQuality,
                ShippingSpeed = model.ShippingSpeed,
                RatingValue = (model.ProductQuality + model.PackagingQuality + model.ShippingSpeed) / 3,
                Comment = model.SupplierComments,
                CreatedAt = DateTime.Now
            };
            _context.Ratings.Add(supplierRating);

            await _context.SaveChangesAsync();

            // Update Delivery Agent's average rating
            await UpdateDeliveryAgentAverageRating(model.DeliveryAgentId);
            
            // Update Supplier's average rating
            await UpdateSupplierAverageRating(model.SupplierId);

            TempData["SuccessMessage"] = "Thank you for your feedback! Your ratings have been submitted.";
            return RedirectToAction("MyPurchaseOrders", "Retailer");
        }

        private async Task<int> GetRetailerIdAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return 0;
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            return retailer?.Id ?? 0;
        }

        private async Task UpdateDeliveryAgentAverageRating(int? deliveryAgentId)
        {
            if (!deliveryAgentId.HasValue) return;
            
            var ratings = await _context.Ratings
                .Where(r => r.DeliveryAgentId == deliveryAgentId && r.RatingType == "Delivery")
                .ToListAsync();
            
            if (ratings.Any())
            {
                var avgRating = ratings.Average(r => (double)(r.Timeliness.Value + r.Professionalism.Value + r.VehicleCondition.Value + r.Communication.Value) / 4.0);
                
                var agent = await _context.SupplierEmployees.FindAsync(deliveryAgentId);
                if (agent != null)
                {
                    agent.AverageRating = (decimal)avgRating;
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task UpdateSupplierAverageRating(int supplierId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.SupplierId == supplierId && r.RatingType == "Supplier")
                .ToListAsync();
            
            if (ratings.Any())
            {
                var avgRating = ratings.Average(r => (double)(r.ProductQuality.Value + r.PackagingQuality.Value + r.ShippingSpeed.Value) / 3.0);
                
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier != null)
                {
                    supplier.AverageRating = (decimal)avgRating;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}