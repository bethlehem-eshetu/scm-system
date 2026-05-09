using SCM_System.Models.Entities;

namespace SCM_System.Models.ViewModels
{
    public class CommissionReportViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommissions { get; set; }
        public decimal PendingPayouts { get; set; }
        public List<CommissionHistoryItem> RecentTransactions { get; set; } = new();
        public List<TierStatistic> TierStats { get; set; } = new();
        public List<ChartDataPoint> RevenueByMonth { get; set; } = new();
    }

    public class CommissionHistoryItem
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string SupplierName { get; set; }
        public string RetailerName { get; set; }
        public decimal Amount { get; set; }
        public decimal CommissionFee { get; set; }
        public decimal Rate { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }

    public class TierStatistic
    {
        public string TierName { get; set; }
        public int SupplierCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
    }
}
