using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        public string? SKU { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        public IEnumerable<SelectListItem>? CategoryList { get; set; }

        [Required]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
        public IEnumerable<SelectListItem>? SupplierList { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        // New professional fields
        public int? SubCategoryId { get; set; }
        public string? Brand { get; set; }
        public string? ShortDescription { get; set; }
        public string? Tags { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true; // corresponds to IsAvailable
        
        // Pricing fields
        [Required]
        public decimal BasePrice { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? WholesalePrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal TaxRate { get; set; } = 15;
        
        // Inventory fields
        [Required]
        [Display(Name = "Initial Stock")]
        public int StockQuantity { get; set; } // maps to Quantity
        
        public int MinimumOrderQuantity { get; set; } = 1;
        public int? MaximumStockLevel { get; set; }
        public int? ReorderLevel { get; set; } // Added by me based on html template
        public int? ReorderQuantity { get; set; }
        public int? LeadTimeDays { get; set; }
        
        [Required]
        public string Unit { get; set; }
        
        // Dynamic attributes for the EAV model
        public Dictionary<int, string>? DynamicAttributes { get; set; } = new Dictionary<int, string>();
        
        // Crowdsourced new specifications mapping
        public List<NewAttributeViewModel>? NewAttributes { get; set; } = new List<NewAttributeViewModel>();
        
        // Shipping fields
        public decimal? ShippingWeight { get; set; }
        public decimal? ShippingLength { get; set; }
        public decimal? ShippingWidth { get; set; }
        public decimal? ShippingHeight { get; set; }
        public string? HSCode { get; set; }
        public bool IsHazardous { get; set; }
        
        // SEO fields
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? Slug { get; set; }
        
        // Gallery
        public IFormFile? ImageFile { get; set; }
        public string? ExistingImageUrl { get; set; }
        public List<IFormFile>? GalleryImages { get; set; }
    }

    public class NewAttributeViewModel
    {
        public string Name { get; set; }
        public string? Value { get; set; }
        public string? Unit { get; set; }
    }
}
