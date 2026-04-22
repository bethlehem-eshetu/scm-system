using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SCM_System.Models.ViewModels
{
    public class AdminSettingsViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }

        public string? ExistingProfileImage { get; set; }

        [Display(Name = "Current Password")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        // Admin Preferences
        [Display(Name = "Default Dashboard View")]
        public string? DefaultDashboardView { get; set; }

        [EmailAddress]
        [Display(Name = "Secondary Notification Email")]
        public string? SecondaryNotificationEmail { get; set; }

        [Display(Name = "Receive System Alerts")]
        public bool ReceiveSystemAlerts { get; set; }

        // New Advanced Preferences
        [Display(Name = "Two-Factor Authentication")]
        public bool TwoFactorEnabled { get; set; }

        [Display(Name = "Theme Preference")]
        public string ThemePreference { get; set; } = "System";

        [Display(Name = "Language Preference")]
        public string LanguagePreference { get; set; } = "English";

        [Display(Name = "Alert on New Registration")]
        public bool AlertNewRegistration { get; set; }

        [Display(Name = "Alert on System Error")]
        public bool AlertSystemError { get; set; }

        [Display(Name = "Daily Activity Summary")]
        public bool AlertDailySummary { get; set; }
    }
}
