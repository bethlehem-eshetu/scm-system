using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class DispatchTask
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public int? DeliveryAgentId { get; set; }
        [ForeignKey("DeliveryAgentId")]
        public SupplierEmployee? DeliveryAgent { get; set; }

        public int? HubId { get; set; }
        [ForeignKey("HubId")]
        public Warehouse? Hub { get; set; }

        [StringLength(200)]
        public string? RouteName { get; set; }
        
        public DateTime? PlannedDeparture { get; set; }
        public DateTime? ActualDeparture { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public DateTime? ActualArrival { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Loading, InTransit, Delivered, Delayed, Cancelled

        [StringLength(500)]
        public string? Notes { get; set; }

        // Proof of Delivery Placeholders
        public string? RecipientName { get; set; }
        public string? SignaturePath { get; set; }
        public string? DeliveryPhotoPath { get; set; }
        public decimal? DeliveryLat { get; set; }
        public decimal? DeliveryLong { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
    }
}
