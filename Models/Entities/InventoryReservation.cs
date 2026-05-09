using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class InventoryReservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int? PurchaseOrderId { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [Required]
        public int SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        public int? WarehouseId { get; set; }
        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public DateTime ReservedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiresAt { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Picking, Packed, Shipped, Cancelled, Expired, Completed

        public DateTime? ReleasedAt { get; set; }

        public int? PickedBy { get; set; }
        public DateTime? PickedAt { get; set; }

        public int? PackedBy { get; set; }
        public DateTime? PackedAt { get; set; }

        public int? ShippedBy { get; set; }
        public DateTime? ShippedAt { get; set; }

        [Range(1, 3)]
        public int Priority { get; set; } = 1; // 1=Normal, 2=Urgent, 3=Express

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}