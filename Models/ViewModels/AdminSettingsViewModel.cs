using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SCM_System.Models.Entities;

namespace SCM_System.Models.ViewModels
{
    public class AdminSettingsViewModel
    {
        // Personal Profile
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePicture { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }

        // Password Change
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        // Security
        public bool TwoFactorEnabled { get; set; }
        public List<UserSession> ActiveSessions { get; set; } = new();
        public List<AuditLog> LoginHistory { get; set; } = new();

        // Platform Config
        public decimal CommissionBronze { get; set; }
        public decimal CommissionSilver { get; set; }
        public decimal CommissionGold { get; set; }
        public decimal CommissionPlatinum { get; set; }

        public int PenaltyWarningThreshold { get; set; }
        public int PenaltySuspensionDays { get; set; }
        public int LowStockDefaultThreshold { get; set; }
        public int MaxTenderDays { get; set; }
        public int OrderCancellationHours { get; set; }
        public int AutoReleaseEscrowDays { get; set; }

        // User Defaults
        public bool RequireSupplierApproval { get; set; }
        public bool RequireRetailerApproval { get; set; }
        public string DefaultAccountStatus { get; set; } = "Pending";
        public bool EnableFaydaVerification { get; set; }

        // Notification Templates
        public List<EmailTemplate> EmailTemplates { get; set; } = new();

        // System Settings
        public string? AppUrl { get; set; }
        public string? SupportEmail { get; set; }
        public string? PlatformLogo { get; set; }
        public string? Favicon { get; set; }
        public string? Timezone { get; set; }
        public string? Currency { get; set; }
        public string? DateFormat { get; set; }

        // Chapa Config
        public string? ChapaSecretKey { get; set; }
        public string? ChapaWebhookSecret { get; set; }
        public string? ChapaEnvironment { get; set; } // "Test" or "Live"
        public bool ChapaTestMode { get; set; }

        // Performance Stats (View Only)
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public int PendingApprovals { get; set; }
    }
}
