using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Delivery
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int? DeliveryEmployeeId { get; set; }
        public SupplierEmployee? DeliveryEmployee { get; set; }

        [StringLength(50)]
        [Display(Name = "Tracking Number")]
        public required string TrackingNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public required string Carrier { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Delivery Status")]
        public required string DeliveryStatus { get; set; } = "Preparing"; // Preparing, OnTheWay, Delivered

        public DateTime? DepartureTime { get; set; }

        public DateTime? ArrivalTime { get; set; }

        public DateTime? DeliveredDate { get; set; }

        [StringLength(255)]
        [Display(Name = "Proof of Delivery")]
        public string? ProofOfDelivery { get; set; }

        public string? CustomerQRCode { get; set; }  // QR code shown to customer
        public bool IsQRVerified { get; set; } = false;
        public DateTime? QRVerifiedAt { get; set; }
        public string? QRVerificationMethod { get; set; }

        // Navigation properties
        public ICollection<DeliveryTracking> TrackingHistory { get; set; } = [];
    }
}