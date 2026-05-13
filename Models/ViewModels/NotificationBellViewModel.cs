using System.Collections.Generic;

namespace SCM_System.Models.ViewModels
{
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; }
    }
}
