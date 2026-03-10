using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class ProductAttributeValue
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int AttributeId { get; set; }
        public ProductAttributeDefinition AttributeDefinition { get; set; }

        [Required]
        [StringLength(255)]
        public string Value { get; set; }
    }
}