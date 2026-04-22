using System.ComponentModel.DataAnnotations;

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
        public string CompanyName { get; set; }

        [StringLength(50)]
        [Display(Name = "Business Type")]
        public string? BusinessType { get; set; } // Manufacturer, Distributor, Wholesaler

        [Required]
        [StringLength(100)]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "License Document")]
        public string? LicenseFilePath { get; set; }

        [StringLength(50)]
        [Display(Name = "Tax Identification Number")]
        public string? TaxIdentificationNumber { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Company Address")]
        public string CompanyAddress { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Tender> Tenders { get; set; } = new List<Tender>();
        public ICollection<TenderBid> TenderBids { get; set; } = new List<TenderBid>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<SupplierEmployee> Employees { get; set; } = new List<SupplierEmployee>();
        public ICollection<Commission> Commissions { get; set; } = new List<Commission>();
        public ICollection<Rating> ReceivedRatings { get; set; } = new List<Rating>();
        public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<SupplierCategory> SupplierCategories { get; set; } = new List<SupplierCategory>();
    }
}