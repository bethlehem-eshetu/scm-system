using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Tender
    {
        public int Id { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime ClosingDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open, Closed, Awarded, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<TenderItem> TenderItems { get; set; }
        public ICollection<TenderBid> Bids { get; set; }
    }
}