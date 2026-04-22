using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Models.Enums;
using SCM_System.Models.Constants;
using SCM_System.Services;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SCM_System.Services.ISupplierService _supplierService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IWebHostEnvironment _env;

        public SupplierController(ApplicationDbContext context, SCM_System.Services.ISupplierService supplierService, IAuditLogService auditLogService, INotificationService notificationService, IWebHostEnvironment env)
        {
            _context = context;
            _supplierService = supplierService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _env = env;
        }

        // GET: /Supplier/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (supplier.User != null && !string.IsNullOrEmpty(supplier.User.ApprovalStatusMessage))
            {
                ViewBag.ApprovalStatusMessage = supplier.User.ApprovalStatusMessage;
                ViewBag.ApprovalStatusType = supplier.User.ApprovalStatusType;

                // Clear after read
                supplier.User.ApprovalStatusMessage = null;
                // Keep the type for now if needed, or clear both
                // supplier.User.ApprovalStatusType = null; 
                await _context.SaveChangesAsync();
            }

            var analytics = await _supplierService.GetDashboardAnalyticsAsync(supplier.Id);

            // ========== ADD MESSAGING VIEWBAGS ==========

            // Get unread message count
            var unreadCount = await _context.Messages
                .Where(m => m.Conversation.SupplierId == supplier.Id &&
                            m.SenderId != userId &&
                            !m.IsRead)
                .CountAsync();

            ViewBag.UnreadMessagesCount = unreadCount;

            // Get active penalties count
            
            ViewBag.ActivePenalties = await _context.Penalties
                .CountAsync(p => p.UserId == userId && (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));

            // Get recent conversations for dashboard widget
            ViewBag.RecentConversations = await _context.Conversations
                .Include(c => c.Retailer)
                    .ThenInclude(r => r.User)
                .Include(c => c.Supplier)
                    .ThenInclude(s => s.User)
                .Where(c => c.SupplierId == supplier.Id)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Take(5)
                .Select(c => new
                {
                    Id = c.Id,
                    OtherUserName = c.Retailer != null ? c.Retailer.User.FullName : "Retailer",
                    OtherUserRole = "Retailer",
                    OtherUserId = c.RetailerId,
                    LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.MessageText.Length > 50 ? m.MessageText.Substring(0, 50) + "..." : m.MessageText)
                        .FirstOrDefault() ?? "No messages yet",
                    LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead)
                })
                .ToListAsync();

            // ========== AUDIT LOGS ==========
            ViewBag.RecentLogs = await _auditLogService.GetLogsForEntityAsync("Supplier", supplier.Id.ToString());

            return View(analytics);
        }

        // GET: /Supplier/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: /Supplier/Notifications
        public async Task<IActionResult> Notifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // POST: /Supplier/MarkNotificationRead
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // GET: /Supplier/Employees        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var reports = await _supplierService.GetSupplierReportsAsync(supplier.Id);
            return View(reports);
        }

        // GET: /Supplier/OrderTracking
        public async Task<IActionResult> OrderTracking()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var orders = await _supplierService.GetSupplierOrdersForTrackingAsync(supplier.Id);
            return View(orders);
        }

        // GET: /Supplier/Payments
        public async Task<IActionResult> Payments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var commissions = await _supplierService.GetSupplierCommissionsAsync(supplier.Id);
            return View(commissions);
        }

        // POST: /Supplier/PayCommission
        [HttpPost]
        public async Task<IActionResult> PayCommission(int id)
        {
            var commission = await _supplierService.GetCommissionByIdAsync(id);
            if (commission == null) return NotFound();

            // Mock Chapa payment redirection
            // In a real scenario, we would call the Chapa API to create a checkout session
            return Redirect($"https://test.chapa.co/pay?order_id={commission.PurchaseOrderId}&amount={commission.CommissionAmount}");
        }

        [Route("ArchivedAssets")]
        public async Task<IActionResult> ArchivedAssets()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return Unauthorized();

            ViewBag.DeletedEmployees = await _context.SupplierEmployees
                .Include(e => e.User)
                .Where(e => e.SupplierId == supplier.Id && !e.IsActive)
                .ToListAsync();

            ViewBag.DeletedWarehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && !w.IsActive)
                .ToListAsync();

            ViewBag.DeletedVehicles = await _context.Vehicles
                .Where(v => v.SupplierId == supplier.Id && v.Status == SCM_System.Models.Enums.VehicleStatus.Retired)
                .ToListAsync();

            return View();
        }

        [HttpPost("RestoreAsset")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreAsset(string type, int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            try {
                if (type == "Employee")
                {
                    var emp = await _context.SupplierEmployees.FindAsync(id);
                    if (emp != null) {
                        emp.IsActive = true;
                        await _context.SaveChangesAsync();
                        await _auditLogService.LogActionAsync("Employee", id.ToString(), "Restore", notes: "Record reactivated via Restore Hub", performedByUserId: userId);
                    }
                }
                else if (type == "Warehouse")
                {
                    var warehouse = await _context.Warehouses.FindAsync(id);
                    if (warehouse != null) {
                        warehouse.IsActive = true;
                        await _context.SaveChangesAsync();
                        await _auditLogService.LogActionAsync("Warehouse", id.ToString(), "Restore", notes: "Facility reactivated via Restore Hub", performedByUserId: userId);
                    }
                }
                else if (type == "Vehicle")
                {
                    var vehicle = await _context.Vehicles.FindAsync(id);
                    if (vehicle != null) {
                        vehicle.Status = SCM_System.Models.Enums.VehicleStatus.Available;
                        await _context.SaveChangesAsync();
                        await _auditLogService.LogActionAsync("Vehicle", id.ToString(), "Restore", notes: "Vehicle reactivated via Restore Hub", performedByUserId: userId);
                    }
                }

                TempData["SuccessMessage"] = $"{type} restored successfully.";
            }
            catch (Exception ex) {
                TempData["ErrorMessage"] = "Failed to restore asset: " + ex.Message;
            }

            return RedirectToAction(nameof(ArchivedAssets));
        }
    }
}
