using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class TenderItem
    {
        public int Id { get; set; }

        [Required]
        public int TenderId { get; set; }
        public Tender Tender { get; set; }

        [Required]
        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; }

        public string Description { get; set; }
        public string? Specifications { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedUnitPrice { get; set; }
    }
}
