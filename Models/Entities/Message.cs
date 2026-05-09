using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        [Required]
        public int SenderId { get; set; }
        public User Sender { get; set; }

        [Required]
        public string MessageText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
        public DateTime? SeenAt { get; set; }
        public string? MessageType { get; set; } = "Text";
        public string? Priority { get; set; } = "Normal";
        public string? AttachmentUrl { get; set; }

        // ✅ ADD THESE FIELDS
        public bool IsBlocked { get; set; } = false;
        public string? BlockedReason { get; set; }
        public DateTime? BlockedAt { get; set; }
        public bool TriggeredPenalty { get; set; } = false;
        public int? PenaltyId { get; set; }
    }
}