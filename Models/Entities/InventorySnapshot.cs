using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class InventorySnapshot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int? WarehouseId { get; set; }
        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [Required]
        public int AvailableStock { get; set; }

        [Required]
        public int ReservedStock { get; set; }

        [Required]
        public int DispatchedStock { get; set; }

        [Required]
        public int DamagedStock { get; set; }

        [Required]
        public int InTransitStock { get; set; }

        [Required]
        public DateOnly SnapshotDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}