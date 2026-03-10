using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Penalty
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Penalty Type")]
        public string PenaltyType { get; set; } // Warning, Restriction, Suspension

        [Required]
        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}