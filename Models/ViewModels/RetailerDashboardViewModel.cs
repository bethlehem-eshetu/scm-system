using SCM_System.Models.Entities;

namespace SCM_System.Models.ViewModels
{
    public class RetailerDashboardViewModel
    {
        public Retailer Retailer { get; set; }

        // KPIs
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int StatusPending { get; set; }
        public int DeliveriesInProgress { get; set; }
        public decimal TotalSpent { get; set; }
        
        // Orders
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        
        // New Sections
        public List<Product> PopularProducts { get; set; } = new List<Product>();
        public List<Supplier> RecommendedSuppliers { get; set; } = new List<Supplier>();
        
        // Footer Stats
        public DateTime StoreSince { get; set; }
        public bool IsEmailVerified { get; set; }
        public string AccountStatus { get; set; } = "Active";
    }
}
