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

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<Product> Products { get; set; } = [];
        public ICollection<Tender> Tenders { get; set; } = [];
        public ICollection<TenderBid> TenderBids { get; set; } = [];
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
        public ICollection<Order> Orders { get; set; } = [];
        public ICollection<SupplierEmployee> Employees { get; set; } = [];
        public ICollection<Commission> Commissions { get; set; } = [];
        public ICollection<Rating> ReceivedRatings { get; set; } = [];
        public ICollection<Conversation> Conversations { get; set; } = [];
        public ICollection<Warehouse> Warehouses { get; set; } = [];
        public ICollection<Vehicle> Vehicles { get; set; } = [];
        public ICollection<SupplierCategory> SupplierCategories { get; set; } = [];
        public ICollection<InboundShipment> InboundShipments { get; set; } = [];
    }
}