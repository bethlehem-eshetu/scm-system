using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class AddressUpdateRequest
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string OldAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string NewAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Applied, Rejected

        public string? Reason { get; set; }
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? HandledAt { get; set; }
    }
}
