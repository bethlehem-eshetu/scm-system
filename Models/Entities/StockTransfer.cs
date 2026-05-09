using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class StockTransfer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SourceWarehouseId { get; set; }
        [ForeignKey("SourceWarehouseId")]
        public virtual Warehouse SourceWarehouse { get; set; }

        [Required]
        public int DestinationWarehouseId { get; set; }
        [ForeignKey("DestinationWarehouseId")]
        public virtual Warehouse DestinationWarehouse { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Requested"; // Requested, Approved, InTransit, Received, Rejected, Cancelled

        public int? RequestedById { get; set; }
        [ForeignKey("RequestedById")]
        public virtual SupplierEmployee RequestedBy { get; set; }

        public int? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public virtual SupplierEmployee ApprovedBy { get; set; }

        public DateTime? RequestedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}