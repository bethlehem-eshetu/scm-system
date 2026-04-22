using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; }



        [StringLength(255)]
        public string DeliveryAddress { get; set; }

        [StringLength(100)]
        public string? DeliveryCity { get; set; }

        [StringLength(100)]
        public string? DeliveryRegion { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderStatus { get; set; } = "Pending"; // Pending, Accepted, Partially Processing, Partially Delivered, Completed, Rejected, Cancelled, Partially Failed

        [Required]
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Escrow, Released

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime? RejectedAt { get; set; }

        // Navigation
        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<OrderStatusHistory> StatusHistory { get; set; }
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; }
        public Delivery Delivery { get; set; }

        public ICollection<ReturnRequest> ReturnRequests { get; set; }

        [StringLength(200)]
        public string? QRCodeValue { get; set; }
    }
}
