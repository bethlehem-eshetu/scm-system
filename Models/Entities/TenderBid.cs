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
        [Display(Name = "Bid Amount")]
        public decimal BidAmount { get; set; }

        [StringLength(100)]
        [Display(Name = "Delivery Timeline")]
        public string DeliveryTimeline { get; set; }

        public string BidNotes { get; set; }

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
    }
}