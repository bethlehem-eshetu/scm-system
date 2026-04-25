using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class SupportTicket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } // "General Question", "Bug Report", "Feedback"

        [Required]
        public string Message { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }
    }
}
