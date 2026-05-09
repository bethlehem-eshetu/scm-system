using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Business Type")]
        public string? BusinessType { get; set; } // Manufacturer, Distributor, Wholesaler

        [Required]
        [StringLength(100)]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "License Document")]
        public string? LicenseFilePath { get; set; }

        [StringLength(50)]
        [Display(Name = "Tax Identification Number")]
        public string? TaxIdentificationNumber { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Company Address")]
        public string CompanyAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Region { get; set; } // Nullable to allow existing rows with NULL values

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = "Ethiopia";

        [StringLength(255)]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Company Description")]
        public string? Description { get; set; }

        [StringLength(20)]
        [Display(Name = "Verification Status")]
        public string VerificationStatus { get; set; } = "Pending"; // Pending, Verified, Rejected

        [StringLength(20)]
        [Display(Name = "Commission Tier")]
        public string CommissionTier { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; } = 5.0m; // Default to Bronze

        public static decimal GetRateByTier(string tier) => tier switch
        {
            "Silver" => 4.0m,
            "Gold" => 3.0m,
            "Platinum" => 2.5m,
            _ => 5.0m // Bronze or default
        };

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;

        public string? CompanyDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? PickupAddress { get; set; }
        public string? CompanyLogo { get; set; }

        // Notification Preferences
        public bool NotifyOrderAlert { get; set; } = true;
        public bool NotifyBidAlert { get; set; } = true;
        public bool NotifyLowStockAlert { get; set; } = true;
        public bool NotifyPaymentAlert { get; set; } = true;
        public bool NotifyDisputeAlert { get; set; } = true;
        public string NotifyChannel { get; set; } = "Both"; // Email, SMS, Both

        // Navigation properties
        public ICollection<Product> Products { get; set; } = [];

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public ICollection<SupplierTransaction> SupplierTransactions { get; set; } = new List<SupplierTransaction>();
        public ICollection<TenderBid> TenderBids { get; set; } = [];
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<SupplierEmployee> Employees { get; set; } = new List<SupplierEmployee>();
        public ICollection<Commission> Commissions { get; set; } = new List<Commission>();
        public ICollection<Rating> ReceivedRatings { get; set; } = new List<Rating>();
        public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<SupplierCategory> SupplierCategories { get; set; } = new List<SupplierCategory>();
        public ICollection<InboundShipment> InboundShipments { get; set; } = new List<InboundShipment>();
        public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
    }
}