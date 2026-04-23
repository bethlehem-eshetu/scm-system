using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class WarehouseManagerSettingsViewModel
    {
        public int EmployeeId { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }

        public string? ExistingProfileImage { get; set; }

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

        [Display(Name = "Default Warehouse Location")]
        [StringLength(200)]
        public string? DefaultWarehouseLocation { get; set; }

        [Required]
        [Display(Name = "Low Stock Threshold")]
        [Range(1, 1000, ErrorMessage = "Threshold must be between 1 and 1000")]
        public int LowStockThreshold { get; set; } = 5;

        [Required]
        [Display(Name = "Picklist Format")]
        public string PicklistFormat { get; set; } = "Detailed";

        [Display(Name = "Auto-Accept Pick Tasks")]
        public bool AutoAcceptPickTasks { get; set; }

        [Display(Name = "Notify on Low Stock")]
        public bool NotifyLowStock { get; set; } = true;
    }
}
