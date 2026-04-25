using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class InventoryMovement
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
        [StringLength(50)]
        public string MovementType { get; set; } // InboundReceive, ReservationHold, ReservationRelease, PickDeduction, TransferOut, TransferIn, DamageWriteoff, ReturnRestock, StockAdjustment

        [Required]
        public int Quantity { get; set; }

        public int BeforeAvailableStock { get; set; }
        public int BeforeReservedStock { get; set; }
        public int AfterAvailableStock { get; set; }
        public int AfterReservedStock { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; }

        [StringLength(50)]
        public string ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public int? PerformedBy { get; set; }
        [ForeignKey("PerformedBy")]
        public virtual User PerformedByUser { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
        public string? Remarks { get; set; }
        [StringLength(255)]
        public string? DocumentReference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}