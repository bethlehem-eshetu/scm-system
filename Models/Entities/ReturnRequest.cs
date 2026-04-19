using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public enum ReturnStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Processing = 4,
        Completed = 5,
        Cancelled = 6
    }

    public enum RefundMethod
    {
        OriginalPayment = 1,
        StoreCredit = 2,
        BankTransfer = 3
    }

    public class ReturnRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ReturnNumber { get; set; } = string.Empty;

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [StringLength(20)]
        public string Reason { get; set; } = string.Empty; // Damaged, Wrong Item, Defective, Expired, Other

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Images { get; set; } // Comma-separated image paths

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [Required]
        public ReturnStatus Status { get; set; } = ReturnStatus.Pending;

        [Required]
        [StringLength(20)]
        public string RefundMethod { get; set; } = "OriginalPayment";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        // Tracking
        public bool IsReturnLabelGenerated { get; set; } = false;
        public string? TrackingNumber { get; set; }
        public DateTime? ItemsShippedAt { get; set; }
        public DateTime? ItemsReceivedAt { get; set; }
    }
}