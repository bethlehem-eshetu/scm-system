using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class RatingService : IRatingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public RatingService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Rating> CreateRatingAsync(int purchaseOrderId, int ratingValue, string? comment, string? category = null)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Order)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
                throw new Exception("Purchase Order not found");

            var rating = new Rating
            {
                PurchaseOrderId = purchaseOrderId,
                OrderId = purchaseOrder.OrderId,
                SupplierId = purchaseOrder.SupplierId,
                RetailerId = purchaseOrder.RetailerId,
                RatingValue = ratingValue,
                Comment = comment,
                Category = category,
                CreatedAt = DateTime.Now,
                IsVerifiedPurchase = true
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            // Notify supplier about new rating
            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == purchaseOrder.SupplierId);

            if (supplier?.UserId != null)
            {
                await _notificationService.SendNotificationAsync(
                    supplier.UserId,
                    "⭐ New Rating Received",
                    $"You received a {ratingValue}-star rating for order #{purchaseOrder.PONumber}",
                    "Success",
                    "/Supplier/Ratings"
                );
            }

            return rating;
        }

        public async Task<Rating?> GetRatingByPurchaseOrderAsync(int purchaseOrderId)
        {
            return await _context.Ratings
                .FirstOrDefaultAsync(r => r.PurchaseOrderId == purchaseOrderId);
        }

        public async Task<List<Rating>> GetSupplierRatingsAsync(int supplierId)
        {
            return await _context.Ratings
                .Include(r => r.Retailer)
                    .ThenInclude(r => r.User)
                .Include(r => r.Order)
                .Where(r => r.SupplierId == supplierId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<SupplierRatingSummary> GetSupplierRatingSummaryAsync(int supplierId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.SupplierId == supplierId)
                .ToListAsync();

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == supplierId);

            var summary = new SupplierRatingSummary
            {
                SupplierId = supplierId,
                SupplierName = supplier?.CompanyName ?? "Unknown",
                TotalRatings = ratings.Count,
                FiveStarCount = ratings.Count(r => r.RatingValue == 5),
                FourStarCount = ratings.Count(r => r.RatingValue == 4),
                ThreeStarCount = ratings.Count(r => r.RatingValue == 3),
                TwoStarCount = ratings.Count(r => r.RatingValue == 2),
                OneStarCount = ratings.Count(r => r.RatingValue == 1)
            };

            if (ratings.Any())
            {
                summary.AverageRating = ratings.Average(r => r.RatingValue);
            }

            return summary;
        }

        public async Task<bool> CanRateAsync(int purchaseOrderId, int retailerId)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId && po.RetailerId == retailerId);

            if (purchaseOrder == null)
                return false;

            // Check if order is delivered
            if (purchaseOrder.Status != "Delivered" && purchaseOrder.Status != "Completed")
                return false;

            // Check if already rated
            var existingRating = await _context.Ratings
                .AnyAsync(r => r.PurchaseOrderId == purchaseOrderId);

            return !existingRating;
        }

        public async Task<Rating> UpdateRatingAsync(int ratingId, int ratingValue, string? comment)
        {
            var rating = await _context.Ratings.FindAsync(ratingId);
            if (rating == null)
                throw new Exception("Rating not found");

            rating.RatingValue = ratingValue;
            rating.Comment = comment;
            rating.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return rating;
        }

        public async Task MarkHelpfulAsync(int ratingId, bool isHelpful)
        {
            var rating = await _context.Ratings.FindAsync(ratingId);
            if (rating == null) return;

            if (isHelpful)
                rating.HelpfulCount++;
            else
                rating.NotHelpfulCount++;

            await _context.SaveChangesAsync();
        }
    }
}
