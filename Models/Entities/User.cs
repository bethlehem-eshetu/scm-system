using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

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

        // Navigation properties
        public Supplier Supplier { get; set; }
        public Retailer Retailer { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Penalty> Penalties { get; set; }
    }
}