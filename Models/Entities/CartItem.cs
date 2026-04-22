using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }
        public Cart Cart { get; set; }

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        // For custom products or name persistence
        [StringLength(100)]
        public string? ProductName { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}
