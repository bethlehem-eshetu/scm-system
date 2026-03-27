using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [ForeignKey("AttributeId")]
        public ProductAttributeDefinition AttributeDefinition { get; set; }

        [Required]
        [StringLength(255)]
        public string Value { get; set; }
    }
}