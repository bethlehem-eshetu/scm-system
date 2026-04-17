using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IRatingService
    {
        Task<Rating> CreateRatingAsync(int purchaseOrderId, int ratingValue, string? comment, string? category = null);
        Task<Rating?> GetRatingByPurchaseOrderAsync(int purchaseOrderId);
        Task<List<Rating>> GetSupplierRatingsAsync(int supplierId);
        Task<SupplierRatingSummary> GetSupplierRatingSummaryAsync(int supplierId);
        Task<bool> CanRateAsync(int purchaseOrderId, int retailerId);
        Task<Rating> UpdateRatingAsync(int ratingId, int ratingValue, string? comment);
        Task MarkHelpfulAsync(int ratingId, bool isHelpful);
    }

    public class SupplierRatingSummary
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
        public Dictionary<string, double> CategoryAverages { get; set; } = new();
    }
}