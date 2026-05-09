using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int? ParentCategoryId { get; set; }
        public ProductCategory? ParentCategory { get; set; }

        public int Level { get; set; } = 1; // 1 for Main, 2 for Sub

        public bool IsActive { get; set; } = true;


        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<ProductCategory> SubCategories { get; set; } = [];
        public ICollection<ProductAttributeDefinition> AttributeDefinitions { get; set; } = [];
        public ICollection<Product> Products { get; set; } = [];
        public ICollection<Tender> Tenders { get; set; } = [];
        public ICollection<SupplierCategory> SupplierCategories { get; set; } = [];
        public ICollection<RetailerCategory> RetailerCategories { get; set; } = [];
    }
}