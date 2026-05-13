using Microsoft.AspNetCore.Mvc;
using SCM_System.Services;
using SCM_System.Models.ViewModels;

namespace SCM_System.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;
        
        public NotificationBellViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }
        
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Content("");
            
            var unreadCount = await _notificationService.GetUnreadCountForUser(userId.Value);
            var recentNotifications = await _notificationService.GetRecentForUser(userId.Value, 5);
            
            var model = new NotificationBellViewModel
            {
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications
            };
            
            return View(model);
        }
    }
}
