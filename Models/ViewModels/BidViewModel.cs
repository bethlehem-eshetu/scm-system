using SCM_System.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class BidSubmitViewModel
    {
        public int TenderId { get; set; }
        
        [Required]
        [Display(Name = "Total Amount")]
        public decimal ProposedTotalAmount { get; set; }
        
        [Required]
        [Display(Name = "Lead Time (Days)")]
        public int DeliveryLeadTimeDays { get; set; }
        
        [Required]
        [Display(Name = "Validity Period (Days)")]
        public int ValidityPeriodDays { get; set; }
        
        public string Notes { get; set; }
    }
}
