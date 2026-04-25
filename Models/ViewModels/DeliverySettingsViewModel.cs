using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SCM_System.Models.ViewModels
{
    public class DeliverySettingsViewModel
    {
        // Profile Info
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        public IFormFile? ProfilePicture { get; set; }
        public string? ExistingProfilePicture { get; set; }

        // Password Change
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [MinLength(6)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string? ConfirmPassword { get; set; }

        // Vehicle & Availability
        public int? VehicleId { get; set; }
        public bool IsOnDuty { get; set; }

        public TimeSpan? WorkingHoursStart { get; set; }
        public TimeSpan? WorkingHoursEnd { get; set; }

        [Range(1, 100)]
        public int MaxDailyDeliveries { get; set; }

        // Delivery Preferences
        public bool AutoAcceptAssignments { get; set; }
        public bool RequireProofPhoto { get; set; }
        public bool RequireSignature { get; set; }
        public bool AllowNightDeliveries { get; set; }

        // Notifications
        public bool NotifyNewAssignment { get; set; }
        public string? SmsNotificationNumber { get; set; }

        // Performance Stats (Read-only for View)
        public int TotalDeliveriesMonth { get; set; }
        public double AverageRating { get; set; }
        public double OnTimePercentage { get; set; }
    }
}
