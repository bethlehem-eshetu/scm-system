using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Google.Authenticator;

using SCM_System.Models.Enums;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(
        ApplicationDbContext context, 
        IWebHostEnvironment webHostEnvironment, 
        INotificationService notificationService,
        SCM_System.Services.IFaydaService faydaService,
        SCM_System.Services.IEmailService emailService,
        IVerificationService verificationService,
        ILogger<AdminController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly INotificationService _notificationService = notificationService;
        private readonly SCM_System.Services.IFaydaService _faydaService = faydaService;
        private readonly SCM_System.Services.IEmailService _emailService = emailService;
        private readonly IVerificationService _verificationService = verificationService;
        private readonly ILogger<AdminController> _logger = logger;

        private async Task<bool> IsAdminAndPopulateNotifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin") return false;

            // Populate ViewBag for layout
            ViewBag.AdminNotifications = await _notificationService.GetAdminNotificationsAsync(5);
            ViewBag.UnreadNotificationCount = await _notificationService.GetAdminUnreadCountAsync();

            return true;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new AdminDashboardViewModel();
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.Category)
                    .ToListAsync() ?? new List<Order>();
                var suppliers = await _context.Suppliers.ToListAsync() ?? new List<Supplier>();
                var retailers = await _context.Retailers.Include(r => r.User).ToListAsync() ?? new List<Retailer>();
                var products = await _context.Products.ToListAsync() ?? new List<Product>();
                var commissions = await _context.Commissions.ToListAsync() ?? new List<Commission>();

                model.TotalOrders = orders?.Count ?? 0;
                model.TotalSuppliers = suppliers?.Count ?? 0;
                model.TotalRetailers = retailers?.Count ?? 0;
                model.TotalProducts = products?.Count ?? 0;

                model.TotalRevenue = Math.Round(commissions?.Where(c => c.Status == PaymentStatus.Paid.ToString()).Sum(c => c.CommissionAmount) ?? 0, 2);

                model.AvgOrderValue = model.TotalOrders > 0 
                    ? Math.Round((orders?.Sum(o => o.TotalAmount) ?? 0) / model.TotalOrders, 2)
                    : 0;

                model.VerifiedSuppliersCount = suppliers?.Count(s => s.VerificationStatus == "Verified") ?? 0;
                model.PendingSuppliersCount = suppliers?.Count(s => s.VerificationStatus == "Pending") ?? 0;
                model.RejectedSuppliersCount = suppliers?.Count(s => s.VerificationStatus == "Rejected") ?? 0;

                model.ApprovedRetailersCount = retailers?.Count(r => r.User != null && r.User.IsApproved) ?? 0;
                model.PendingRetailersCount = retailers?.Count(r => r.User == null || (!r.User.IsApproved && r.User.AccountStatus != "Rejected")) ?? 0;
                model.RejectedRetailersCount = retailers?.Count(r => r.User != null && r.User.AccountStatus == "Rejected") ?? 0;

                model.RecentSuppliers = suppliers.OrderByDescending(s => s.CreatedAt).Take(5).ToList();
                model.RecentRetailers = retailers.OrderByDescending(r => r.CreatedAt).Take(5).ToList();

                // Populate ViewBag for View Compatibility (resolves CS1061/RuntimeBinderException)
                ViewBag.TotalSuppliers = model.TotalSuppliers;
                ViewBag.VerifiedSuppliers = model.VerifiedSuppliersCount;
                ViewBag.PendingSuppliers = model.PendingSuppliersCount;
                ViewBag.RejectedSuppliers = model.RejectedSuppliersCount;
                
                ViewBag.TotalRetailers = model.TotalRetailers;
                ViewBag.ApprovedRetailers = model.ApprovedRetailersCount;
                ViewBag.PendingRetailers = model.PendingRetailersCount;
                ViewBag.RejectedRetailers = model.RejectedRetailersCount;

                ViewBag.TotalProducts = model.TotalProducts;
                ViewBag.TotalRevenue = model.TotalRevenue;
                ViewBag.TotalOrders = model.TotalOrders;
                ViewBag.ActiveSuppliers = model.TotalSuppliers;
                ViewBag.ActiveRetailers = model.TotalRetailers;
                ViewBag.PendingSuppliersCount = model.PendingSuppliersCount;
                ViewBag.PendingRetailersCount = model.PendingRetailersCount;

                // Dummy growth values matching mockup (can be replaced with real calc later)
                ViewBag.OrderGrowth = 12;
                ViewBag.RevenueGrowth = 8;
                ViewBag.SupplierGrowth = model.PendingSuppliersCount + " pending";
                ViewBag.RetailerGrowth = "1 new";

                // --- Chart Data Logic (30 Day Trend) ---
                var now = DateTime.Today;
                var last30Days = Enumerable.Range(0, 30)
                    .Select(i => now.AddDays(-i))
                    .OrderBy(d => d)
                    .ToList();

                var dailyRevenue = last30Days.Select(date => 
                    commissions.Where(c => c.CreatedAt.Date == date.Date && (c.Status == "Paid" || c.Status == "Success" || c.Status == "OperationComplete"))
                              .Sum(c => c.CommissionAmount)
                ).ToList();

                var dailyOrders = last30Days.Select(date => 
                    orders.Count(o => o.CreatedAt.Date == date.Date)
                ).ToList();

                ViewBag.Last30DaysLabels = last30Days.Select(d => d.ToString("MMM dd")).ToArray();
                ViewBag.RevenueData = dailyRevenue.ToArray();
                ViewBag.OrderData = dailyOrders.ToArray();

                // --- Category Data (Top 5) ---
                var topCategories = orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(i => i.Product?.Category?.CategoryName ?? "Uncategorized")
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new { 
                        Name = g.Key, 
                        Percentage = orders.SelectMany(o => o.OrderItems).Count() > 0 
                            ? (int)((double)g.Count() / orders.SelectMany(o => o.OrderItems).Count() * 100) 
                            : 0 
                    })
                    .ToList();

                ViewBag.CategoryLabels = topCategories.Select(c => c.Name).ToArray();
                ViewBag.CategoryData = topCategories.Select(c => c.Percentage).ToArray();

                // --- Recent Suppliers (Last 5 with status) ---
                ViewBag.RecentSuppliers = suppliers.OrderByDescending(s => s.CreatedAt).Take(5).Select(s => new {
                    s.CompanyName,
                    Status = s.VerificationStatus ?? "Pending",
                    s.CreatedAt
                }).ToList();


                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                ViewBag.Error = ex.Message;
                
                // Initialize ViewBags even on error to prevent view crash
                ViewBag.TotalSuppliers = 0; ViewBag.VerifiedSuppliers = 0; ViewBag.PendingSuppliers = 0; ViewBag.RejectedSuppliers = 0;
                ViewBag.TotalRetailers = 0; ViewBag.ApprovedRetailers = 0; ViewBag.PendingRetailers = 0; ViewBag.RejectedRetailers = 0;
                ViewBag.TotalProducts = 0; ViewBag.TotalRevenue = 0;

                return View(new AdminDashboardViewModel());
            }
        }

        // GET: /Admin/GetDashboardStats (AJAX for Dynamic Chart)
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats(int range = 7)
        {
            if (!await IsAdminAndPopulateNotifications()) return Unauthorized();

            var now = DateTime.Today;
            var dates = Enumerable.Range(0, range)
                .Select(i => now.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= now.AddDays(-range))
                .ToListAsync();

            var commissions = await _context.Commissions
                .Where(c => c.CreatedAt >= now.AddDays(-range) && (c.Status == "Paid" || c.Status == "Success" || c.Status == "OperationComplete"))
                .ToListAsync();

            var data = dates.Select(date => new
            {
                date = date.ToString("yyyy-MM-dd"),
                orders = orders.Count(o => o.CreatedAt.Date == date.Date),
                revenue = commissions.Where(c => c.CreatedAt.Date == date.Date).Sum(c => c.CommissionAmount)
            });

            return Json(data);
        }

        // --- New Unified Verification Hub Endpoints ---


        // GET: /Admin/PendingUsers
        public async Task<IActionResult> PendingUsers(string? roleFilter, string? statusFilter, string? searchTerm)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            try
            {
                var viewModel = await _verificationService.GetPendingUsersAsync(roleFilter, statusFilter, searchTerm);
                
                // Clear verification-related admin notifications when viewing the hub
                await _notificationService.MarkVerificationNotificationsAsReadAsync();
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Verification Hub");
                TempData["ErrorMessage"] = "Error loading verification data.";
                return View(new PendingUsersViewModel());
            }
        }

        // POST: /Admin/ApproveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            try
            {
                var adminName = HttpContext.Session.GetString("FullName") ?? "Admin";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var success = await _verificationService.ApproveUserAsync(id, adminName, ip, userAgent);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "User approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred during approval.";
            }

            return RedirectToAction(nameof(PendingUsers));
        }

        // POST: /Admin/RejectUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(int id, string reason, string? notes)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            try
            {
                var adminName = HttpContext.Session.GetString("FullName") ?? "Admin";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var success = await _verificationService.RejectUserAsync(id, reason, notes, adminName, ip, userAgent);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "User rejected successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred during rejection.";
            }

            return RedirectToAction(nameof(PendingUsers));
        }

        // POST: /Admin/RejectApplication (JSON Endpoint for Generic Rejections)
        [HttpPost]
        [Route("Admin/RejectApplication")]
        public async Task<IActionResult> RejectApplication([FromBody] RejectRequest request)
        {
            try
            {
                if (!await IsAdminAndPopulateNotifications()) return Unauthorized(new { message = "Unauthorized access." });

                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    return BadRequest(new { message = "Rejection reason is required." });
                }

                // Check for user
                var user = await _context.Users.FindAsync(request.UserId);
                
                if (user == null)
                {
                    return NotFound(new { message = $"{request.UserType} not found." });
                }

                user.IsApproved = false;
                user.AccountStatus = "Rejected";
                user.RejectionReason = request.RejectionReason;
                user.ApprovalStatus = "Rejected";
                user.ApprovalStatusType = "Rejected";
                user.ApprovalStatusMessage = $"Your account was rejected. Reason: {request.RejectionReason}";

                // Update associated roles
                if (user.Role == "Supplier")
                {
                    var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (supplier != null) supplier.VerificationStatus = "Rejected";
                }
                else if (user.Role == "Retailer")
                {
                    var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == user.Id);
                    if (retailer != null) retailer.IsVerified = false;
                }

                // Log audit
                await LogAudit(user.Id, "Rejected", request.RejectionReason);

                await _context.SaveChangesAsync();

                // Send Email if requested
                if (request.SendEmail)
                {
                    try
                    {
                        await _emailService.SendRejectionEmailAsync(user.Email, user.FullName, request.UserType ?? user.Role, request.RejectionReason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send rejection email to {Email}", user.Email);
                    }
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing generic rejection modal for ID: {UserId}", request.UserId);
                return StatusCode(500, new { message = "An internal error occurred." });
            }
        }

        private async Task LogAudit(int targetUserId, string action, string? reason)
        {
            try
            {
                var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var audit = new AuditLog
                {
                    EntityType = "User",
                    EntityId = targetUserId.ToString(),
                    ActionType = action,
                    PerformedByUserId = adminId,
                    Notes = reason,
                    PerformedAtUtc = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };
                _context.AuditLogs.Add(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record audit log for action {Action} on user {UserId}", action, targetUserId);
            }
        }

        // GET: /Admin/CompareFayda/{id}
        public async Task<IActionResult> CompareFayda(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var faydaRegistry = await _context.FaydaRegistries.FirstOrDefaultAsync(f => f.FAN == user.FAN);
            
            ViewBag.User = user;
            ViewBag.Fayda = faydaRegistry;

            return View();
        }

        // POST: /Admin/VerifyAgain
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAgain(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Check if FAN exists in registry and matches name
            var registry = await _context.FaydaRegistries.FirstOrDefaultAsync(f => f.FAN == user.FAN);
            bool isValid = registry != null && registry.FullName.Equals(user.FullName, StringComparison.OrdinalIgnoreCase);

            user.IsFaydaVerified = isValid;
            user.FaydaStatus = isValid ? "Verified" : "Failed";
            user.FaydaVerifiedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            if (isValid)
                TempData["SuccessMessage"] = $"✅ Manual Fayda check successful for {user.FullName}!";
            else
                TempData["ErrorMessage"] = $"❌ Manual Fayda check failed for {user.FullName}. Database mismatch.";

            return RedirectToAction("PendingUsers");
        }

        // GET: /Admin/VerifiedSuppliers
        public async Task<IActionResult> VerifiedSuppliers()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var verifiedSuppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => s.VerificationStatus == "Verified")
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return View(verifiedSuppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifiedSuppliers Error");
                TempData["ErrorMessage"] = "Error loading verified suppliers.";
                return View(new List<Supplier>());
            }
        }

        // GET: /Admin/RejectedSuppliers
        public async Task<IActionResult> RejectedSuppliers()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var rejectedSuppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => s.VerificationStatus == "Rejected")
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return View(rejectedSuppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RejectedSuppliers Error");
                TempData["ErrorMessage"] = "Error loading rejected suppliers.";
                return View(new List<Supplier>());
            }
        }

        // GET: /Admin/AllRetailers
        public async Task<IActionResult> AllRetailers()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var retailers = await _context.Retailers
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return View(retailers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AllRetailers Error");
                TempData["ErrorMessage"] = "Error loading retailers.";
                return View(new List<Retailer>());
            }
        }

        // GET: /Admin/AllUsers
        public async Task<IActionResult> AllUsers()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var users = await _context.Users
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.FullName)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AllUsers Error");
                TempData["ErrorMessage"] = "Error loading users.";
                return View(new List<User>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SuspendUser([FromBody] SuspendUserRequest request)
        {
            if (!await IsAdminAndPopulateNotifications()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null) return Json(new { success = false, message = "User not found" });

                user.AccountStatus = "Suspended";
                user.RejectionReason = request.Reason; // Reuse field for suspension reason
                
                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "🔒 Account Suspended",
                    Message = $"Your account has been suspended. Reason: {request.Reason}",
                    Type = "Warning",
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyUser([FromBody] VerifyUserRequest request)
        {
            if (!await IsAdminAndPopulateNotifications()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null) return Json(new { success = false, message = "User not found" });

                user.AccountStatus = "Active";
                user.IsApproved = true;
                user.IsFaydaVerified = true;
                user.FaydaStatus = "Verified";

                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "✅ Account Verified",
                    Message = "Your account has been verified by an administrator.",
                    Type = "Success",
                    CreatedAt = DateTime.Now,
                    ActionUrl = "/Dashboard"
                });

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public async Task<IActionResult> BulkVerifyUsers([FromBody] BulkActionRequest request)
        {
            if (!await IsAdminAndPopulateNotifications()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var users = await _context.Users.Where(u => request.UserIds.Contains(u.Id)).ToListAsync();
                foreach (var user in users)
                {
                    user.AccountStatus = "Active";
                    user.IsApproved = true;
                    user.IsFaydaVerified = true;
                    user.FaydaStatus = "Verified";
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, count = users.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkSuspendUsers([FromBody] BulkActionRequest request)
        {
            if (!await IsAdminAndPopulateNotifications()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var users = await _context.Users.Where(u => request.UserIds.Contains(u.Id)).ToListAsync();
                foreach (var user in users)
                {
                    user.AccountStatus = "Suspended";
                    user.RejectionReason = request.Reason;
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, count = users.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkRejectUsers([FromBody] BulkActionRequest request)
        {
            if (!await IsAdminAndPopulateNotifications()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var users = await _context.Users.Where(u => request.UserIds.Contains(u.Id)).ToListAsync();
                foreach (var user in users)
                {
                    user.AccountStatus = "Rejected";
                    user.RejectionReason = request.Reason;
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, count = users.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersPaginated(int page = 1, int pageSize = 10, string searchTerm = "", string role = "All", string status = "All")
        {
            if (!await IsAdminAndPopulateNotifications()) return Unauthorized();

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm) || u.Id.ToString() == searchTerm);
            }

            if (role != "All")
            {
                query = query.Where(u => u.Role == role);
            }

            if (status != "All")
            {
                query = query.Where(u => u.AccountStatus == status);
            }

            var totalCount = await query.CountAsync();
            
            // Global counts for KPI cards (ignoring filters)
            var totalGlobal = await _context.Users.CountAsync();
            var activeGlobal = await _context.Users.CountAsync(u => u.AccountStatus == "Active");
            var pendingGlobal = await _context.Users.CountAsync(u => u.AccountStatus == "Pending");

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.AccountStatus,
                    LastActive = u.LastLoginAt != null ? u.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm") : "Never",
                    CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Json(new { totalCount, totalGlobal, activeGlobal, pendingGlobal, users });
        }

        [HttpGet]
        public async Task<IActionResult> ExportUsers(string format = "csv")
        {
            if (!await IsAdminAndPopulateNotifications()) return Unauthorized();

            var users = await _context.Users.OrderBy(u => u.CreatedAt).ToListAsync();
            
            var csv = new StringBuilder();
            csv.AppendLine("ID,FullName,Email,Role,Status,LastActive,CreatedAt");
            foreach (var u in users)
            {
                csv.AppendLine($"{u.Id},\"{u.FullName}\",{u.Email},{u.Role},{u.AccountStatus},{(u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm") : "Never")},{u.CreatedAt:yyyy-MM-dd HH:mm}");
            }
            
            byte[] buffer = Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", $"SCM_Users_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // GET: /Admin/AllSuppliers
        public async Task<IActionResult> AllSuppliers()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            var suppliers = await _context.Suppliers
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(suppliers);
        }

        // GET: /Admin/SupplierDetails/{id}
        public async Task<IActionResult> SupplierDetails(int id)
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            var supplier = await _context.Suppliers
                .Include(s => s.User)
                    .ThenInclude(u => u.FaydaVerification)
                .Include(s => s.SupplierCategories)
                    .ThenInclude(sc => sc.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
            {
                return View((SupplierDetailsViewModel)null);
            }

            var viewModel = new SupplierDetailsViewModel
            {
                Id = supplier.Id,
                CompanyName = supplier.CompanyName,
                AccountOwner = supplier.User?.FullName,
                Email = supplier.User?.Email,
                Phone = supplier.User?.PhoneNumber,
                BusinessCategory = supplier.SupplierCategories?.FirstOrDefault()?.Category?.CategoryName ?? "Not specified",
                Headquarters = supplier.City,
                DetailedAddress = supplier.CompanyAddress,
                Status = supplier.VerificationStatus,
                MemberSince = supplier.CreatedAt,
                TaxId = supplier.TaxIdentificationNumber,
                BusinessLicensePath = supplier.LicenseFilePath,
                PermitPath = supplier.Products.FirstOrDefault()?.ImageUrl, // Placeholder or use actual permit field
                DocumentUploadDate = supplier.CreatedAt, // Placeholder
                
                // Fayda data
                FaydaVerified = supplier.User?.IsFaydaVerified ?? false,
                FaydaId = supplier.User?.FAN,
                FaydaRegistryName = supplier.User?.FaydaVerification?.VerifiedName,
                FaydaDOB = supplier.User?.FaydaVerification?.VerifiedDob,
                FaydaConfidenceScore = supplier.User?.Role == "Supplier" ? 88 : 94, // Mock score for UI demo
                
                // Audit history
                AuditHistory = await _context.AuditLogs
                    .Where(a => a.EntityId == id.ToString() && a.EntityType == "Supplier")
                    .OrderByDescending(a => a.PerformedAtUtc)
                    .Select(a => new AuditLogEntry
                    {
                        Action = a.ActionType,
                        PerformedBy = a.PerformedByUser != null ? a.PerformedByUser.FullName : "System",
                        Details = a.Notes ?? string.Empty,
                        Timestamp = a.PerformedAtUtc
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocument(int userId, string userType, string docName)
        {
            try
            {
                // In a real app, this should securely fetch from the uploads folder
                // For this demo, we'll construct the path based on userType and userId
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", userType.ToLower(), userId.ToString());
                
                // Security check to prevent path traversal
                string safeFileName = Path.GetFileName(docName);
                string filePath = Path.Combine(uploadsFolder, safeFileName);
                
                // If not found with exact name, try common extensions
                if (!System.IO.File.Exists(filePath))
                {
                    string[] extensions = { ".pdf", ".jpg", ".jpeg", ".png" };
                    foreach (var ext in extensions)
                    {
                        string testPath = Path.Combine(uploadsFolder, safeFileName + ext);
                        if (System.IO.File.Exists(testPath))
                        {
                            filePath = testPath;
                            break;
                        }
                    }
                }

                if (!System.IO.File.Exists(filePath))
                {
                    // If still not found, return a placeholder or 404
                    return NotFound(new { message = "Document not found" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                string contentType = GetContentType(filePath);
                
                return File(fileBytes, contentType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        // GET: /Admin/RetailerDetails/{id}
        public async Task<IActionResult> RetailerDetails(int id)
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            var retailer = await _context.Retailers
                .Include(r => r.User)
                .Include(r => r.PurchaseOrders)
                    .ThenInclude(po => po.PurchaseOrderItems)
                .Include(r => r.Orders)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retailer == null)
            {
                return View((SCM_System.Models.Entities.Retailer)null);
            }

            return View(retailer);
        }

        // GET: /Admin/Notifications
        public async Task<IActionResult> Notifications()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser == null)
                {
                    return NotFound();
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == adminUser.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notifications Error");
                TempData["ErrorMessage"] = "Error loading notifications.";
                return View(new List<Notification>());
            }
        }

        // POST: /Admin/MarkNotificationRead
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Notification not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkNotificationRead Error");
                return Json(new { success = false, message = "Error marking notification as read" });
            }
        }

        // POST: /Admin/MarkAllNotificationsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser == null)
                {
                    return Json(new { success = false, message = "Admin not found" });
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == adminUser.Id && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, count = notifications.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAllNotificationsRead Error");
                return Json(new { success = false, message = "Error marking notifications as read" });
            }
        }

        // GET: /Admin/GetUnreadCount
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return Json(new { count = 0 });
            }

            try
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser == null)
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == adminUser.Id && !n.IsRead);

                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUnreadCount Error");
                return Json(new { count = 0 });
            }
        }


        // POST: /Admin/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound();
                }

                // Verify current password
                if (user.PasswordHash != HashPassword(currentPassword))
                {
                    TempData["ErrorMessage"] = "❌ Current password is incorrect.";
                    return RedirectToAction("Settings");
                }

                // Verify new password matches confirm
                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "❌ New password and confirm password do not match.";
                    return RedirectToAction("Settings");
                }

                // Validate password strength
                if (newPassword.Length < 6)
                {
                    TempData["ErrorMessage"] = "❌ Password must be at least 6 characters long.";
                    return RedirectToAction("Settings");
                }

                // Update password
                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ Password changed successfully.";
                Console.WriteLine($"✅ Password changed for admin user {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangePassword Error");
                TempData["ErrorMessage"] = "❌ An error occurred while changing password.";
            }

            return RedirectToAction("Settings");
        }

        // GET: /Admin/Reports
        public async Task<IActionResult> Reports()
        {
            if (!await IsAdminAndPopulateNotifications())
                return RedirectToAction("Login", "Account");

            try
            {
                // Get monthly statistics
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);
                var startOfYear = new DateTime(now.Year, 1, 1);

                // This month
                ViewBag.NewSuppliersThisMonth = _context.Suppliers.Count(s => s.CreatedAt >= startOfMonth);
                ViewBag.NewRetailersThisMonth = _context.Retailers.Count(r => r.CreatedAt >= startOfMonth);
                ViewBag.NewOrdersThisMonth = _context.Orders.Count(o => o.CreatedAt >= startOfMonth);
                ViewBag.NewProductsThisMonth = _context.Products.Count(p => p.CreatedAt >= startOfMonth);
                ViewBag.NewUsersThisMonth = _context.Users.Count(u => u.CreatedAt >= startOfMonth);

                // Last month
                ViewBag.NewSuppliersLastMonth = _context.Suppliers.Count(s => s.CreatedAt >= startOfLastMonth && s.CreatedAt < startOfMonth);
                ViewBag.NewRetailersLastMonth = _context.Retailers.Count(r => r.CreatedAt >= startOfLastMonth && r.CreatedAt < startOfMonth);
                ViewBag.NewOrdersLastMonth = _context.Orders.Count(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfMonth);
                ViewBag.NewProductsLastMonth = _context.Products.Count(p => p.CreatedAt >= startOfLastMonth && p.CreatedAt < startOfMonth);

                // This year
                ViewBag.NewSuppliersThisYear = _context.Suppliers.Count(s => s.CreatedAt >= startOfYear);
                ViewBag.TotalRevenue = _context.Orders.Where(o => o.OrderStatus == "Completed").Sum(o => (decimal?)o.TotalAmount) ?? 0;

                // Approval stats
                ViewBag.ApprovalRate = _context.Suppliers.Count() > 0
                    ? (_context.Suppliers.Count(s => s.VerificationStatus == "Verified") * 100 / _context.Suppliers.Count())
                    : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reports Error");
                TempData["ErrorMessage"] = "Error loading report data.";
            }

            return View();
        }

        // GET: /Admin/MessageLog
        public async Task<IActionResult> MessageLog()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            
            ViewBag.TotalMessagesCount = await _context.Messages.CountAsync();
            ViewBag.BlockedMessagesCount = await _context.Messages.CountAsync(m => m.IsBlocked);
            ViewBag.ActivePenaltiesCount = await _context.Penalties.CountAsync(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));

            return View(new List<AdminMessageViewModel>());
        }

        // AJAX GET: /Admin/GetFilteredMessages
        [HttpGet]
        public async Task<IActionResult> GetFilteredMessages(string type = "all", string role = "all", string time = "all", string search = "")
        {
            var query = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Conversation)
                    .ThenInclude(c => c.Supplier)
                        .ThenInclude(s => s.User)
                .Include(m => m.Conversation)
                    .ThenInclude(c => c.Retailer)
                        .ThenInclude(r => r.User)
                .AsQueryable();

            // Type Filter
            if (type == "blocked") query = query.Where(m => m.IsBlocked);
            else if (type == "active") query = query.Where(m => !m.IsBlocked && _context.MessageViolations.Any(v => v.MessageId == m.Id && !v.IsResolved));
            else query = query.Where(m => !m.IsBlocked);

            // Role Filter
            if (role != "all") query = query.Where(m => m.Sender.Role == role);

            // Time Filter
            if (time == "today") query = query.Where(m => m.CreatedAt >= DateTime.Today);
            else if (time == "week") query = query.Where(m => m.CreatedAt >= DateTime.Today.AddDays(-7));
            else if (time == "month") query = query.Where(m => m.CreatedAt >= DateTime.Today.AddDays(-30));

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(m => (m.Sender.FullName != null && m.Sender.FullName.ToLower().Contains(search)) || 
                                       (m.MessageText != null && m.MessageText.ToLower().Contains(search)));
            }

            var items = await query.OrderByDescending(m => m.CreatedAt).Take(100).ToListAsync();
            
            var result = items.Select(m => new {
                m.Id,
                SenderName = m.Sender?.FullName ?? "Unknown",
                SenderEmail = m.Sender?.Email,
                SenderRole = m.Sender?.Role,
                Content = m.MessageText,
                SentAt = m.CreatedAt,
                m.ConversationId,
                ConversationBetween = GetConversationBetween(m.Conversation),
                ContainsFlaggedWords = m.IsBlocked
            });

            var counts = new {
                total = await _context.Messages.CountAsync(),
                blocked = await _context.Messages.CountAsync(m => m.IsBlocked),
                penalties = await _context.Penalties.CountAsync(p => p.IsActive)
            };

            return Json(new { items = result, counts });
        }

        // GET: /Admin/BlockedMessages
        public async Task<IActionResult> BlockedMessages()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            ViewBag.TotalBlockedCount = await _context.MessageViolations.CountAsync();
            ViewBag.PendingCount = await _context.MessageViolations.CountAsync(v => !v.IsResolved);
            ViewBag.ResolvedCount = await _context.MessageViolations.CountAsync(v => v.IsResolved);

            return View(new List<BlockedMessageViewModel>());
        }

        // AJAX GET: /Admin/GetFilteredBlockedMessages
        [HttpGet]
        public async Task<IActionResult> GetFilteredBlockedMessages(string type = "all", string role = "all", string time = "all", string search = "")
        {
            var query = _context.MessageViolations
                .Include(v => v.Message)
                    .ThenInclude(m => m.Sender)
                .Include(v => v.Message)
                    .ThenInclude(m => m.Conversation)
                        .ThenInclude(c => c.Supplier)
                            .ThenInclude(s => s.User)
                .Include(v => v.Message)
                    .ThenInclude(m => m.Conversation)
                        .ThenInclude(c => c.Retailer)
                            .ThenInclude(r => r.User)
                .AsQueryable();

            // Type Filter
            if (type == "blocked") query = query.Where(v => !v.IsResolved);
            else if (type == "active") query = query.Where(v => v.IsResolved);

            // Role Filter
            if (role != "all") query = query.Where(v => v.Message.Sender.Role == role);

            // Time Filter
            if (time == "today") query = query.Where(v => v.CreatedAt >= DateTime.Today);
            else if (time == "week") query = query.Where(v => v.CreatedAt >= DateTime.Today.AddDays(-7));
            else if (time == "month") query = query.Where(v => v.CreatedAt >= DateTime.Today.AddDays(-30));

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(v => (v.Message.Sender.FullName != null && v.Message.Sender.FullName.ToLower().Contains(search)) || 
                                       (v.Message.MessageText != null && v.Message.MessageText.ToLower().Contains(search)));
            }

            var items = await query.OrderByDescending(v => v.CreatedAt).Take(100).ToListAsync();

            var result = items.Select(v => new {
                v.Id,
                ViolationId = v.Id,
                SenderName = v.Message.Sender?.FullName ?? "Unknown",
                SenderRole = v.Message.Sender?.Role,
                Content = v.Message.MessageText,
                v.ViolationType,
                v.CreatedAt,
                v.IsResolved,
                v.Message.ConversationId,
                IsSevere = v.ViolationType != null && (v.ViolationType.Contains("Payment") || v.ViolationType.Contains("Phone"))
            });

            var counts = new {
                total = await _context.MessageViolations.CountAsync(),
                pending = await _context.MessageViolations.CountAsync(v => !v.IsResolved),
                resolved = await _context.MessageViolations.CountAsync(v => v.IsResolved)
            };

            return Json(new { items = result, counts });
        }

        // Helper method to get conversation display string
        private string GetConversationBetween(Conversation conversation)
        {
            if (conversation == null) return "Unknown";

            var supplierName = conversation.Supplier?.User?.FullName ?? "Supplier";
            var retailerName = conversation.Retailer?.User?.FullName ?? "Retailer";

            return $"{supplierName} ↔ {retailerName}";
        }

        // Helper methods for pagination and filtering
        private string GetQueryString(Dictionary<string, object> currentFilters)
        {
            var queryParams = new List<string>();
            if (currentFilters.ContainsKey("SenderName") && !string.IsNullOrEmpty(currentFilters["SenderName"]?.ToString()))
                queryParams.Add($"senderName={Uri.EscapeDataString(currentFilters["SenderName"].ToString())}");
            if (currentFilters.ContainsKey("SenderRole") && !string.IsNullOrEmpty(currentFilters["SenderRole"]?.ToString()))
                queryParams.Add($"senderRole={currentFilters["SenderRole"]}");
            if (currentFilters.ContainsKey("DateFilter") && !string.IsNullOrEmpty(currentFilters["DateFilter"]?.ToString()))
                queryParams.Add($"dateFilter={currentFilters["DateFilter"]}");
            if (currentFilters.ContainsKey("StartDate") && !string.IsNullOrEmpty(currentFilters["StartDate"]?.ToString()))
                queryParams.Add($"startDate={currentFilters["StartDate"]}");
            if (currentFilters.ContainsKey("EndDate") && !string.IsNullOrEmpty(currentFilters["EndDate"]?.ToString()))
                queryParams.Add($"endDate={currentFilters["EndDate"]}");
            if (currentFilters.ContainsKey("Keyword") && !string.IsNullOrEmpty(currentFilters["Keyword"]?.ToString()))
                queryParams.Add($"keyword={Uri.EscapeDataString(currentFilters["Keyword"].ToString())}");
            if (currentFilters.ContainsKey("MinLength") && currentFilters["MinLength"] != null && Convert.ToInt32(currentFilters["MinLength"]) > 0)
                queryParams.Add($"minLength={currentFilters["MinLength"]}");
            return string.Join("&", queryParams);
        }

        // GET: /Admin/ViewConversation/{id}
        public async Task<IActionResult> ViewConversation(int id)
        {
            if (!await IsAdminAndPopulateNotifications())
                return RedirectToAction("Login", "Account");

            try
            {
                var conversation = await _context.Conversations
                    .Include(c => c.Supplier)
                        .ThenInclude(s => s.User)
                    .Include(c => c.Retailer)
                        .ThenInclude(r => r.User)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.Sender)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (conversation == null)
                    return NotFound();

                return View(conversation);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ViewConversation Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading conversation.";
                return RedirectToAction("MessageLog");
            }
        }

        // POST: /Admin/ResolveViolation/{id}
        [HttpPost]
        public async Task<IActionResult> ResolveViolation(int id)
        {
            if (!await IsAdminAndPopulateNotifications())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var violation = await _context.MessageViolations.FindAsync(id);
                if (violation == null)
                    return Json(new { success = false, message = "Violation not found" });

                violation.IsResolved = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Admin/Penalties
        public async Task<IActionResult> Penalties()
        {
            if (!await IsAdminAndPopulateNotifications())
                return RedirectToAction("Login", "Account");

            try
            {
                var penalties = await _context.Penalties
                    .Include(p => p.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                ViewBag.ActivePenaltiesCount = penalties.Count(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));
                ViewBag.PendingAppeals = 0; // You can add appeal functionality later

                return View(penalties);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Penalties Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading penalties.";
                return View(new List<Penalty>());
            }
        }

        // GET: /Admin/UserPenalties/{userId}
        public async Task<IActionResult> UserPenalties(int userId)
        {
            if (!await IsAdminAndPopulateNotifications())
                return RedirectToAction("Login", "Account");

            try
            {
                var penalties = await _context.Penalties
                    .Include(p => p.User)
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var user = await _context.Users.FindAsync(userId);
                ViewBag.UserName = user?.FullName ?? $"User {userId}";
                ViewBag.UserRole = user?.Role ?? "Unknown";

                return View(penalties);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ UserPenalties Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading user penalties.";
                return View(new List<Penalty>());
            }
        }

        // POST: /Admin/ClearPenalty
        [HttpPost]
        public async Task<IActionResult> ClearPenalty(int penaltyId)
        {
            if (!await IsAdminAndPopulateNotifications())
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var penalty = await _context.Penalties.FindAsync(penaltyId);
                if (penalty == null)
                    return Json(new { success = false, message = "Penalty not found" });

                penalty.IsActive = false;
                penalty.ExpiresAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/ResolveAppeal
        // POST: /Admin/ResolveAppeal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveAppeal(int penaltyId, bool approve, string response)
        {
            if (!await IsAdminAndPopulateNotifications())
                return RedirectToAction("Login", "Account");

            try
            {
                var penalty = await _context.Penalties
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == penaltyId);

                if (penalty == null)
                {
                    TempData["ErrorMessage"] = "Penalty not found.";
                    return RedirectToAction("Penalties");
                }

                penalty.AppealResponse = response;
                penalty.AppealResponseDate = DateTime.Now;

                if (approve)
                {
                    penalty.IsActive = false;
                    penalty.ExpiresAt = DateTime.Now;

                    TempData["SuccessMessage"] = $"Appeal approved. Penalty #{penalty.Id} has been removed.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Appeal denied. Penalty #{penalty.Id} remains active.";
                }

                await _context.SaveChangesAsync();

                // ✅ Send notification to user about appeal decision
                await _notificationService.SendAppealDecisionNotificationAsync(penalty.UserId, approve, response, penalty.Id);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Penalties");
        }

        // GET: /Admin/Logout
        public IActionResult Logout()
        {
            string userName = HttpContext.Session.GetString("UserName") ?? "Admin";
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = $"👋 Goodbye, {userName}! You have been logged out successfully.";
            return RedirectToAction("Login", "Account");
        }

        private static string HashPassword(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
        
        // GET: /Admin/AuditLogs
        public async Task<IActionResult> AuditLogs()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var logs = await _context.AuditLogs
                    .Include(l => l.PerformedByUser)
                    .OrderByDescending(l => l.PerformedAtUtc)
                    .Take(100)
                    .ToListAsync();
                
                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audit logs");
                TempData["ErrorMessage"] = "Error loading audit logs data.";
                return View(new List<AuditLog>());
            }
        }

        // GET: /Admin/EmailLogs
        public async Task<IActionResult> EmailLogs()
        {
            if (!await IsAdminAndPopulateNotifications())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var logs = await _context.EmailLogs
                    .OrderByDescending(l => l.SentAt)
                    .Take(100)
                    .ToListAsync();
                
                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading email logs");
                TempData["ErrorMessage"] = "Error loading email logs data.";
                return View(new List<EmailLog>());
            }
        }

        // --- Category Management Methods ---

        // GET: /Admin/Categories
        public async Task<IActionResult> Categories()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            var categories = await _context.ProductCategories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            return View(categories);
        }

        // POST: /Admin/ToggleCategoryActive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        // GET: /Admin/ManageSupplierCategories/{id}
        public async Task<IActionResult> ManageSupplierCategories(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            var supplier = await _context.Suppliers
                .Include(s => s.SupplierCategories)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (supplier == null) return NotFound();

            var allCategories = await _context.ProductCategories.OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.AllCategories = allCategories;
            return View(supplier);
        }

        // POST: /Admin/UpdateSupplierCategories
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSupplierCategories(int supplierId, List<int> selectedCategoryIds)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            
            var existingCategories = _context.SupplierCategories.Where(sc => sc.SupplierId == supplierId);
            _context.SupplierCategories.RemoveRange(existingCategories);

            if (selectedCategoryIds != null)
            {
                foreach (var catId in selectedCategoryIds)
                {
                    _context.SupplierCategories.Add(new SupplierCategory { SupplierId = supplierId, CategoryId = catId });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Supplier categories updated successfully.";
            return RedirectToAction("SupplierDetails", new { id = supplierId });
        }

        // GET: /Admin/ManageRetailerCategories/{id}
        public async Task<IActionResult> ManageRetailerCategories(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            var retailer = await _context.Retailers
                .Include(r => r.RetailerCategories)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (retailer == null) return NotFound();

            var allCategories = await _context.ProductCategories.OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.AllCategories = allCategories;
            return View(retailer);
        }

        // POST: /Admin/UpdateRetailerCategories
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRetailerCategories(int retailerId, List<int> selectedCategoryIds)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var existingCategories = _context.RetailerCategories.Where(rc => rc.RetailerId == retailerId);
            _context.RetailerCategories.RemoveRange(existingCategories);

            if (selectedCategoryIds != null)
            {
                foreach (var catId in selectedCategoryIds)
                {
                    _context.RetailerCategories.Add(new RetailerCategory { RetailerId = retailerId, CategoryId = catId });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Retailer categories updated successfully.";
            return RedirectToAction("RetailerDetails", new { id = retailerId });
        }

        // POST: /Admin/BulkAssignCategories
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAssignCategories(List<int> userIds, List<int> categoryIds, string role)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            if (userIds == null || !userIds.Any() || categoryIds == null || !categoryIds.Any())
            {
                TempData["ErrorMessage"] = "Please select users and categories.";
                return RedirectToAction("Dashboard");
            }

            foreach (var userId in userIds)
            {
                if (role == "Supplier")
                {
                    var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (supplier != null)
                    {
                        foreach (var catId in categoryIds)
                        {
                            if (!await _context.SupplierCategories.AnyAsync(sc => sc.SupplierId == supplier.Id && sc.CategoryId == catId))
                            {
                                _context.SupplierCategories.Add(new SupplierCategory { SupplierId = supplier.Id, CategoryId = catId });
                            }
                        }
                    }
                }
                else if (role == "Retailer")
                {
                    var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
                    if (retailer != null)
                    {
                        foreach (var catId in categoryIds)
                        {
                            if (!await _context.RetailerCategories.AnyAsync(rc => rc.RetailerId == retailer.Id && rc.CategoryId == catId))
                            {
                                _context.RetailerCategories.Add(new RetailerCategory { RetailerId = retailer.Id, CategoryId = catId });
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Bulk category assignment completed for {userIds.Count} users.";
            return RedirectToAction("Dashboard");
        }

        // NEW ROUTES for Admin Modernization
        // GET: /Admin/CommissionOverview
        public async Task<IActionResult> CommissionOverview()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            
            var commissions = await _context.Commissions
                .Include(c => c.Supplier)
                .Include(c => c.Order)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // High-Level KPIs
            ViewBag.GrossSales = commissions.Where(c => c.PaymentType == "OrderPayment" && c.Status == PaymentStatus.Paid.ToString()).Sum(c => c.OrderAmount);
            ViewBag.PlatformRevenue = commissions.Where(c => c.PaymentType == "PlatformCommission" && c.Status == PaymentStatus.Paid.ToString()).Sum(c => c.CommissionAmount);
            ViewBag.SupplierPayables = commissions.Where(c => c.PaymentType == "SupplierPayout" && c.Status == PaymentStatus.Pending.ToString()).Sum(c => c.CommissionAmount);
            ViewBag.FailedPayments = commissions.Where(c => c.Status == PaymentStatus.Failed.ToString()).Count();

            // Chart Data: Status Breakdown
            var statusGroups = commissions.GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();
            ViewBag.StatusLabels = statusGroups.Select(g => g.Status).ToArray();
            ViewBag.StatusCounts = statusGroups.Select(g => g.Count).ToArray();

            // Chart Data: Revenue Trend (Last 30 Days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var trendData = commissions
                .Where(c => c.PaymentType == "PlatformCommission" && c.PaidAt >= thirtyDaysAgo)
                .GroupBy(c => c.PaidAt.Value.Date)
                .Select(g => new { Date = g.Key.ToString("MMM dd"), Amount = g.Sum(c => c.CommissionAmount) })
                .OrderBy(g => g.Date)
                .ToList();
            ViewBag.TrendLabels = trendData.Select(g => g.Date).ToArray();
            ViewBag.TrendAmounts = trendData.Select(g => g.Amount).ToArray();

            // Top Suppliers
            ViewBag.TopSuppliers = commissions
                .Where(c => c.PaymentType == "PlatformCommission")
                .GroupBy(c => c.Supplier?.CompanyName ?? "Unknown")
                .Select(g => new { Name = g.Key, Revenue = g.Sum(c => c.CommissionAmount) })
                .OrderByDescending(g => g.Revenue)
                .Take(5)
                .ToList();

            return View(commissions);
        }

        // GET: /Admin/Transactions
        public async Task<IActionResult> Transactions()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            var transactions = await _context.Commissions
                .Include(c => c.Retailer)
                .Include(c => c.Supplier)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(transactions);
        }

        // GET: /Admin/Payouts
        public async Task<IActionResult> Payouts()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");
            var payouts = await _context.Commissions
                .Include(c => c.Supplier)
                .Where(c => c.PaymentType == "SupplierPayout")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(payouts);
        }

        // GET: /Admin/FinancialReports
        public async Task<IActionResult> FinancialReports()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            try
            {
                // Summary Metrics
                ViewBag.TotalRevenue = await _context.Commissions
                    .Where(c => c.Status == "Paid")
                    .SumAsync(c => (decimal?)c.CommissionAmount) ?? 0;

                ViewBag.TotalOrderValue = await _context.Orders
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                ViewBag.PendingCommissionsValue = await _context.Commissions
                    .Where(c => c.Status == "Pending")
                    .SumAsync(c => (decimal?)c.CommissionAmount) ?? 0;

                ViewBag.TotalPayouts = await _context.Commissions
                    .Where(c => c.SupplierPayoutStatus == "Processed")
                    .SumAsync(c => (decimal?)c.SupplierPayoutAmount) ?? 0;

                // Monthly Data for Chart (Last 6 Months)
                var last6Months = Enumerable.Range(0, 6)
                    .Select(i => DateTime.Now.AddMonths(-i))
                    .OrderBy(d => d)
                    .ToList();

                var monthlyData = new List<decimal>();
                var labels = new List<string>();

                foreach (var month in last6Months)
                {
                    var revenue = await _context.Commissions
                        .Where(c => c.Status == "Paid" && c.PaidAt.HasValue && c.PaidAt.Value.Month == month.Month && c.PaidAt.Value.Year == month.Year)
                        .SumAsync(c => (decimal?)c.CommissionAmount) ?? 0;
                    
                    monthlyData.Add(revenue);
                    labels.Add(month.ToString("MMM yyyy"));
                }

                ViewBag.ChartLabels = labels;
                ViewBag.ChartData = monthlyData;

                // Recent Financial Movements
                var recentFinancials = await _context.Commissions
                    .Include(c => c.Supplier)
                    .Include(c => c.Order)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                return View(recentFinancials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FinancialReports Error");
                TempData["ErrorMessage"] = "Error loading financial data.";
                return View(new List<Commission>());
            }
        }

        // GET: /Admin/ExportCommissions
        public async Task<IActionResult> ExportCommissions()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var commissions = await _context.Commissions
                .Include(c => c.Supplier)
                .Include(c => c.Order)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Date,Supplier,OrderReference,Amount,Status,PaymentType,Notes");

            foreach (var c in commissions)
            {
                csv.AppendLine($"{c.Id},{c.CreatedAt:yyyy-MM-dd HH:mm},\"{c.Supplier?.CompanyName}\",\"{c.Order?.OrderNumber}\",{c.CommissionAmount},{c.Status},{c.PaymentType},\"{c.Notes?.Replace("\"", "'")}\"");
            }

            var fileName = $"Commission_Ledger_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        // GET: /Admin/ExportFinancials
        public async Task<IActionResult> ExportFinancials()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var financials = await _context.Commissions
                .Include(c => c.Supplier)
                .Include(c => c.Order)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Date,Supplier,OrderNumber,OrderAmount,CommissionAmount,Status,PaidAt,PaymentType");

            foreach (var item in financials)
            {
                csv.AppendLine($"{item.Id},{item.CreatedAt:yyyy-MM-dd HH:mm},\"{item.Supplier?.CompanyName}\",\"{item.Order?.OrderNumber}\",{item.OrderAmount},{item.CommissionAmount},{item.Status},{item.PaidAt:yyyy-MM-dd HH:mm},{item.PaymentType}");
            }

            var fileName = $"FinancialReport_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }


        // GET: /Admin/ExportAuditLogs
        public async Task<IActionResult> ExportAuditLogs()
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var logs = await _context.AuditLogs
                .Include(a => a.PerformedByUser)
                .OrderByDescending(a => a.PerformedAtUtc)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,UserId,User,Action,Entity,EntityId,Notes");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id},{log.PerformedAtUtc:yyyy-MM-dd HH:mm:ss},{log.PerformedByUserId},{log.PerformedByUser?.FullName ?? "N/A"},{log.ActionType},{log.EntityType},{log.EntityId},\"{log.Notes?.Replace("\"", "'")}\"");
            }

            var fileName = $"AuditLog_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        // GET: /Admin/Settings
        public async Task<IActionResult> Settings()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var sysConfigs = await _context.SystemConfigurations.ToDictionaryAsync(c => c.Key, c => c.Value);

            var viewModel = new AdminSettingsViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                ProfilePicture = user.ProfileImage,
                TwoFactorEnabled = user.TwoFactorEnabled,
                
                // Platform Config
                CommissionBronze = GetConfigDecimal(sysConfigs, "CommissionBronze", 2.0m),
                CommissionSilver = GetConfigDecimal(sysConfigs, "CommissionSilver", 1.5m),
                CommissionGold = GetConfigDecimal(sysConfigs, "CommissionGold", 1.0m),
                CommissionPlatinum = GetConfigDecimal(sysConfigs, "CommissionPlatinum", 0.5m),
                PenaltyWarningThreshold = GetConfigInt(sysConfigs, "PenaltyWarningThreshold", 3),
                PenaltySuspensionDays = GetConfigInt(sysConfigs, "PenaltySuspensionDays", 7),
                LowStockDefaultThreshold = GetConfigInt(sysConfigs, "LowStockDefaultThreshold", 10),
                MaxTenderDays = GetConfigInt(sysConfigs, "MaxTenderDays", 30),
                AutoReleaseEscrowDays = GetConfigInt(sysConfigs, "AutoReleaseEscrowDays", 5),

                // User Defaults
                RequireSupplierApproval = GetConfigBool(sysConfigs, "RequireSupplierApproval", true),
                RequireRetailerApproval = GetConfigBool(sysConfigs, "RequireRetailerApproval", true),
                DefaultAccountStatus = GetConfigString(sysConfigs, "DefaultAccountStatus", "Pending"),
                EnableFaydaVerification = GetConfigBool(sysConfigs, "EnableFaydaVerification", true),

                // System Settings
                AppUrl = GetConfigString(sysConfigs, "AppUrl", "https://ethiochain.com"),
                SupportEmail = GetConfigString(sysConfigs, "SupportEmail", "support@ethiochain.com"),
                PlatformLogo = GetConfigString(sysConfigs, "PlatformLogo", "/assets/logo.png"),
                Favicon = GetConfigString(sysConfigs, "Favicon", "/favicon.ico"),
                Timezone = GetConfigString(sysConfigs, "Timezone", "Africa/Addis_Ababa"),
                Currency = GetConfigString(sysConfigs, "Currency", "ETB"),
                DateFormat = GetConfigString(sysConfigs, "DateFormat", "dd MMM yyyy"),

                // Chapa Config
                ChapaSecretKey = GetConfigString(sysConfigs, "ChapaSecretKey", ""),
                ChapaWebhookSecret = GetConfigString(sysConfigs, "ChapaWebhookSecret", ""),
                ChapaEnvironment = GetConfigString(sysConfigs, "ChapaEnvironment", "Test"),
                ChapaTestMode = GetConfigBool(sysConfigs, "ChapaTestMode", true),

                // Stats
                TotalUsers = await _context.Users.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
                TotalCommission = await _context.Commissions.SumAsync(c => (decimal?)c.CommissionAmount) ?? 0,
                PendingApprovals = await _context.Users.CountAsync(u => !u.IsApproved && u.AccountStatus != "Rejected"),
                
                ActiveSessions = await _context.UserSessions.Where(s => s.UserId == userId && s.IsActive).OrderByDescending(s => s.LastActivityTime).ToListAsync(),
                LoginHistory = await _context.AuditLogs.Where(l => l.PerformedByUserId == userId && l.ActionType == "Login").OrderByDescending(l => l.PerformedAtUtc).Take(10).ToListAsync(),
                EmailTemplates = await _context.EmailTemplates.ToListAsync()
            };

            // 2FA Setup
            if (!user.TwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    user.TwoFactorSecret = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10);
                    await _context.SaveChangesAsync();
                }

                TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                var setupInfo = tfa.GenerateSetupCode("EthioChain SCM", user.Email, user.TwoFactorSecret, false, 3);
                ViewBag.QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl;
                ViewBag.ManualSetupKey = setupInfo.ManualEntryKey;
            }

            return View(viewModel);
        }

        // POST: /Admin/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(AdminSettingsViewModel model)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            // Profile
            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;

            // Password
            if (!string.IsNullOrEmpty(model.NewPassword) && !string.IsNullOrEmpty(model.CurrentPassword))
            {
                if (VerifyPasswordHash(model.CurrentPassword, user.PasswordHash))
                {
                    user.PasswordHash = HashPassword(model.NewPassword);
                }
                else
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction("Settings");
                }
            }

            // Profile Picture Upload
            if (model.ProfilePictureFile != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "admins");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = $"admin_{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(model.ProfilePictureFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePictureFile.CopyToAsync(fileStream);
                }
                user.ProfileImage = $"/uploads/admins/{uniqueFileName}";
            }

            // Save Configs
            await SaveOrUpdateConfig("CommissionBronze", model.CommissionBronze.ToString(), "decimal");
            await SaveOrUpdateConfig("CommissionSilver", model.CommissionSilver.ToString(), "decimal");
            await SaveOrUpdateConfig("CommissionGold", model.CommissionGold.ToString(), "decimal");
            await SaveOrUpdateConfig("CommissionPlatinum", model.CommissionPlatinum.ToString(), "decimal");
            await SaveOrUpdateConfig("PenaltyWarningThreshold", model.PenaltyWarningThreshold.ToString(), "int");
            await SaveOrUpdateConfig("PenaltySuspensionDays", model.PenaltySuspensionDays.ToString(), "int");
            await SaveOrUpdateConfig("LowStockDefaultThreshold", model.LowStockDefaultThreshold.ToString(), "int");
            await SaveOrUpdateConfig("MaxTenderDays", model.MaxTenderDays.ToString(), "int");
            await SaveOrUpdateConfig("AutoReleaseEscrowDays", model.AutoReleaseEscrowDays.ToString(), "int");

            await SaveOrUpdateConfig("RequireSupplierApproval", model.RequireSupplierApproval.ToString(), "bool");
            await SaveOrUpdateConfig("RequireRetailerApproval", model.RequireRetailerApproval.ToString(), "bool");
            await SaveOrUpdateConfig("DefaultAccountStatus", model.DefaultAccountStatus, "string");
            await SaveOrUpdateConfig("EnableFaydaVerification", model.EnableFaydaVerification.ToString(), "bool");

            await SaveOrUpdateConfig("AppUrl", model.AppUrl ?? "", "string");
            await SaveOrUpdateConfig("SupportEmail", model.SupportEmail ?? "", "string");
            await SaveOrUpdateConfig("Timezone", model.Timezone ?? "", "string");
            await SaveOrUpdateConfig("Currency", model.Currency ?? "", "string");
            await SaveOrUpdateConfig("DateFormat", model.DateFormat ?? "", "string");

            await SaveOrUpdateConfig("ChapaSecretKey", model.ChapaSecretKey ?? "", "string");
            await SaveOrUpdateConfig("ChapaWebhookSecret", model.ChapaWebhookSecret ?? "", "string");
            await SaveOrUpdateConfig("ChapaEnvironment", model.ChapaEnvironment ?? "", "string");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Admin settings updated successfully.";
            return RedirectToAction("Settings");
        }


        // POST: /Admin/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string confirmPassword)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(confirmPassword) || !VerifyPasswordHash(confirmPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Invalid password. Account deletion aborted.";
                return RedirectToAction(nameof(Settings));
            }

            // Deactivate account instead of hard delete for audit purposes
            user.AccountStatus = "Deleted";
            user.IsApproved = false;
            
            await _context.SaveChangesAsync();

            // Clear session and logout
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Your account has been deactivated and marked for deletion.";
            return RedirectToAction("Login", "Account");
        }

        // POST: /Admin/Verify2FA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify2FA(string pin)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            bool isValid = tfa.ValidateTwoFactorPIN(user.TwoFactorSecret, pin);

            if (isValid)
            {
                user.TwoFactorEnabled = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Two-Factor Authentication has been enabled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid PIN. Please try again.";
            }

            return RedirectToAction(nameof(Settings));
        }

        // POST: /Admin/RevokeSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSession(int sessionId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Session revoked successfully." });
            }

            return Json(new { success = false, message = "Session not found or access denied." });
        }

        private bool VerifyPasswordHash(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        // Helpers
        private decimal GetConfigDecimal(Dictionary<string, string?> configs, string key, decimal defaultVal)
            => configs.TryGetValue(key, out var val) && decimal.TryParse(val, out var res) ? res : defaultVal;
        private int GetConfigInt(Dictionary<string, string?> configs, string key, int defaultVal)
            => configs.TryGetValue(key, out var val) && int.TryParse(val, out var res) ? res : defaultVal;
        private bool GetConfigBool(Dictionary<string, string?> configs, string key, bool defaultVal)
            => configs.TryGetValue(key, out var val) && bool.TryParse(val, out var res) ? res : defaultVal;
        private string GetConfigString(Dictionary<string, string?> configs, string key, string defaultVal)
            => configs.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val) ? val : defaultVal;

        private async Task SaveOrUpdateConfig(string key, string value, string dataType)
        {
            var config = await _context.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == key);
            if (config != null)
            {
                config.Value = value;
            }
            else
            {
                _context.SystemConfigurations.Add(new SystemConfiguration { Key = key, Value = value, DataType = dataType });
            }
        }

        // AJAX: ToggleTestMode
        [HttpPost]
        public async Task<IActionResult> ToggleTestMode(bool isTestMode)
        {
            if (!await IsAdminAndPopulateNotifications()) return Unauthorized();
            await SaveOrUpdateConfig("ChapaTestMode", isTestMode.ToString(), "bool");
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // AJAX: GenerateApiKey
        [HttpPost]
        public async Task<IActionResult> GenerateApiKey()
        {
            if (!await IsAdminAndPopulateNotifications()) return Unauthorized();
            var newKey = "SCM_" + Guid.NewGuid().ToString("N").ToUpper();
            return Json(new { success = true, key = newKey });
        }

        // AJAX: SendTestEmail
        [HttpPost]
        public async Task<IActionResult> SendTestEmail(string email)
        {
            if (!await IsAdminAndPopulateNotifications()) return Unauthorized();
            try
            {
                await _emailService.SendApprovalEmailAsync(email, "Test User", "Admin");
                return Json(new { success = true });
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Admin/UserDetails/{id}
        public async Task<IActionResult> UserDetails(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("AllUsers");
            }

            if (user.Role == "Supplier")
            {
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == id);
                if (supplier != null) return RedirectToAction("SupplierDetails", new { id = supplier.Id });
            }
            else if (user.Role == "Retailer")
            {
                var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == id);
                if (retailer != null) return RedirectToAction("RetailerDetails", new { id = retailer.Id });
            }

            // Fallback for roles like Admin, DeliveryAgent, etc., or users with empty roles
            return View(user);
        }

        // POST: /Admin/SuspendUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("AllUsers");
            }

            user.AccountStatus = "Suspended";
            user.IsApproved = false;

            await LogAudit(user.Id, "Suspended", $"Admin suspended user {user.FullName}.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User {user.FullName} has been suspended.";
            return RedirectToAction("AllUsers");
        }

        // POST: /Admin/RejectFayda
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectFayda(int id)
        {
            if (!await IsAdminAndPopulateNotifications()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("AllUsers");
            }

            user.FaydaStatus = "Rejected";
            user.IsFaydaVerified = false;

            await LogAudit(user.Id, "Rejected", $"Admin rejected Fayda identity for {user.FullName}.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Fayda identity for {user.FullName} has been rejected.";
            return RedirectToAction("AllUsers");
        }
    }

    public class RejectRequest
    {
        public int UserId { get; set; }
        public string UserType { get; set; }
        public string RejectionReason { get; set; }
        public string AdditionalComments { get; set; }
        public bool SendEmail { get; set; }
    }
}