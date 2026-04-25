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

        public async Task<Notification> CreateNotificationAsync(int userId, string title, string message, string type = "Info", string? actionUrl = null, int? targetWarehouseId = null, string? targetRole = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                TargetWarehouseId = targetWarehouseId,
                TargetRole = targetRole,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 20)
        {
            // Get user specific notifications OR role-targeted notifications for this user's role
            var user = await _context.Users.FindAsync(userId);
            var role = user?.Role; // Not the SCM role, but the AspNet role (Supplier, WarehouseManager)

            return await _context.Notifications
                .Where(n => n.UserId == userId || (n.TargetRole == role && (n.TargetWarehouseId == null))) // Basic logic
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
                    $"/Message/Conversation/{senderId}"
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
                    "Info",
                    "/Penalty"
                );
            }
            else
            {
                await CreateNotificationAsync(
                    userId,
                    "❌ Appeal Denied",
                    $"Your appeal for penalty #{penaltyId} has been denied. Response: {response}",
                    "Warning",
                    "/Penalty"
                );
            }
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type = "Info", string? actionUrl = null, int? targetWarehouseId = null, string? targetRole = null)
        {
            await CreateNotificationAsync(userId, title, message, type, actionUrl, targetWarehouseId, targetRole);
        }

        public async Task SendRoleNotificationAsync(string role, string title, string message, string type = "Info", string? actionUrl = null, int? warehouseId = null)
        {
            // This could send to all users in a role or just create a role-targeted notification entry
            var notification = new Notification
            {
                UserId = 0, // System notification
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                TargetRole = role,
                TargetWarehouseId = warehouseId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task SendEscalatedNotificationAsync(int userId, string title, string message, string type = "Critical", string? actionUrl = null)
        {
            await CreateNotificationAsync(userId, title, message, type, actionUrl);
        }

        public async Task SendReservationExpiredNotification(InventoryReservation reservation)
        {
            var supplierUserId = await _context.Suppliers
                .Where(s => s.Id == reservation.SupplierId)
                .Select(s => s.UserId)
                .FirstOrDefaultAsync();

            int? retailerUserId = null;
            if (reservation.PurchaseOrderId.HasValue)
            {
                var po = await _context.PurchaseOrders
                    .Include(p => p.Retailer)
                    .FirstOrDefaultAsync(p => p.Id == reservation.PurchaseOrderId);
                retailerUserId = po?.Retailer?.UserId;
            }

            if (supplierUserId != 0)
            {
                await CreateNotificationAsync(
                    supplierUserId,
                    "Reservation Expired ⏳",
                    $"Order reservation for product {reservation.ProductId} has expired. Stock has been released back to inventory.",
                    "Warning",
                    "/Supplier/Inventory" // assuming some inventory page
                );
            }

            if (retailerUserId.HasValue && retailerUserId.Value != 0)
            {
                var poNumber = reservation.PurchaseOrderId.HasValue ? $"PO #{reservation.PurchaseOrderId}" : "your order";
                await CreateNotificationAsync(
                    retailerUserId.Value,
                    "Order Cancelled ❌",
                    $"Purchase Order {poNumber} has been automatically cancelled due to reservation expiry (24 hours).",
                    "Error",
                    "/Retailer/OrderTracking"
                );
            }
        }
    }
}