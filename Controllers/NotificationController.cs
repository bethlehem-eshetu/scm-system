using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public NotificationController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "";
        }

        // GET: /Notification
        public async Task<IActionResult> Index()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var notifications = await _notificationService.GetUserNotificationsAsync(currentUserId, 50);
            var unreadCount = await _notificationService.GetUnreadCountAsync(currentUserId);

            ViewBag.UnreadCount = unreadCount;
            ViewBag.UserRole = GetCurrentUserRole();

            return View(notifications);
        }

        // POST: /Notification/MarkAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Json(new { success = false });

            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        // POST: /Notification/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Json(new { success = false });

            await _notificationService.MarkAllAsReadAsync(currentUserId);
            return Json(new { success = true });
        }

        // POST: /Notification/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Json(new { success = false });

            await _notificationService.DeleteNotificationAsync(id);
            return Json(new { success = true });
        }

        // GET: /Notification/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(currentUserId);
            return Json(new { count });
        }

        // GET: /Notification/GetRecentJson  
        [HttpGet]
        public async Task<IActionResult> GetRecentJson()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Json(new { count = 0, items = new object[0] });

            var count = await _notificationService.GetUnreadCountAsync(currentUserId);
            var recent = await _notificationService.GetUserNotificationsAsync(currentUserId, 5);
            
            var items = recent.Select(n => new {
                n.Id,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                n.Type,
                ActionUrl = n.ActionUrl ?? "/Notification/Index"
            }).ToList();

            return Json(new { count, items });
        }
    }
}