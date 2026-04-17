using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public enum PenaltyStatus
    {
        Active = 1,
        Expired = 2,
        Appealed = 3
    }

    public class Penalty
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Penalty Type")]
        public string PenaltyType { get; set; } = string.Empty; // Warning, Restriction, Suspension

        [Required]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Add missing properties that exist in the database
        public string UserType { get; set; } = string.Empty; // "Supplier" or "Retailer"

       

        public int? MessageId { get; set; }
        public Message? Message { get; set; }

        public int? IssuedByAdminId { get; set; }
        public User? IssuedByAdmin { get; set; }

        public bool HasAppealed { get; set; } = false;
        public string? AppealReason { get; set; }
        public DateTime? AppealDate { get; set; }
        public string? AppealResponse { get; set; }
        public DateTime? AppealResponseDate { get; set; }

        public PenaltyStatus Status { get; set; } = PenaltyStatus.Active;
    }
}