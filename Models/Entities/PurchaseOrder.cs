using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "PO Number")]
        public string PONumber { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int? TenderBidId { get; set; }
        public TenderBid TenderBid { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Cancelled

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? ExpectedDeliveryDate { get; set; }

        // Navigation properties
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public Order Order { get; set; }
        public Commission Commission { get; set; }
        public Rating Rating { get; set; }
    }
}