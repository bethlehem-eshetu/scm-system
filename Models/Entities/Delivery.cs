using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Delivery
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int? DeliveryEmployeeId { get; set; }
        public SupplierEmployee DeliveryEmployee { get; set; }

        [StringLength(50)]
        [Display(Name = "Tracking Number")]
        public string TrackingNumber { get; set; }

        [StringLength(50)]
        public string Carrier { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Delivery Status")]
        public string DeliveryStatus { get; set; } = "Preparing"; // Preparing, OnTheWay, Delivered

        public DateTime? DepartureTime { get; set; }

        public DateTime? ArrivalTime { get; set; }

        public DateTime? DeliveredDate { get; set; }

        [StringLength(255)]
        [Display(Name = "Proof of Delivery")]
        public string ProofOfDelivery { get; set; }

        // Navigation properties
        public ICollection<DeliveryTracking> TrackingHistory { get; set; }
    }
}