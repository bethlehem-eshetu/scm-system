using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Order Status")]
        public string OrderStatus { get; set; } = "Processing"; // Processing, Packed, Shipped, Delivered, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<OrderStatusHistory> StatusHistory { get; set; }
        public Delivery Delivery { get; set; }
    }
}