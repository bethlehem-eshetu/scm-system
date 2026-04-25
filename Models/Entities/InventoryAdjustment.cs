using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class InventoryAdjustment
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
        public int QuantityChange { get; set; } // Positive or negative

        [Required]
        [StringLength(50)]
        public string AdjustmentType { get; set; } // Damage, Theft, CycleCount, Return, Expired

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        public int? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public virtual User ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? PerformedById { get; set; }
        [ForeignKey("PerformedById")]
        public virtual User PerformedBy { get; set; }

        [StringLength(255)]
        public string DocumentReference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}