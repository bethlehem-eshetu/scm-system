using System;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
