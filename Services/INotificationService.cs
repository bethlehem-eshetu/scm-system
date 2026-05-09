using SCM_System.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCM_System.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(int userId, string title, string message, string type = "Info", string? actionUrl = null, int? targetWarehouseId = null, string? targetRole = null);
        Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteNotificationAsync(int notificationId);
        Task SendMessageNotificationAsync(int receiverId, int senderId, int conversationId);
        Task SendPenaltyNotificationAsync(int userId, string penaltyType, string reason, int? penaltyId = null);
        Task SendAppealDecisionNotificationAsync(int userId, bool approved, string response, int penaltyId);
        Task SendNotificationAsync(int userId, string title, string message, string type = "Info", string? actionUrl = null, int? targetWarehouseId = null, string? targetRole = null);
        Task SendRoleNotificationAsync(string role, string title, string message, string type = "Info", string? actionUrl = null, int? warehouseId = null);
        Task SendEscalatedNotificationAsync(int userId, string title, string message, string type = "Critical", string? actionUrl = null);
        Task SendReservationExpiredNotification(InventoryReservation reservation);
    }
}