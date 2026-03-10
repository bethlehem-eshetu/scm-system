using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class ProductAttributeDefinition
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Attribute Name")]
        public string AttributeName { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Data Type")]
        public string DataType { get; set; } // Number, Text, Boolean, Date

        [StringLength(20)]
        public string Unit { get; set; } // kg, volt, GB, etc.

        public bool IsRequired { get; set; } = false;
    }
}