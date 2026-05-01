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
using Microsoft.AspNetCore.Authentication;
using Google.Authenticator;
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

        // GET: /Supplier/Settings
        public async Task<IActionResult> Settings()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null) return NotFound();

            var model = new SupplierSettingsViewModel
            {
                SupplierId = supplier.Id,
                CompanyDescription = supplier.CompanyDescription,
                WebsiteUrl = supplier.WebsiteUrl,
                PickupAddress = supplier.PickupAddress,
                ExistingLogo = supplier.CompanyLogo,
                Region = supplier.Region,
                FullName = supplier.User.FullName,
                Email = supplier.User.Email,
                Phone = supplier.User.PhoneNumber,
                BankAccounts = await _context.BankAccounts.Where(b => b.SupplierId == supplier.Id).ToListAsync(),
                Employees = await _context.SupplierEmployees.Include(e => e.User).Include(e => e.Warehouse).Where(e => e.SupplierId == supplier.Id).ToListAsync(),
                
                // Notifications
                NotifyOrderAlert = supplier.NotifyOrderAlert,
                NotifyBidAlert = supplier.NotifyBidAlert,
                NotifyLowStockAlert = supplier.NotifyLowStockAlert,
                NotifyPaymentAlert = supplier.NotifyPaymentAlert,
                NotifyDisputeAlert = supplier.NotifyDisputeAlert,
                NotifyChannel = supplier.NotifyChannel,

                // Security
                TwoFactorEnabled = supplier.User.TwoFactorEnabled,
                ActiveSessions = await _context.UserSessions.Where(us => us.UserId == supplier.User.Id && us.IsActive).ToListAsync(),

                // KPIs
                TotalOrders = await _context.Orders.CountAsync(o => o.SupplierId == supplier.Id),
                TotalRevenue = await _context.Orders.Where(o => o.SupplierId == supplier.Id && o.OrderStatus == "Completed").SumAsync(o => o.TotalAmount),
                AverageRating = await _context.Ratings.Where(r => r.SupplierId == supplier.Id).AnyAsync() ? await _context.Ratings.Where(r => r.SupplierId == supplier.Id).AverageAsync(r => r.RatingValue) : 0,
                OnTimeDeliveryRate = await _context.Deliveries.CountAsync(d => d.Order.SupplierId == supplier.Id) > 0 ? 
                    await _context.Deliveries.CountAsync(d => d.Order.SupplierId == supplier.Id && d.DeliveredDate <= d.Order.ExpectedDeliveryDate) * 100.0 / await _context.Deliveries.CountAsync(d => d.Order.SupplierId == supplier.Id) : 0,
                BidWinRate = await _context.TenderBids.CountAsync(b => b.SupplierId == supplier.Id) > 0 ? 
                    await _context.TenderBids.CountAsync(b => b.SupplierId == supplier.Id && b.Status == "Accepted") * 100.0 / await _context.TenderBids.CountAsync(b => b.SupplierId == supplier.Id) : 0
            };

            ViewBag.Warehouses = new SelectList(await _context.Warehouses.Where(w => w.SupplierId == supplier.Id).ToListAsync(), "Id", "Name");

            // Load Login History (AuditLog)
            ViewBag.LoginHistory = await _context.AuditLogs
                .Where(al => al.PerformedByUserId == userId && al.ActionType == "Login")
                .OrderByDescending(al => al.PerformedAtUtc)
                .Take(10)
                .ToListAsync();

            return View(model);
        }

        // POST: /Supplier/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SupplierSettingsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == model.SupplierId && s.UserId == userId);

            if (supplier == null) return NotFound();

            // Update Supplier Info
            supplier.CompanyDescription = model.CompanyDescription;
            supplier.WebsiteUrl = model.WebsiteUrl;
            supplier.PickupAddress = model.PickupAddress;
            supplier.Region = model.Region;

            // Handle Logo Upload
            if (model.CompanyLogoFile != null && model.CompanyLogoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "suppliers");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"supplier_{supplier.Id}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(model.CompanyLogoFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CompanyLogoFile.CopyToAsync(fileStream);
                }

                // Delete old logo if exists
                if (!string.IsNullOrEmpty(supplier.CompanyLogo))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, supplier.CompanyLogo.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                supplier.CompanyLogo = "/uploads/suppliers/" + uniqueFileName;
            }

            // Update User Info
            supplier.User.FullName = model.FullName;
            supplier.User.Email = model.Email;
            supplier.User.PhoneNumber = model.Phone;

            // Password Change Logic
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required to change password.");
                    return View(model);
                }

                if (!VerifyPassword(model.CurrentPassword, supplier.User.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Invalid current password.");
                    return View(model);
                }

                supplier.User.PasswordHash = HashPassword(model.NewPassword);
            }

            _context.Update(supplier);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("Supplier", supplier.Id.ToString(), "UpdateSettings", notes: "Profile and company info updated", performedByUserId: userId);

            TempData["SuccessMessage"] = "Settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBankAccount(BankAccount model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return NotFound();

            model.SupplierId = supplier.Id;
            if (model.IsPrimary)
            {
                var existingPrimary = await _context.BankAccounts.Where(b => b.SupplierId == supplier.Id && b.IsPrimary).ToListAsync();
                foreach (var b in existingPrimary) b.IsPrimary = false;
            }

            _context.BankAccounts.Add(model);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("BankAccount", model.Id.ToString(), "Add", notes: $"Added bank account: {model.BankName}", performedByUserId: userId);

            return Json(new { success = true, message = "Bank account added successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBankAccount(BankAccount model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var account = await _context.BankAccounts.Include(b => b.Supplier).FirstOrDefaultAsync(b => b.Id == model.Id);
            if (account == null || account.Supplier.UserId != userId) return NotFound();

            account.BankName = model.BankName;
            account.AccountHolderName = model.AccountHolderName;
            account.AccountNumber = model.AccountNumber;
            account.Branch = model.Branch;
            account.SwiftCode = model.SwiftCode;

            if (model.IsPrimary && !account.IsPrimary)
            {
                var existingPrimary = await _context.BankAccounts.Where(b => b.SupplierId == account.SupplierId && b.IsPrimary).ToListAsync();
                foreach (var b in existingPrimary) b.IsPrimary = false;
            }
            account.IsPrimary = model.IsPrimary;

            _context.Update(account);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("BankAccount", account.Id.ToString(), "Update", notes: $"Updated bank account: {account.BankName}", performedByUserId: userId);

            return Json(new { success = true, message = "Bank account updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var account = await _context.BankAccounts.Include(b => b.Supplier).FirstOrDefaultAsync(b => b.Id == id);
            if (account == null || account.Supplier.UserId != userId) return NotFound();

            _context.BankAccounts.Remove(account);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("BankAccount", id.ToString(), "Delete", notes: $"Deleted bank account: {account.BankName}", performedByUserId: userId);

            return Json(new { success = true, message = "Bank account deleted successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(string fullName, string email, string phone, string role, int? warehouseId, bool isActive)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return NotFound();

            // Create User
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                Role = "SupplierEmployee",
                AccountStatus = "Active",
                IsApproved = true,
                PasswordHash = HashPassword("TempPass123!") // User should change this later
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create SupplierEmployee
            var employee = new SupplierEmployee
            {
                UserId = user.Id,
                SupplierId = supplier.Id,
                WarehouseId = warehouseId,
                EmployeeRole = role,
                Phone = phone,
                Email = email,
                IsActive = isActive
            };

            _context.SupplierEmployees.Add(employee);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("SupplierEmployee", employee.Id.ToString(), "Add", notes: $"Added employee: {fullName}", performedByUserId: userId);

            return Json(new { success = true, message = "Employee added successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmployee(int id, string fullName, string email, string phone, string role, int? warehouseId, bool isActive)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var employee = await _context.SupplierEmployees.Include(e => e.User).Include(e => e.Supplier).FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null || employee.Supplier.UserId != userId) return NotFound();

            employee.User.FullName = fullName;
            employee.User.Email = email;
            employee.User.PhoneNumber = phone;
            employee.Email = email;
            employee.Phone = phone;
            employee.EmployeeRole = role;
            employee.WarehouseId = warehouseId;
            employee.IsActive = isActive;

            _context.Update(employee);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("SupplierEmployee", id.ToString(), "Update", notes: $"Updated employee: {fullName}", performedByUserId: userId);

            return Json(new { success = true, message = "Employee updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var employee = await _context.SupplierEmployees.Include(e => e.Supplier).FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null || employee.Supplier.UserId != userId) return NotFound();

            employee.IsDeleted = true;
            employee.IsActive = false;
            _context.Update(employee);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("SupplierEmployee", id.ToString(), "Delete", notes: "Marked employee as deleted", performedByUserId: userId);

            return Json(new { success = true, message = "Employee deleted successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotifications(bool notifyOrderAlert, bool notifyBidAlert, bool notifyLowStockAlert, bool notifyPaymentAlert, bool notifyDisputeAlert, string notifyChannel)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return NotFound();

            supplier.NotifyOrderAlert = notifyOrderAlert;
            supplier.NotifyBidAlert = notifyBidAlert;
            supplier.NotifyLowStockAlert = notifyLowStockAlert;
            supplier.NotifyPaymentAlert = notifyPaymentAlert;
            supplier.NotifyDisputeAlert = notifyDisputeAlert;
            supplier.NotifyChannel = notifyChannel;

            _context.Update(supplier);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Notification preferences updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTwoFactor(bool enable)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (enable)
            {
                user.TwoFactorEnabled = true;
                if (string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    user.TwoFactorSecret = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10);
                }
                
                var tfa = new TwoFactorAuthenticator();
                var setupCode = tfa.GenerateSetupCode("SCM System", user.Email, user.TwoFactorSecret, false, 3);
                
                await _context.SaveChangesAsync();
                return Json(new { success = true, qrCodeUrl = setupCode.QrCodeSetupImageUrl, manualCode = setupCode.ManualEntryKey });
            }
            else
            {
                user.TwoFactorEnabled = false;
                user.TwoFactorSecret = null;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "2FA disabled successfully." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSession(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, message = "Session revoked successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAccount(string password)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.PasswordHash != HashPassword(password))
            {
                return Json(new { success = false, message = "Invalid password." });
            }

            user.AccountStatus = "Suspended";
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier != null) supplier.IsDeleted = true;

            await _context.SaveChangesAsync();
            
            await HttpContext.SignOutAsync();
            
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportData()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return NotFound();

            var orders = await _context.Orders.Where(o => o.SupplierId == supplier.Id).ToListAsync();
            var employees = await _context.SupplierEmployees.Include(e => e.User).Where(e => e.SupplierId == supplier.Id).ToListAsync();
            var bankAccounts = await _context.BankAccounts.Where(b => b.SupplierId == supplier.Id).ToListAsync();

            var csv = new StringBuilder();

            // Orders Section
            csv.AppendLine("ORDERS");
            csv.AppendLine("Order Number,Date,Total Amount,Status,Payment Status");
            foreach (var o in orders)
            {
                csv.AppendLine($"{o.OrderNumber},{o.CreatedAt:yyyy-MM-dd HH:mm},{o.TotalAmount},{o.OrderStatus},{o.PaymentStatus}");
            }
            csv.AppendLine();

            // Employees Section
            csv.AppendLine("EMPLOYEES");
            csv.AppendLine("Full Name,Email,Phone,Role,Status");
            foreach (var e in employees)
            {
                csv.AppendLine($"{e.User.FullName},{e.Email},{e.Phone},{e.EmployeeRole},{(e.IsActive ? "Active" : "Inactive")}");
            }
            csv.AppendLine();

            // Bank Accounts Section
            csv.AppendLine("BANK ACCOUNTS");
            csv.AppendLine("Bank Name,Account Holder,Account Number,Branch,Is Primary");
            foreach (var b in bankAccounts)
            {
                csv.AppendLine($"{b.BankName},{b.AccountHolderName},{b.AccountNumber},{b.Branch},{(b.IsPrimary ? "Yes" : "No")}");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", $"supplier_data_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
