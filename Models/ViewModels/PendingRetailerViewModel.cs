using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class PendingRetailerViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        [Display(Name = "Business Type")]
        public string BusinessType { get; set; }

        [Display(Name = "Business License Number")]
        public string BusinessLicenseNumber { get; set; }

        [Display(Name = "Tax Identification Number")]
        public string TaxIdentificationNumber { get; set; }

        [Display(Name = "Business Address")]
        public string BusinessAddress { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        [Display(Name = "Store Size")]
        public string StoreSize { get; set; }

        public string Description { get; set; }

        [Display(Name = "Registered Date")]
        public DateTime CreatedAt { get; set; }

        // User information
        [Display(Name = "Contact Person")]
        public string FullName { get; set; }

        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}