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

        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }

        [StringLength(255)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // New professional fields
        public int? SubCategoryId { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        [StringLength(255)]
        public string? Tags { get; set; }

        public bool IsFeatured { get; set; }

        // Pricing fields
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? WholesalePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 15;

        // Inventory fields
        public int MinimumOrderQuantity { get; set; } = 1;
        public int? MaximumStockLevel { get; set; }
        public int? ReorderLevel { get; set; }
        public int? ReorderQuantity { get; set; }
        public int? LeadTimeDays { get; set; }

        // Specifications are now utilizing the EAV Model (ProductAttributeDefinition/ProductAttributeValue)


        // Shipping fields
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShippingWeight { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShippingLength { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShippingWidth { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShippingHeight { get; set; }

        [StringLength(50)]
        public string? HSCode { get; set; }

        public bool IsHazardous { get; set; }

        // SEO fields
        [StringLength(255)]
        public string? MetaTitle { get; set; }

        [StringLength(1000)]
        public string? MetaDescription { get; set; }

        [StringLength(500)]
        public string? MetaKeywords { get; set; }

        [StringLength(255)]
        public string? Slug { get; set; }

        // Navigation properties
        public ICollection<Inventory> Inventories { get; set; } = [];
        public ICollection<ProductAttributeValue> AttributeValues { get; set; } = [];
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = [];
        public ICollection<OrderItem> OrderItems { get; set; } = [];
        // Add these after your existing fields

        // ========== INVENTORY TRACKING FIELDS ==========
        [Display(Name = "Available Stock")]
        [Column(TypeName = "int")]
        public int AvailableStock { get; set; } = 0;

        [Display(Name = "Reserved Stock")]
        [Column(TypeName = "int")]
        public int ReservedStock { get; set; } = 0;

        [Display(Name = "Dispatched Stock")]
        [Column(TypeName = "int")]
        public int DispatchedStock { get; set; } = 0;

        [Display(Name = "Damaged Stock")]
        [Column(TypeName = "int")]
        public int DamagedStock { get; set; } = 0;

        [Display(Name = "In Transit Stock")]
        [Column(TypeName = "int")]
        public int InTransitStock { get; set; } = 0;

        [Display(Name = "Last Stock Update")]
        public DateTime? LastStockUpdate { get; set; }

        // Total Stock (calculated, not stored in DB)
        [NotMapped]
        public int TotalStock => AvailableStock + ReservedStock + DispatchedStock + DamagedStock + InTransitStock;

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }
}