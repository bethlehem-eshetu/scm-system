using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class InventoryTransfer
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        [Required]
        public int SourceWarehouseId { get; set; }
        [ForeignKey("SourceWarehouseId")]
        public Warehouse? SourceWarehouse { get; set; }

        [Required]
        public int DestinationWarehouseId { get; set; }
        [ForeignKey("DestinationWarehouseId")]
        public Warehouse? DestinationWarehouse { get; set; }

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public SCM_System.Models.Enums.TransferStatus Status { get; set; } = SCM_System.Models.Enums.TransferStatus.Requested;

        public int? RequestedById { get; set; }
        [ForeignKey("RequestedById")]
        public SupplierEmployee? RequestedBy { get; set; }

        public int? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public SupplierEmployee? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public DateTime? RequestedDate { get; set; } = DateTime.Now;
        public DateTime? CompletionDate { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
