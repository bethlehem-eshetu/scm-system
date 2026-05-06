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
        ILogger<AdminController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly INotificationService _notificationService = notificationService;
        private readonly SCM_System.Services.IFaydaService _faydaService = faydaService;
        private readonly SCM_System.Services.IEmailService _emailService = emailService;
        private readonly ILogger<AdminController> _logger = logger;

        // Helper method to check if user is admin
        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            var user = _context.Users.Find(userId);
            return user != null && user.Role == "Admin";
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
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
            if (!IsAdmin()) return Unauthorized();

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

        // GET: /Admin/PendingSuppliers
        public async Task<IActionResult> PendingSuppliers()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Get pending suppliers and convert to ViewModel
                var pendingSuppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => s.VerificationStatus == "Pending")
                    .Select(s => new PendingSupplierViewModel
                    {
                        Id = s.Id,
                        CompanyName = s.CompanyName,
                        BusinessType = s.BusinessType,
                        LicenseNumber = s.LicenseNumber ?? string.Empty,
                        LicenseFilePath = s.LicenseFilePath,
                        TaxIdentificationNumber = s.TaxIdentificationNumber ?? string.Empty,
                        CompanyAddress = s.CompanyAddress ?? string.Empty,
                        City = s.City,
                        Country = s.Country,
                        Website = s.Website ?? string.Empty,
                        Description = s.Description ?? string.Empty,
                        CreatedAt = s.CreatedAt,
                        FullName = s.User != null ? s.User.FullName : string.Empty,
                        Email = s.User != null ? s.User.Email : string.Empty,
                        PhoneNumber = s.User != null ? s.User.PhoneNumber : string.Empty,
                        IsFaydaVerified = s.User != null && s.User.IsFaydaVerified,
                        FaydaStatus = s.User != null ? s.User.FaydaStatus : "N/A"
                    })
                    .ToListAsync();

                // Get pending retailers and convert to ViewModel - REMOVED VerificationStatus
                var pendingRetailers = await _context.Retailers
                    .Include(r => r.User)
                    .Where(r => r.User != null && !r.User.IsApproved && r.User.AccountStatus != "Rejected")
                    .Select(r => new PendingRetailerViewModel
                    {
                        Id = r.Id,
                        BusinessName = r.BusinessName,
                        BusinessType = r.BusinessType,
                        BusinessLicenseNumber = r.BusinessLicenseNumber ?? string.Empty,
                        TaxIdentificationNumber = r.TaxIdentificationNumber ?? string.Empty,
                        BusinessAddress = r.BusinessAddress ?? string.Empty,
                        City = r.City,
                        Country = r.Country,
                        StoreSize = r.StoreSize ?? string.Empty,
                        Description = r.Description ?? string.Empty,
                        CreatedAt = r.CreatedAt,
                        FullName = r.User != null ? r.User.FullName : string.Empty,
                        Email = r.User != null ? r.User.Email : string.Empty,
                        PhoneNumber = r.User != null ? r.User.PhoneNumber : string.Empty,
                        IsFaydaVerified = r.User != null && r.User.IsFaydaVerified,
                        FaydaStatus = r.User != null ? r.User.FaydaStatus : "N/A"
                    })
                    .ToListAsync();

                ViewBag.PendingRetailers = pendingRetailers;
                ViewBag.PendingSuppliersCount = pendingSuppliers.Count;
                ViewBag.PendingRetailersCount = pendingRetailers.Count;

                return View(pendingSuppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PendingSuppliers Error");
                TempData["ErrorMessage"] = "Error loading pending approvals.";
                return View(new List<PendingSupplierViewModel>());
            }
        }

        // GET: /Admin/ViewLicense/{id}
        public async Task<IActionResult> ViewLicense(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null || string.IsNullOrEmpty(supplier.LicenseFilePath))
                {
                    TempData["ErrorMessage"] = "No license file is attached for this supplier.";
                    return RedirectToAction("SupplierDetails", new { id = id });
                }

                string filePath = Path.Combine(_webHostEnvironment.WebRootPath,
                    supplier.LicenseFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "The license document file was not found on the server.";
                    return RedirectToAction("SupplierDetails", new { id = id });
                }

                string fileExtension = Path.GetExtension(filePath).ToLower();
                string contentType = fileExtension switch
                {
                    ".pdf" => "application/pdf",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                };

                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewLicense Error");
                return NotFound("Error loading license file.");
            }
        }

        // POST: /Admin/ApproveSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSupplier(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Find the supplier with user
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "❌ Supplier not found.";
                    return RedirectToAction("PendingSuppliers");
                }

                if (supplier.User == null)
                {
                    TempData["ErrorMessage"] = "❌ Associated user not found for this supplier.";
                    return RedirectToAction("PendingSuppliers");
                }

                // Update supplier
                supplier.VerificationStatus = "Verified";

                // Update user
                supplier.User.IsApproved = true;
                supplier.User.AccountStatus = "Active";

                // Create notification for supplier
                var supplierNotification = new Notification
                {
                    UserId = supplier.UserId,
                    Title = "✅ Account Approved",
                    Message = $"Congratulations! Your supplier account for '{supplier.CompanyName}' has been approved! You can now login to the system.",
                    Type = "Success",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    ActionUrl = "/Account/Login"
                };
                _context.Notifications.Add(supplierNotification);

                // Create notification for admin
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser != null)
                {
                    var adminNotification = new Notification
                    {
                        UserId = adminUser.Id,
                        Title = "✅ Supplier Approved",
                        Message = $"You approved supplier '{supplier.CompanyName}'.",
                        Type = "Info",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        ActionUrl = "/Admin/PendingUsers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                await _context.SaveChangesAsync();

                // Send Email Notification (Non-blocking)
                try
                {
                    await _emailService.SendApprovalEmailAsync(supplier.User.Email, supplier.User.FullName, "Supplier");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send approval email to {Email}", supplier.User.Email);
                }

                TempData["SuccessMessage"] = $"✅ Supplier '{supplier.CompanyName}' has been approved successfully!";
                Console.WriteLine($"✅ Supplier {id} approved successfully - User Approved: {supplier.User.IsApproved}, Supplier Verified: {supplier.VerificationStatus}");

                return RedirectToAction("PendingSuppliers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving supplier {SupplierId}", id);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner error: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "❌ An error occurred while approving the supplier. Please try again.";
                return RedirectToAction("PendingSuppliers");
            }
        }

        // POST: /Admin/ApproveRetailer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRetailer(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Find the retailer with user
                var retailer = await _context.Retailers
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (retailer == null)
                {
                    TempData["ErrorMessage"] = "❌ Retailer not found.";
                    return RedirectToAction("AllRetailers");
                }

                if (retailer.User == null)
                {
                    TempData["ErrorMessage"] = "❌ Associated user not found for this retailer.";
                    return RedirectToAction("AllRetailers");
                }

                // Update User
                retailer.User.IsApproved = true;
                retailer.User.AccountStatus = "Active";

                // Update Retailer
                retailer.IsVerified = true;

                // Create notification for retailer
                var retailerNotification = new Notification
                {
                    UserId = retailer.UserId,
                    Title = "✅ Account Approved",
                    Message = $"Congratulations! Your retailer account for '{retailer.BusinessName}' has been approved! You can now login to the system.",
                    Type = "Success",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    ActionUrl = "/Account/Login"
                };
                _context.Notifications.Add(retailerNotification);

                // Create notification for admin
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser != null)
                {
                    var adminNotification = new Notification
                    {
                        UserId = adminUser.Id,
                        Title = "✅ Retailer Approved",
                        Message = $"You approved retailer '{retailer.BusinessName}'.",
                        Type = "Info",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        ActionUrl = "/Admin/PendingUsers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                await _context.SaveChangesAsync();

                // Send Email Notification (Non-blocking)
                try
                {
                    await _emailService.SendApprovalEmailAsync(retailer.User.Email, retailer.User.FullName, "Retailer");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send approval email to {Email}", retailer.User.Email);
                }

                TempData["SuccessMessage"] = $"✅ Retailer '{retailer.BusinessName}' has been approved successfully!";
                Console.WriteLine($"✅ Retailer {id} approved successfully - User Approved: {retailer.User.IsApproved}, Retailer Verified: {retailer.IsVerified}");

                return RedirectToAction("RetailerDetails", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving retailer {RetailerId}", id);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner error: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "❌ An error occurred while approving the retailer. Please try again.";
                return RedirectToAction("RetailerDetails", new { id });
            }
        }

        // POST: /Admin/RejectSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSupplier(int id, string rejectionReason)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "❌ Supplier not found.";
                    return RedirectToAction("PendingSuppliers");
                }

                if (supplier.User == null)
                {
                    TempData["ErrorMessage"] = "❌ Associated user not found for this supplier.";
                    return RedirectToAction("PendingSuppliers");
                }

                // Validate rejection reason
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["ErrorMessage"] = "❌ Rejection reason is required.";
                    return RedirectToAction("SupplierDetails", new { id });
                }

                // Update BOTH the User AND the Supplier
                supplier.VerificationStatus = "Rejected";
                supplier.User.IsApproved = false;
                supplier.User.AccountStatus = "Rejected";

                // Create notification for supplier
                var notification = new Notification
                {
                    UserId = supplier.UserId,
                    Title = "❌ Account Rejected",
                    Message = $"Your supplier account for '{supplier.CompanyName}' has been rejected. Reason: {rejectionReason}",
                    Type = "Error",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    ActionUrl = null
                };
                _context.Notifications.Add(notification);

                // Create notification for admin
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser != null)
                {
                    var adminNotification = new Notification
                    {
                        UserId = adminUser.Id,
                        Title = "❌ Supplier Rejected",
                        Message = $"You rejected supplier '{supplier.CompanyName}'. Reason: {rejectionReason}",
                        Type = "Info",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        ActionUrl = "/Admin/PendingUsers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                await _context.SaveChangesAsync();

                // Send Email Notification (Non-blocking)
                try
                {
                    await _emailService.SendRejectionEmailAsync(supplier.User.Email, supplier.User.FullName, "Supplier", rejectionReason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send rejection email to {Email}", supplier.User.Email);
                }

                TempData["SuccessMessage"] = $"✅ Supplier '{supplier.CompanyName}' has been rejected.";
                Console.WriteLine($"✅ Supplier {id} rejected successfully - User Approved: {supplier.User.IsApproved}, Supplier Status: {supplier.VerificationStatus}");

                return RedirectToAction("SupplierDetails", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting supplier {SupplierId}", id);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner error: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "❌ An error occurred while rejecting the supplier.";
                return RedirectToAction("SupplierDetails", new { id });
            }
        }

        // POST: /Admin/RejectRetailer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRetailer(int id, string rejectionReason)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Find the retailer with user - IMPORTANT: Include User
                var retailer = await _context.Retailers
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (retailer == null)
                {
                    TempData["ErrorMessage"] = "❌ Retailer not found.";
                    return RedirectToAction("AllRetailers");
                }

                if (retailer.User == null)
                {
                    TempData["ErrorMessage"] = "❌ Associated user not found for this retailer.";
                    return RedirectToAction("AllRetailers");
                }

                // Validate rejection reason
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["ErrorMessage"] = "❌ Rejection reason is required.";
                    return RedirectToAction("RetailerDetails", new { id });
                }

                // IMPORTANT: Log before changes for debugging
                Console.WriteLine($"Before Rejection - User ID: {retailer.User.Id}, IsApproved: {retailer.User.IsApproved}, AccountStatus: {retailer.User.AccountStatus}");

                // Update User properties (THIS IS WHAT CONTROLS THE STATUS)
                retailer.User.IsApproved = false;
                retailer.User.AccountStatus = "Rejected";

                // Update Retailer properties
                retailer.IsVerified = false;

                // IMPORTANT: Also update User's Approval status in the database
                _context.Entry(retailer.User).State = EntityState.Modified;
                _context.Entry(retailer).State = EntityState.Modified;

                // Create notification for retailer
                var notification = new Notification
                {
                    UserId = retailer.UserId,
                    Title = "❌ Account Rejected",
                    Message = $"Your retailer account for '{retailer.BusinessName}' has been rejected. Reason: {rejectionReason}",
                    Type = "Error",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    ActionUrl = null
                };
                _context.Notifications.Add(notification);

                // Create notification for admin
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser != null)
                {
                    var adminNotification = new Notification
                    {
                        UserId = adminUser.Id,
                        Title = "❌ Retailer Rejected",
                        Message = $"You rejected retailer '{retailer.BusinessName}'. Reason: {rejectionReason}",
                        Type = "Info",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        ActionUrl = "/Admin/PendingUsers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                // Save changes
                await _context.SaveChangesAsync();

                // Send Email Notification (Non-blocking)
                try
                {
                    await _emailService.SendRejectionEmailAsync(retailer.User.Email, retailer.User.FullName, "Retailer", rejectionReason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send rejection email to {Email}", retailer.User.Email);
                }

                // IMPORTANT: Log after changes to verify
                Console.WriteLine($"After Rejection - User ID: {retailer.User.Id}, IsApproved: {retailer.User.IsApproved}, AccountStatus: {retailer.User.AccountStatus}");

                TempData["SuccessMessage"] = $"✅ Retailer '{retailer.BusinessName}' has been rejected successfully.";

                // Redirect to the same details page to see the updated status
                return RedirectToAction("RetailerDetails", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting retailer {RetailerId}", id);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner error: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "❌ An error occurred while rejecting the retailer. Please try again.";
                return RedirectToAction("RetailerDetails", new { id });
            }
        }

        // GET: /Admin/PendingUsers
        public async Task<IActionResult> PendingUsers()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var pendingUsers = await _context.Users
                    .Include(u => u.FaydaVerification)
                    .Include(u => u.Supplier)
                    .Include(u => u.Retailer)
                    .Where(u => !u.IsApproved && u.Role != "Admin" && u.AccountStatus != "Rejected")
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return View(pendingUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending users");
                TempData["ErrorMessage"] = "Error loading pending users.";
                return View(new List<User>());
            }
        }

        // POST: /Admin/ApproveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .Include(u => u.FaydaVerification)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null) return NotFound();

            var adminId = HttpContext.Session.GetInt32("UserId") ?? 0;

            user.IsApproved = true;
            user.IsFaydaVerified = true; // Critical Fix: Ensure user can log in
            user.FaydaStatus = "Verified";
            user.AccountStatus = "Active";
            user.ApprovedAt = DateTime.Now;
            user.ApprovalStatus = "Approved";
            user.ApprovalStatusType = "Approved";
            user.ApprovalStatusMessage = "Your account has been approved! You can now access all platform features.";

            // Sync with FaydaVerification table if record exists
            if (user.FaydaVerification != null)
            {
                user.FaydaVerification.IsVerified = true;
                user.FaydaVerification.VerifiedName = user.FullName;
            }
            else if (!string.IsNullOrEmpty(user.FAN))
            {
                // Optionally create missing verification record
                var newVerification = new FaydaVerification
                {
                    FAN = user.FAN,
                    UserEmail = user.Email,
                    IsVerified = true,
                    VerifiedName = user.FullName,
                    VerifiedPhone = user.PhoneNumber,
                    TransactionId = "ADMIN_APPROVAL_" + Guid.NewGuid().ToString().Substring(0, 8)
                };
                _context.FaydaVerifications.Add(newVerification);
            }

            // Update associated roles
            if (user.Role == "Supplier")
            {
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (supplier != null) supplier.VerificationStatus = "Verified";
            }
            else if (user.Role == "Retailer")
            {
                var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == user.Id);
                if (retailer != null) retailer.IsVerified = true;
            }

            // Create Audit Log
            await LogAudit(user.Id, "Approved", null);

            await _context.SaveChangesAsync();

            // Send Email Notification (Non-blocking)
            try
            {
                await _emailService.SendApprovalEmailAsync(user.Email, user.FullName, user.Role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval email to {Email}", user.Email);
            }

            TempData["SuccessMessage"] = $"✅ User {user.FullName} approved successfully!";
            return RedirectToAction("PendingUsers");
        }

        // POST: /Admin/RejectUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(int id, string reason)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "❌ Rejection reason is required.";
                return RedirectToAction("PendingUsers");
            }

            user.IsApproved = false;
            user.AccountStatus = "Rejected";
            user.RejectionReason = reason;
            user.ApprovalStatus = "Rejected";
            user.ApprovalStatusType = "Rejected";
            user.ApprovalStatusMessage = $"Your account was rejected. Reason: {reason}";

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

            // Create Audit Log
            await LogAudit(user.Id, "Rejected", reason);

            await _context.SaveChangesAsync();

            // Send Email Notification (Non-blocking)
            try
            {
                await _emailService.SendRejectionEmailAsync(user.Email, user.FullName, user.Role, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection email to {Email}", user.Email);
            }

            TempData["SuccessMessage"] = $"❌ User {user.FullName} rejected.";
            return RedirectToAction("PendingUsers");
        }

        // POST: /Admin/RejectApplication (JSON Endpoint for Generic Rejections)
        [HttpPost]
        [Route("Admin/RejectApplication")]
        public async Task<IActionResult> RejectApplication([FromBody] RejectRequest request)
        {
            try
            {
                if (!IsAdmin()) return Unauthorized(new { message = "Unauthorized access." });

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

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
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

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
        public async Task<IActionResult> RejectUser([FromBody] RejectUserRequest request)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null) return Json(new { success = false, message = "User not found" });

                user.AccountStatus = "Rejected";
                user.RejectionReason = request.Reason;

                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "❌ Account Rejected",
                    Message = $"Your account application was rejected. Reason: {request.Reason}",
                    Type = "Error",
                    CreatedAt = DateTime.Now
                });

                if (request.SendEmail)
                {
                    await _emailService.SendRejectionEmailAsync(user.Email, user.FullName, user.Role, request.Reason);
                }

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
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

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
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

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
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

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
            if (!IsAdmin()) return Unauthorized();

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

            return Json(new { totalCount, users });
        }

        [HttpGet]
        public async Task<IActionResult> ExportUsers(string format = "csv")
        {
            if (!IsAdmin()) return Unauthorized();

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
            if (!IsAdmin())
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
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .Include(s => s.Products)
                .Include(s => s.PurchaseOrders)
                    .ThenInclude(po => po.PurchaseOrderItems)
                .Include(s => s.Tenders)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
            {
                return View((SCM_System.Models.Entities.Supplier)null);
            }

            return View(supplier);
        }

        // GET: /Admin/RetailerDetails/{id}
        public async Task<IActionResult> RetailerDetails(int id)
        {
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
        public IActionResult Reports()
        {
            if (!IsAdmin())
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
        // GET: /Admin/MessageLog (Enhanced with filtering)
        public async Task<IActionResult> MessageLog(
            string senderName = null,
            string senderRole = null,
            string dateFilter = "all",
            DateTime? startDate = null,
            DateTime? endDate = null,
            string keyword = null,
            int? minLength = null,
            int page = 1,
            int pageSize = 20)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                // Start with base query - get messages that are NOT blocked
                var messagesQuery = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Conversation)
                        .ThenInclude(c => c.Supplier)
                            .ThenInclude(s => s.User)
                    .Include(m => m.Conversation)
                        .ThenInclude(c => c.Retailer)
                            .ThenInclude(r => r.User)
                    .Where(m => !m.IsBlocked) // Only show non-blocked messages
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(senderName))
                {
                    messagesQuery = messagesQuery.Where(m =>
                        m.Sender.FullName.Contains(senderName) ||
                        (m.Sender.Email != null && m.Sender.Email.Contains(senderName)));
                }

                if (!string.IsNullOrEmpty(senderRole))
                {
                    messagesQuery = messagesQuery.Where(m => m.Sender.Role == senderRole);
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    messagesQuery = messagesQuery.Where(m =>
                        m.MessageText != null && m.MessageText.Contains(keyword));
                }

                if (minLength.HasValue && minLength.Value > 0)
                {
                    messagesQuery = messagesQuery.Where(m =>
                        m.MessageText != null && m.MessageText.Length >= minLength.Value);
                }

                // Date filtering
                var today = DateTime.Today;
                switch (dateFilter)
                {
                    case "today":
                        messagesQuery = messagesQuery.Where(m => m.CreatedAt.Date == today);
                        break;
                    case "yesterday":
                        messagesQuery = messagesQuery.Where(m => m.CreatedAt.Date == today.AddDays(-1));
                        break;
                    case "last7days":
                        messagesQuery = messagesQuery.Where(m => m.CreatedAt.Date >= today.AddDays(-7));
                        break;
                    case "last30days":
                        messagesQuery = messagesQuery.Where(m => m.CreatedAt.Date >= today.AddDays(-30));
                        break;
                    case "custom":
                        if (startDate.HasValue)
                            messagesQuery = messagesQuery.Where(m => m.CreatedAt.Date >= startDate.Value.Date);
                        if (endDate.HasValue)
                            messagesQuery = messagesQuery.Where(m => m.CreatedAt.Date <= endDate.Value.Date);
                        break;
                }

                // Get counts for stats cards
                ViewBag.TotalMessagesCount = await _context.Messages.CountAsync();
                ViewBag.BlockedMessagesCount = await _context.Messages.CountAsync(m => m.IsBlocked);
                ViewBag.ActivePenaltiesCount = await _context.Penalties.CountAsync(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));

                // Execute query and get results
                var filteredMessages = await messagesQuery
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                ViewBag.FilteredCount = filteredMessages.Count;

                // Pagination
                var totalItems = filteredMessages.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var pagedMessages = filteredMessages
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Map to ViewModel
                var viewModels = pagedMessages.Select(m => new AdminMessageViewModel
                {
                    MessageId = m.Id,
                    SenderName = m.Sender?.FullName ?? "Unknown User",
                    SenderRole = m.Sender?.Role ?? "Unknown",
                    Content = m.MessageText ?? string.Empty,
                    SentAt = m.CreatedAt,
                    IsRead = m.IsRead,
                    ConversationId = m.ConversationId,
                    ConversationBetween = GetConversationBetween(m.Conversation)
                }).ToList();

                // Store filter values for the view
                ViewBag.CurrentFilters = new
                {
                    SenderName = senderName,
                    SenderRole = senderRole,
                    DateFilter = dateFilter,
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd"),
                    Keyword = keyword,
                    MinLength = minLength
                };

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasFilters = !string.IsNullOrEmpty(senderName) ||
                                    !string.IsNullOrEmpty(senderRole) ||
                                    dateFilter != "all" ||
                                    !string.IsNullOrEmpty(keyword) ||
                                    (minLength.HasValue && minLength.Value > 0);

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MessageLog Error");
                TempData["ErrorMessage"] = "Error loading message logs.";
                return View(new List<AdminMessageViewModel>());
            }
        }

        // GET: /Admin/BlockedMessages
        // GET: /Admin/BlockedMessages (Enhanced with filtering)
        public async Task<IActionResult> BlockedMessages(
            string senderName = null,
            string senderRole = null,
            string violationType = null,
            string dateFilter = "all",
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isResolved = null,
            int page = 1,
            int pageSize = 20)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                var violationsQuery = _context.MessageViolations
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

                // Apply filters
                if (!string.IsNullOrEmpty(senderName))
                {
                    violationsQuery = violationsQuery.Where(v =>
                        v.Message.Sender.FullName.Contains(senderName));
                }

                if (!string.IsNullOrEmpty(senderRole))
                {
                    violationsQuery = violationsQuery.Where(v =>
                        v.Message.Sender.Role == senderRole);
                }

                if (!string.IsNullOrEmpty(violationType))
                {
                    violationsQuery = violationsQuery.Where(v =>
                        v.ViolationType.Contains(violationType));
                }

                if (isResolved.HasValue)
                {
                    violationsQuery = violationsQuery.Where(v => v.IsResolved == isResolved.Value);
                }

                // Date filtering
                var today = DateTime.Today;
                switch (dateFilter)
                {
                    case "today":
                        violationsQuery = violationsQuery.Where(v => v.CreatedAt.Date == today);
                        break;
                    case "yesterday":
                        violationsQuery = violationsQuery.Where(v => v.CreatedAt.Date == today.AddDays(-1));
                        break;
                    case "last7days":
                        violationsQuery = violationsQuery.Where(v => v.CreatedAt.Date >= today.AddDays(-7));
                        break;
                    case "last30days":
                        violationsQuery = violationsQuery.Where(v => v.CreatedAt.Date >= today.AddDays(-30));
                        break;
                    case "custom":
                        if (startDate.HasValue)
                            violationsQuery = violationsQuery.Where(v => v.CreatedAt.Date >= startDate.Value.Date);
                        if (endDate.HasValue)
                            violationsQuery = violationsQuery.Where(v => v.CreatedAt.Date <= endDate.Value.Date);
                        break;
                }

                // Execute query
                var filteredViolations = await violationsQuery
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                ViewBag.FilteredCount = filteredViolations.Count;

                // Pagination
                var totalItems = filteredViolations.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var pagedViolations = filteredViolations
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Map to ViewModel
                var viewModels = pagedViolations.Select(v => new BlockedMessageViewModel
                {
                    ViolationId = v.Id,
                    MessageId = v.Message.Id,
                    SenderName = v.Message.Sender?.FullName ?? "Unknown",
                    SenderRole = v.Message.Sender?.Role ?? "Unknown",
                    Content = v.Message.MessageText ?? string.Empty,
                    ViolationType = v.ViolationType,
                    CreatedAt = v.CreatedAt,
                    IsResolved = v.IsResolved,
                    ConversationId = v.Message.ConversationId,
                    ConversationBetween = GetConversationBetween(v.Message.Conversation)
                }).ToList();

                // Store filter values for the view
                ViewBag.CurrentFilters = new
                {
                    SenderName = senderName,
                    SenderRole = senderRole,
                    ViolationType = violationType,
                    DateFilter = dateFilter,
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd"),
                    IsResolved = isResolved
                };

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasFilters = !string.IsNullOrEmpty(senderName) ||
                                    !string.IsNullOrEmpty(senderRole) ||
                                    !string.IsNullOrEmpty(violationType) ||
                                    dateFilter != "all" ||
                                    isResolved.HasValue;

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlockedMessages Error");
                TempData["ErrorMessage"] = "Error loading blocked messages.";
                return View(new List<BlockedMessageViewModel>());
            }
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin())
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        // GET: /Admin/ManageSupplierCategories/{id}
        public async Task<IActionResult> ManageSupplierCategories(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return Unauthorized();
            await SaveOrUpdateConfig("ChapaTestMode", isTestMode.ToString(), "bool");
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // AJAX: GenerateApiKey
        [HttpPost]
        public IActionResult GenerateApiKey()
        {
            if (!IsAdmin()) return Unauthorized();
            var newKey = "SCM_" + Guid.NewGuid().ToString("N").ToUpper();
            return Json(new { success = true, key = newKey });
        }

        // AJAX: SendTestEmail
        [HttpPost]
        public async Task<IActionResult> SendTestEmail(string email)
        {
            if (!IsAdmin()) return Unauthorized();
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

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