using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalRetailers { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgOrderValue { get; set; }
        
        public int VerifiedSuppliersCount { get; set; }
        public int PendingSuppliersCount { get; set; }
        public int RejectedSuppliersCount { get; set; }

        public int ApprovedRetailersCount { get; set; }
        public int PendingRetailersCount { get; set; }
        public int RejectedRetailersCount { get; set; }


        public List<SCM_System.Models.Entities.Supplier> RecentSuppliers { get; set; } = new();
        public List<SCM_System.Models.Entities.Retailer> RecentRetailers { get; set; } = new();
    }
}
