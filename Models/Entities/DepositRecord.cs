using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class DepositRecord
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string TransactionRef { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Paid, Failed

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? PaidAt { get; set; }
    }
}