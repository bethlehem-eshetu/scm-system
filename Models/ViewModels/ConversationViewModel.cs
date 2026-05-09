using System;

namespace SCM_System.Models.ViewModels
{
    public class ConversationViewModel
    {
        public int Id { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserType { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool HasBlockedMessages { get; set; }
        public bool IsActive { get; set; }

        public string TimeSpanDisplay => TimeAgo;

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - LastMessageAt;

                if (timeSpan.TotalMinutes < 1)
                    return "just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d";
                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)}w";

                return LastMessageAt.ToString("MMM dd");
            }
        }
    }
}