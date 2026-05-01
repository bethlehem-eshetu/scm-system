using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class SupplierTransaction
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "Credit"; // Credit, Debit

        [StringLength(255)]
        public string Reference { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
