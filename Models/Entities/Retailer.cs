using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Retailer
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        [StringLength(50)]
        [Display(Name = "Business Type")]
        public string? BusinessType { get; set; } // Retail Shop, Supermarket, Distributor

        [StringLength(50)]
        [Display(Name = "Tax Identification Number")]
        public string? TaxIdentificationNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Business License Number")]
        public string? BusinessLicenseNumber { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Business Address")]
        public string BusinessAddress { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = "Ethiopia";

        [StringLength(20)]
        [Display(Name = "Store Size")]
        public string? StoreSize { get; set; } // Small, Medium, Large

        [StringLength(255)]
        [Display(Name = "Business Logo")]
        public string? BusinessLogo { get; set; }

        [Display(Name = "Business Description")]
        public string? Description { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<Order> Orders { get; set; }
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; }
        public ICollection<Tender> Tenders { get; set; }
        public ICollection<Rating> GivenRatings { get; set; }
        public ICollection<Conversation> Conversations { get; set; }
    }
}