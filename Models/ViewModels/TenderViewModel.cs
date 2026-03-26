using SCM_System.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class TenderCreateViewModel
    {
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        
        [Required]
        [Display(Name = "Submission Deadline")]
        public DateTime SubmissionDeadline { get; set; }
        
        [Required]
        [Display(Name = "Expected Delivery Date")]
        public DateTime ExpectedDeliveryDate { get; set; }
        
        public List<TenderItemViewModel> Items { get; set; } = new List<TenderItemViewModel>();
    }

    public class TenderItemViewModel
    {
        [Required]
        public string ProductName { get; set; }
        public string Description { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public string Unit { get; set; }
        public decimal? EstimatedUnitPrice { get; set; }
    }
}
