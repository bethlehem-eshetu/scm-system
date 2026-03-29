using SCM_System.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class BidSubmitViewModel
    {
        public int TenderId { get; set; }
        
        [Required]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }
        
        public int Quantity { get; set; }
        
        [Display(Name = "Discount (%)")]
        public decimal DiscountPercentage { get; set; }
        
        [Display(Name = "VAT (%)")]
        public decimal VATPercentage { get; set; }
        
        [Required]
        [Display(Name = "Lead Time (Days)")]
        public int DeliveryLeadTimeDays { get; set; }
        
        [Display(Name = "Proposed Delivery Date")]
        public DateTime? ProposedDeliveryDate { get; set; }
        
        [Display(Name = "Delivery Method")]
        public string? DeliveryMethod { get; set; }
        
        [Display(Name = "Delivery Capacity")]
        public string? DeliveryCapacity { get; set; }
        
        [Required]
        [Display(Name = "Validity Period (Days)")]
        public int ValidityPeriodDays { get; set; }
        
        [Display(Name = "Technical Proposal")]
        public string? TechnicalProposal { get; set; }
        
        [Display(Name = "Packaging Plan")]
        public string? PackagingPlan { get; set; }
        
        [Display(Name = "Inspection Compliance")]
        public string? InspectionCompliance { get; set; }
        
        [Display(Name = "Accept Delay Penalties")]
        public bool PenaltyAcceptance { get; set; }
        
        public string? Notes { get; set; }
    }
}
