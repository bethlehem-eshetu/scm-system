using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class RegisterViewModel
    {
        // Common fields for all users
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "Register As")]
        public string Role { get; set; }

        // Fayda Identity Fields
        [Required(ErrorMessage = "Fayda Account Number (FAN) is required")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "FAN must be a valid 16-digit number.")]
        [Display(Name = "Fayda Account Number (FAN)")]
        public string? FAN { get; set; }

        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        // Supplier fields
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "Business Type")]
        public string? BusinessType { get; set; }

        [Display(Name = "License Number")]
        public string? LicenseNumber { get; set; }

        [Display(Name = "Upload License Document")]
        public IFormFile? LicenseFile { get; set; }

        [Display(Name = "Tax Identification Number")]
        public string? TaxIdentificationNumber { get; set; }

        [Display(Name = "Company Address")]
        public string? CompanyAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [Display(Name = "Country")]
        public string? Country { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Company Description")]
        public string? CompanyDescription { get; set; }

        // Retailer fields
        [Display(Name = "Business Name")]
        public string? BusinessName { get; set; }

        [Display(Name = "Business Type")]
        public string? RetailerBusinessType { get; set; }

        [Display(Name = "Business License Number")]
        public string? RetailerLicenseNumber { get; set; }

        [Display(Name = "Business Address")]
        public string? BusinessAddress { get; set; }

        [Display(Name = "Store Size")]
        public string? StoreSize { get; set; }

        [Display(Name = "Business Description")]
        public string? BusinessDescription { get; set; }

        // Selected Categories for Supplier
        [Display(Name = "Business Categories")]
        public List<int>? SelectedCategoryIds { get; set; } = new List<int>();
    }
}