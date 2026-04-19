using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string To { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty; // e.g., "OTP", "Approval", "Rejection"

        public string? ReferenceId { get; set; } // e.g., "FaydaID" or "UserID"

        public bool IsSuccess { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}
