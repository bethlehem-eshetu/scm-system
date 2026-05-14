using System.ComponentModel.DataAnnotations;

using SCM_System.Models.Enums;

namespace SCM_System.Models.Entities
{
    public class AdminNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        public string NotificationType { get; set; } = "Info";

        public int? RelatedUserId { get; set; }

        [StringLength(200)]
        public string ActionUrl { get; set; } = "/Admin/PendingUsers"; // Always points to the Hub

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
