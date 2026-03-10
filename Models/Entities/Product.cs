using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Base Price")]
        public decimal BasePrice { get; set; }

        public string Description { get; set; }

        [StringLength(50)]
        public string SKU { get; set; }

        [StringLength(255)]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public Inventory Inventory { get; set; }
        public ICollection<ProductAttributeValue> AttributeValues { get; set; }
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }
}