namespace SCM_System.Models.ViewModels
{
    public class AdminMessageViewModel
    {
        public int MessageId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public int ConversationId { get; set; }
        public string ConversationBetween { get; set; } = string.Empty;
        public bool ContainsFlaggedWords { get; set; }
        public int AttachmentCount { get; set; }
        public DateTime Timestamp
        {
            get => SentAt;
            set => SentAt = value;
        }

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - SentAt;
                if (timeSpan.TotalMinutes < 1) return "just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
                return SentAt.ToString("MMM dd");
            }
        }
    }

    public class BlockedMessageViewModel
    {
        public int ViolationId { get; set; }
        public int MessageId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ViolationType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
        public int ConversationId { get; set; }
        public string ConversationBetween { get; set; } = string.Empty;
    }
}