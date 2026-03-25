using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Tender
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ReferenceNumber { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        [Required]
        public DateTime SubmissionDeadline { get; set; }

        [Required]
        public DateTime ExpectedDeliveryDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open, Closed, Awarded, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<TenderItem> TenderItems { get; set; }
        public ICollection<TenderBid> Bids { get; set; }
    }
}
