using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class TenderBid
    {
        public int Id { get; set; }

        [Required]
        public int TenderId { get; set; }
        public Tender Tender { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VATPercentage { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ProposedTotalAmount { get; set; }

        [Required]
        public int DeliveryLeadTimeDays { get; set; }

        public DateTime? ProposedDeliveryDate { get; set; }

        [StringLength(100)]
        public string? DeliveryMethod { get; set; } // Own fleet, Third-party, Mixed

        [StringLength(255)]
        public string? DeliveryCapacity { get; set; }

        [Required]
        public int ValidityPeriodDays { get; set; }

        public string? Notes { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public string? TechnicalProposal { get; set; }
        
        public string? PackagingPlan { get; set; }
        
        public string? InspectionCompliance { get; set; } // Accept / Reject inspection

        public string? QualityGuarantee { get; set; }
        
        public bool PenaltyAcceptance { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; }

        public string? WarrantyPeriod { get; set; }
        public string? WarrantyType { get; set; }
        public string? PreviousExperience { get; set; }
        public string? PaymentTerms { get; set; }
 
        public string? ProductSpecifications { get; set; }
        public string? QualityCertifications { get; set; }
        public string? InsuranceCoverage { get; set; }
        public string? AfterSalesSupport { get; set; }
        public string? References { get; set; }

        public bool IsWinningBid { get; set; } = false;

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}
