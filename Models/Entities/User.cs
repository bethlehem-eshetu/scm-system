using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // "Admin", "Supplier", "Retailer", "SupplierEmployee"

        [StringLength(20)]
        [Display(Name = "Account Status")]
        public string AccountStatus { get; set; } = "Pending"; // Pending, Active, Suspended

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public int? LoginAttempts { get; set; } = 0;

        public bool EmailVerified { get; set; } = false;

        public bool PhoneVerified { get; set; } = false;
        
        // Fayda Identity Fields
        [StringLength(16)]
        [Display(Name = "Fayda Account Number (FAN)")]
        public string? FAN { get; set; }

        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        public bool IsFaydaVerified { get; set; } = false;

        [StringLength(20)]
        public string FaydaStatus { get; set; } = "Pending"; // Pending, Verified, Rejected

        public DateTime? FaydaVerifiedAt { get; set; }
        
        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Approved At")]
        public DateTime? ApprovedAt { get; set; }

        public string ApprovalStatus { get; set; } = "Pending";

        public string? VerifiedFullName { get; set; }
        public string? VerifiedPhoneNumber { get; set; }

        public string? ApprovalStatusMessage { get; set; }
        public string? ApprovalStatusType { get; set; } // "Approved" or "Rejected"

        [ForeignKey("FAN")]
        public virtual FaydaVerification? FaydaVerification { get; set; }

        // Navigation properties
        public Supplier Supplier { get; set; }
        public Retailer Retailer { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Penalty> Penalties { get; set; }
    }
}