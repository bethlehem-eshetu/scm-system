namespace SCM_System.Models.ViewModels
{
    public class CommissionDashboardViewModel
    {
        public decimal TotalEarned { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalSettled { get; set; }
        public int PendingCount { get; set; }
        public int ActiveSuppliersCount { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartData { get; set; } = new();

        public List<string> TierLabels { get; set; } = new();
        public List<int> TierData { get; set; } = new();
        public Dictionary<string, int> TierDistribution { get; set; } = new();
    }
}
