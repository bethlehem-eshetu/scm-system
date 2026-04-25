using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SCM_System.Models.Entities;

namespace SCM_System.Models.ViewModels
{
    public class SupplierSettingsViewModel
    {
        public int SupplierId { get; set; }

        public IFormFile? CompanyLogoFile { get; set; }
        public string? ExistingLogo { get; set; }
        public List<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
        public List<SupplierEmployee> Employees { get; set; } = new List<SupplierEmployee>();
        public List<UserSession> ActiveSessions { get; set; } = new List<UserSession>();

        // Notification Preferences
        public bool NotifyOrderAlert { get; set; }
        public bool NotifyBidAlert { get; set; }
        public bool NotifyLowStockAlert { get; set; }
        public bool NotifyPaymentAlert { get; set; }
        public bool NotifyDisputeAlert { get; set; }
        public string NotifyChannel { get; set; } = "Both";

        // Security
        public bool TwoFactorEnabled { get; set; }

        // Performance KPIs
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public double OnTimeDeliveryRate { get; set; }
        public double BidWinRate { get; set; }

        [Display(Name = "Company Description")]
        public string? CompanyDescription { get; set; }

        [Display(Name = "Website URL")]
        [Url]
        public string? WebsiteUrl { get; set; }

        [Display(Name = "Pickup Address")]
        public string? PickupAddress { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
