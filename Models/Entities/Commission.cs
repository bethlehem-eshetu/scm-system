using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Commission
    {
        public int Id { get; set; }

        public int? PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        // ✅ Add this for retailer payments (who is paying)
        public int? RetailerId { get; set; }
        public Retailer? Retailer { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionRate { get; set; } = 0.05m;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRateAtTransaction { get; set; } // Snapshot at payment time

        // ✅ Add PaymentType to distinguish who pays whom
        [Required]
        [StringLength(30)]
        public string PaymentType { get; set; } = "PlatformCommission"; // PlatformCommission, OrderPayment

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string? ChapaTransactionId { get; set; }

        [StringLength(200)]
        public string? ChapaPaymentUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime? DueDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? PaymentRequestData { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? PaymentVerificationData { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance { get; set; }

        public bool IsFullyPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SupplierPayoutAmount { get; set; }

        public DateTime? SupplierPayoutDate { get; set; }

        [StringLength(20)]
        public string? SupplierPayoutStatus { get; set; } // Pending, Processed, Failed
    }
}