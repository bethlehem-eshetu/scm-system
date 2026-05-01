using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using SCM_System.Models.Enums;

namespace SCM_System.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(100)]
        public string? TxRef { get; set; }

        [StringLength(255)]
        public string? ReceiptUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}
