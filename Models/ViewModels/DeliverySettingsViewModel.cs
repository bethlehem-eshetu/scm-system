using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class DeliverySettingsViewModel
    {
        // Profile Info
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string Phone { get; set; }

        [Display(Name = "Profile Picture")]
        public string? ProfilePicture { get; set; }

        // Security
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

        // Vehicle & Availability
        [Display(Name = "Current Vehicle")]
        public int? VehicleId { get; set; }

        [Display(Name = "On Duty Status")]
        public bool IsOnDuty { get; set; }

        [Display(Name = "Working Hours Start")]
        public TimeSpan? WorkingHoursStart { get; set; }

        [Display(Name = "Working Hours End")]
        public TimeSpan? WorkingHoursEnd { get; set; }

        // Delivery Preferences
        [Range(1, 100)]
        [Display(Name = "Max Daily Deliveries")]
        public int MaxDailyDeliveries { get; set; }

        [Display(Name = "Require Proof Photo")]
        public bool RequireProofPhoto { get; set; }

        [Display(Name = "Require Signature")]
        public bool RequireSignature { get; set; }

        [Display(Name = "Auto Accept Assignments")]
        public bool AutoAcceptAssignments { get; set; }

        [Display(Name = "Allow Night Deliveries")]
        public bool AllowNightDeliveries { get; set; }

        // Notifications
        [Display(Name = "Notify on New Assignment")]
        public bool NotifyNewAssignment { get; set; }

        [Display(Name = "SMS Notification Number")]
        public string? SmsNotificationNumber { get; set; }

        // Performance (Read-only)
        public int TotalDeliveriesMonth { get; set; }
        public double AverageRating { get; set; }
        public double OnTimePercentage { get; set; }
    }
}
