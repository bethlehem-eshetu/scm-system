using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Rating
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; } // 1 to 5 stars (Overall)

        [StringLength(50)]
        public string? RatingType { get; set; } // "Delivery" or "Supplier"

        // Delivery specific
        public int? DeliveryAgentId { get; set; }
        public SupplierEmployee? DeliveryAgent { get; set; }

        public int? Timeliness { get; set; }
        public int? Professionalism { get; set; }
        public int? VehicleCondition { get; set; }
        public int? Communication { get; set; }

        // Supplier specific
        public int? ProductQuality { get; set; }
        public int? PackagingQuality { get; set; }
        public int? ShippingSpeed { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        [StringLength(50)]
        public string? Category { get; set; } // Legacy field for product categorizations

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsVerifiedPurchase { get; set; } = true;

        public DateTime? UpdatedAt { get; set; }

        public int HelpfulCount { get; set; } = 0;
        public int NotHelpfulCount { get; set; } = 0;
    }
}