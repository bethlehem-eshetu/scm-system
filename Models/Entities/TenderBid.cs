using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class TenderBid
    {
        public int Id { get; set; }

        [Required]
        public int TenderId { get; set; }
        public Tender Tender { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ProposedTotalAmount { get; set; }

        [Required]
        public int DeliveryLeadTimeDays { get; set; }

        [Required]
        public int ValidityPeriodDays { get; set; }

        public string Notes { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}
