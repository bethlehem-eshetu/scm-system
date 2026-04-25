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
    public class SupplierController(
        ApplicationDbContext context, 
        SCM_System.Services.ISupplierService supplierService, 
        IAuditLogService auditLogService, 
        INotificationService notificationService, 
        IWebHostEnvironment env) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly SCM_System.Services.ISupplierService _supplierService = supplierService;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IAuditLogService _auditLogService = auditLogService;
        private readonly IWebHostEnvironment _env = env;

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

            // ========== FINANCIAL ZONE CALCULATIONS ==========
            var allCommissions = await _context.Commissions
                .Where(c => c.SupplierId == supplier.Id)
                .ToListAsync();

            ViewBag.GrossSales = allCommissions.Where(c => c.PaymentType == "OrderPayment" && c.Status == "Paid").Sum(c => c.OrderAmount);
            ViewBag.NetEarnings = allCommissions.Where(c => c.PaymentType == "SupplierPayout").Sum(c => c.CommissionAmount);
            ViewBag.PendingPayouts = allCommissions.Where(c => c.PaymentType == "SupplierPayout" && c.Status == "Pending").Sum(c => c.CommissionAmount);
            ViewBag.CommissionsPaid = allCommissions.Where(c => c.PaymentType == "PlatformCommission").Sum(c => c.CommissionAmount);
            ViewBag.UnpaidOrdersCount = await _context.PurchaseOrders
                .CountAsync(po => po.SupplierId == supplier.Id && po.PaymentStatus == "Unpaid");

            // Chart Data: Earnings Trend (Last 6 Months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var earningsTrend = allCommissions
                .Where(c => c.PaymentType == "SupplierPayout" && c.CreatedAt >= sixMonthsAgo)
                .GroupBy(c => new { Month = c.CreatedAt.Month, Year = c.CreatedAt.Year })
                .Select(g => new { 
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yy"),
                    Value = g.Sum(c => c.CommissionAmount)
                })
                .OrderBy(x => x.Label)
                .ToList();
            ViewBag.EarningsTrendLabels = earningsTrend.Select(x => x.Label).ToArray();
            ViewBag.EarningsTrendValues = earningsTrend.Select(x => x.Value).ToArray();

            // Chart Data: Payout Status (Pie)
            ViewBag.PayoutStatusLabels = new[] { "Paid Out", "Pending Clearance" };
            ViewBag.PayoutStatusValues = new[] {
                allCommissions.Where(c => c.PaymentType == "SupplierPayout" && c.Status == "Paid").Sum(c => c.CommissionAmount),
                allCommissions.Where(c => c.PaymentType == "SupplierPayout" && c.Status == "Pending").Sum(c => c.CommissionAmount)
            };

            // Top Retailers by Revenue
            ViewBag.TopRetailers = allCommissions
                .Where(c => c.PaymentType == "OrderPayment")
                .GroupBy(c => c.Retailer?.BusinessName ?? "Unknown")
                .Select(g => new { Name = g.Key, Amount = g.Sum(c => c.OrderAmount) })
                .OrderByDescending(x => x.Amount)
                .Take(5)
                .ToList();

            // Recent Financial Ledger (Detailed)
            ViewBag.FinancialLedger = allCommissions
                .Where(c => c.PaymentType == "OrderPayment")
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .Select(c => new {
                    OrderNumber = c.Order?.OrderNumber ?? "N/A",
                    Retailer = c.Retailer?.BusinessName ?? "Unknown",
                    Amount = c.OrderAmount,
                    Commission = allCommissions.FirstOrDefault(pc => pc.PurchaseOrderId == c.PurchaseOrderId && pc.PaymentType == "PlatformCommission")?.CommissionAmount ?? 0,
                    Net = allCommissions.FirstOrDefault(sp => sp.PurchaseOrderId == c.PurchaseOrderId && sp.PaymentType == "SupplierPayout")?.CommissionAmount ?? 0,
                    Status = c.Status,
                    PayoutStatus = allCommissions.FirstOrDefault(sp => sp.PurchaseOrderId == c.PurchaseOrderId && sp.PaymentType == "SupplierPayout")?.Status ?? "N/A"
                })
                .ToList();

            // ========== END FINANCIAL ZONE ==========

            // ========== ADD MESSAGING VIEWBAGS ==========
            // Get unread message count
            var unreadCount = await _context.Messages
                .Where(m => m.Conversation.SupplierId == supplier.Id &&
                            m.SenderId != userId &&
                            !m.IsRead)
                .CountAsync();

            ViewBag.UnreadMessagesCount = unreadCount;

            // Inventory Metrics
            ViewBag.LowStockProductsCount = await _context.Products
                .CountAsync(p => p.SupplierId == supplier.Id && p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved) < 20);
            ViewBag.TotalInventoryValue = await _context.Products
                .Where(p => p.SupplierId == supplier.Id)
                .SumAsync(p => (decimal?)(p.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved) * p.BasePrice)) ?? 0;
            ViewBag.TotalReservedValue = await _context.Products
                .Where(p => p.SupplierId == supplier.Id)
                .SumAsync(p => (decimal?)(p.Inventories.Sum(i => i.QuantityReserved) * p.BasePrice)) ?? 0;

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

            // ✅ RUN FISCAL RECONCILIATION (One-time audit for old records)
            var reconciler = new SCM_System.Tools.FiscalReconciler(_context);
            await reconciler.ReconcileAllAsync();

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
