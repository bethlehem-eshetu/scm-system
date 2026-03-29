using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, string type, string? actionUrl = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type, string? actionUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}
