using System;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class RatingViewModel
    {
        public int PurchaseOrderId { get; set; }
        public string OrderNumber { get; set; }
        public string PONumber { get; set; }
        
        // Delivery Agent Info
        public int? DeliveryAgentId { get; set; }
        public string DeliveryAgentName { get; set; }
        public string VehiclePlate { get; set; }
        
        // Supplier Info
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        
        // Delivery Rating (1-5)
        [Range(1, 5, ErrorMessage = "Please rate timeliness")]
        public int DeliveryTimeliness { get; set; } = 5;
        
        [Range(1, 5, ErrorMessage = "Please rate professionalism")]
        public int DeliveryProfessionalism { get; set; } = 5;
        
        [Range(1, 5, ErrorMessage = "Please rate vehicle condition")]
        public int VehicleCondition { get; set; } = 5;
        
        [Range(1, 5, ErrorMessage = "Please rate communication")]
        public int Communication { get; set; } = 5;
        
        public string? DeliveryComments { get; set; }
        
        // Supplier Rating (1-5)
        [Range(1, 5, ErrorMessage = "Please rate product quality")]
        public int ProductQuality { get; set; } = 5;
        
        [Range(1, 5, ErrorMessage = "Please rate packaging")]
        public int PackagingQuality { get; set; } = 5;
        
        [Range(1, 5, ErrorMessage = "Please rate shipping speed")]
        public int ShippingSpeed { get; set; } = 5;
        
        public string? SupplierComments { get; set; }
        
        public DateTime DeliveredDate { get; set; }
        public bool IsRated { get; set; }
    }
}
