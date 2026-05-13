namespace SCM_System.Models.ViewModels
{
    public class BidFeedbackViewModel
    {
        public decimal Score { get; set; }
        public int Rank { get; set; }
        public int TotalBids { get; set; }
        public decimal WinningScore { get; set; }
        public List<string> Improvements { get; set; } = new List<string>();
        
        // Detailed Breakdown
        public decimal PriceScore { get; set; }
        public decimal TechnicalScore { get; set; }
        public decimal DeliveryScore { get; set; }
        
        public string TenderTitle { get; set; }
        public string ReferenceNumber { get; set; }
    }
}
