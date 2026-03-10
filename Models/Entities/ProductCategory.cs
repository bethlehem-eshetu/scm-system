using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }

        public string Description { get; set; }

        public int? ParentCategoryId { get; set; }
        public ProductCategory ParentCategory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<ProductCategory> SubCategories { get; set; }
        public ICollection<ProductAttributeDefinition> AttributeDefinitions { get; set; }
        public ICollection<Product> Products { get; set; }
        public ICollection<Tender> Tenders { get; set; }
    }
}