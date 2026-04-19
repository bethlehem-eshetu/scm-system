using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        public string Type { get; set; } = "Info"; // Success, Error, Warning, Info

        [StringLength(200)]
        public string? ActionUrl { get; set; } // Link to related page (e.g., /Message/Conversation/5)

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }
    }
}