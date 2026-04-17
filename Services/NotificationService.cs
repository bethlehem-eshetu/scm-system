using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateNotificationAsync(int userId, string title, string message, string type = "Info", string? actionUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendMessageNotificationAsync(int receiverId, int senderId, int conversationId)
        {
            var sender = await _context.Users.FindAsync(senderId);
            if (sender != null)
            {
                await CreateNotificationAsync(
                    receiverId,
                    "New Message Received",
                    $"You have a new message from {sender.FullName}",
                    "Info",
                    $"/Message/Conversation/{conversationId}"
                );
            }
        }

        public async Task SendPenaltyNotificationAsync(int userId, string penaltyType, string reason, int? penaltyId = null)
        {
            string title = penaltyType switch
            {
                "Warning" => "⚠️ Warning Issued",
                "Message Restriction" => "🔒 Message Restriction Applied",
                "Account Suspension" => "🚫 Account Suspended",
                _ => "Penalty Issued"
            };

            string message = penaltyType switch
            {
                "Warning" => $"You have received a warning: {reason}",
                "Message Restriction" => $"Your messaging has been restricted: {reason}",
                "Account Suspension" => $"Your account has been suspended: {reason}",
                _ => $"A penalty has been issued: {reason}"
            };

            await CreateNotificationAsync(
                userId,
                title,
                message,
                "Warning",
                penaltyId.HasValue ? $"/Penalty" : null
            );
        }

        public async Task SendAppealDecisionNotificationAsync(int userId, bool approved, string response, int penaltyId)
        {
            if (approved)
            {
                await CreateNotificationAsync(
                    userId,
                    "✅ Appeal Approved",
                    $"Your appeal for penalty #{penaltyId} has been approved. The penalty has been removed. Response: {response}",
                    "Success",
                    "/Penalty"
                );
            }
            else
            {
                await CreateNotificationAsync(
                    userId,
                    "❌ Appeal Denied",
                    $"Your appeal for penalty #{penaltyId} has been denied. Response: {response}",
                    "Error",
                    "/Penalty"
                );
            }
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type = "Info", string? actionUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}