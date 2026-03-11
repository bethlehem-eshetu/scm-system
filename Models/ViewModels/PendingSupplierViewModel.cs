using System;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class PendingSupplierViewModel
    {
        public int Id { get; set; }  // Keep as Id

        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Display(Name = "Business Type")]
        public string BusinessType { get; set; }

        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; }

        [Display(Name = "License Document")]
        public string LicenseFilePath { get; set; }

        [Display(Name = "Tax Identification Number")]
        public string TaxIdentificationNumber { get; set; }

        [Display(Name = "Company Address")]
        public string CompanyAddress { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        public string Website { get; set; }

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