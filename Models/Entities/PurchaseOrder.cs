using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PONumber { get; set; }

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
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Cancelled

        [Required]
        [StringLength(255)]
        public string DeliveryAddress { get; set; }

        [StringLength(100)]
        public string? DeliveryMethod { get; set; }

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public int? DeliveryAgentId { get; set; }
        public SupplierEmployee DeliveryAgent { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed

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

        // Navigation
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public ICollection<ReturnRequest> ReturnRequests { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public Commission Commission { get; set; }
        public Rating Rating { get; set; }


    }
}
