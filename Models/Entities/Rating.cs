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
        public int RatingValue { get; set; } // 1 to 5 stars

        [StringLength(1000)]
        public string? Comment { get; set; }

        [StringLength(50)]
        public string? Category { get; set; } // Product Quality, Delivery Speed, Communication, etc.

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsVerifiedPurchase { get; set; } = true; // Only verified purchases can rate

        public DateTime? UpdatedAt { get; set; }

        // Helpful counts
        public int HelpfulCount { get; set; } = 0;
        public int NotHelpfulCount { get; set; } = 0;
    }
}