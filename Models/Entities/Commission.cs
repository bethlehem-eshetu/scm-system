using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Commission
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Chapa Transaction ID")]
        public string ChapaTransactionId { get; set; }

        public string PaymentRequestData { get; set; }

        public string PaymentVerificationData { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Failed

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}