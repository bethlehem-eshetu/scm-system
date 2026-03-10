using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class TenderItem
    {
        public int Id { get; set; }

        [Required]
        public int TenderId { get; set; }
        public Tender Tender { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

        public string Description { get; set; }
    }
}