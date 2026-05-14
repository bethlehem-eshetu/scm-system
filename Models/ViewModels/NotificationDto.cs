using SCM_System.Models.Enums;

namespace SCM_System.Models.ViewModels
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public string ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public string Icon { get; set; }
    }
}
