using SCM_System.Models.Entities;

namespace SCM_System.Models.ViewModels
{
    public class LiveTrackingViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string RetailerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;

        // Delivery Information
        public string DeliveryAddress { get; set; } = string.Empty;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string DeliveryAgentName { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
        public string ProofOfDelivery { get; set; } = string.Empty;

        // Tracking
        public List<StatusHistoryItem> StatusHistory { get; set; } = new();
        public List<PurchaseOrderTracking> PurchaseOrders { get; set; } = new();

        // Progress
        public int ProgressPercentage { get; set; }
        public string EstimatedArrival { get; set; } = string.Empty;
    }

    public class StatusHistoryItem
    {
        public string Status { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class PurchaseOrderTracking
    {
        public int Id { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string DeliveryAgent { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
    }
}