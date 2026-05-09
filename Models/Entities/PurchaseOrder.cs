using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PONumber { get; set; } = string.Empty;

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int? TenderBidId { get; set; }
        public TenderBid TenderBid { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VAT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(255)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DeliveryMethod { get; set; }

        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public int? DeliveryAgentId { get; set; }
        public SupplierEmployee DeliveryAgent { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(255)]
        public string? ProofOfDelivery { get; set; }

        [Required]
        public DateTime ExpectedDeliveryDate { get; set; }

        public DateTime? DeliveredAt { get; set; }
        public DateTime? PickedAt { get; set; }
        public DateTime? PackedAt { get; set; }

        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? SignaturePath { get; set; }

        [StringLength(1000)]
        public string? DeliveryNotes { get; set; }

        public bool ChecklistVerified { get; set; } = false;

        public bool IsQRVerified { get; set; } = false;

        [StringLength(500)]
        public string? FailureReason { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        // ERP Overrides & Smart Routing
        public int LoadWeight { get; set; } = 1;
        public bool IsDispatchOverride { get; set; } = false;
        [StringLength(500)]
        public string? DispatchOverrideReason { get; set; }

        // Navigation
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = [];
        public ICollection<ReturnRequest> ReturnRequests { get; set; } = [];
        public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = [];

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public Commission Commission { get; set; }
        public Rating Rating { get; set; }


    }
}
