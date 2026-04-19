using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; } // The user being acted upon

        [Required]
        public string Action { get; set; } = string.Empty; // e.g., "Approved", "Rejected"

        public int PerformedBy { get; set; } // The Admin ID who performed the action
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("PerformedBy")]
        public virtual User? PerformedByAdmin { get; set; }

        public string? Reason { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Enhanced tracking
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
