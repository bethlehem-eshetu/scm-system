using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Expanded Settings
        public int YearsInBusiness { get; set; } = 0;
        
        [StringLength(200)]
        public string? WebsiteUrl { get; set; }

        [StringLength(100)]
        public string? ContactPersonName { get; set; }
        
        [StringLength(100)]
        public string? ContactPersonEmail { get; set; }
        
        [StringLength(20)]
        public string? ContactPersonPhone { get; set; }

        public string? DefaultBillingAddress { get; set; }
        public string? DefaultShippingAddress { get; set; }
        public string? PreferredPaymentMethod { get; set; }
        
        public int DefaultTenderClosingDays { get; set; } = 7;
        public int PreferredDeliveryTimeline { get; set; } = 5;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BudgetMin { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BudgetMax { get; set; }

        public string? PreferredCategories { get; set; } // JSON of category IDs
        
        public bool AutoNotifyNewTenders { get; set; } = true;
        public bool AutoAcceptPreferredBids { get; set; } = false;
        
        [StringLength(100)]
        public string? DefaultShippingMethod { get; set; }
        public bool ProofOfDeliveryRequired { get; set; } = true;

        public string? FavoriteSuppliers { get; set; } // JSON array of IDs
        public string? BlockedSuppliers { get; set; } // JSON array of IDs
        public int SupplierRatingThreshold { get; set; } = 0;

        // Notification Preferences
        public bool NewTenderMatchAlert { get; set; } = true;
        public bool BidAcceptedAlert { get; set; } = true;
        public bool OrderShippedAlert { get; set; } = true;
        public bool OrderDeliveredAlert { get; set; } = true;
        public bool LowStockAlert { get; set; } = true;
        public bool PriceDropAlert { get; set; } = true;
        public bool DeliveryNotifications { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<Tender> Tenders { get; set; }
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Rating> GivenRatings { get; set; }
        public ICollection<Conversation> Conversations { get; set; }
        public Cart Cart { get; set; }
        public ICollection<RetailerCategory> RetailerCategories { get; set; } = new List<RetailerCategory>();
        public ICollection<RetailerAddress> Addresses { get; set; } = new List<RetailerAddress>();
        public ICollection<RetailerPaymentMethod> PaymentMethods { get; set; } = new List<RetailerPaymentMethod>();
        public RetailerPreference? Preference { get; set; }
    }
}