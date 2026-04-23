using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class AddStockViewModel
    {
        [Required(ErrorMessage = "Please select a product")]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity to Add")]
        public int QuantityToAdd { get; set; }

        [StringLength(50)]
        [Display(Name = "Batch Number")]
        public string? BatchNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
