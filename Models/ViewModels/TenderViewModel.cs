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

        public string? PackagingRequirements { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? InspectionRequirement { get; set; }
        public string? Language { get; set; } = "English";
        public string? PaymentTerms { get; set; }

        public int PriceWeight { get; set; } = 40;
        public int TechnicalWeight { get; set; } = 40;
        public int DeliveryWeight { get; set; } = 20;

        public decimal? BudgetMin { get; set; }
        public decimal? BudgetMax { get; set; }
        public bool AllowPartialBids { get; set; }
        public IFormFile? Attachment { get; set; }
        public string? PreferredSuppliers { get; set; }
        
        public List<TenderItemViewModel> Items { get; set; } = new List<TenderItemViewModel>();
    }

    public class TenderItemViewModel
    {
        public int? ProductId { get; set; }
        public bool IsCustom { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public string? Specifications { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public string Unit { get; set; }
        public decimal? EstimatedUnitPrice { get; set; }
    }
}
