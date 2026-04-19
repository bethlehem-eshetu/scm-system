using System;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class FaydaVerification
    {
        [Key]
        [Required]
        [StringLength(16)]
        public string FAN { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty; // Where OTP was sent
        public string? OTP { get; set; }

        public DateTime ExpiryTime { get; set; }

        public int Attempts { get; set; } = 0;

        public bool IsLocked { get; set; } = false;
        
        public bool IsVerified { get; set; } = false;

        public DateTime LastOtpRequestTime { get; set; } = DateTime.UtcNow;

        public string TransactionId { get; set; } = string.Empty;

        public DateTime? OtpExpiry { get; set; }

        public int ResendCount { get; set; } = 0;

        // Identity Snapshots (populated after successful verification)
        public string? VerifiedName { get; set; }
        public DateTime? VerifiedDob { get; set; }
        public string? VerifiedPhone { get; set; }
    }
}
