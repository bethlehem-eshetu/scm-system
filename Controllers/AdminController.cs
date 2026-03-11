using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace SCM_System.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Helper method to check if user is admin
        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            var user = _context.Users.Find(userId);
            return user != null && user.Role == "Admin";
        }

        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "⚠️ Please login as admin to access the dashboard.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Supplier Statistics
                ViewBag.TotalSuppliers = _context.Suppliers.Count();
                ViewBag.PendingSuppliers = _context.Suppliers.Count(s => s.VerificationStatus == "Pending");
                ViewBag.VerifiedSuppliers = _context.Suppliers.Count(s => s.VerificationStatus == "Verified");
                ViewBag.RejectedSuppliers = _context.Suppliers.Count(s => s.VerificationStatus == "Rejected");

                // Retailer Statistics - Using User properties instead of VerificationStatus
                ViewBag.TotalRetailers = _context.Retailers.Count();
                ViewBag.PendingRetailers = _context.Retailers.Count(r => r.User != null && !r.User.IsApproved && r.User.AccountStatus != "Rejected");
                ViewBag.ApprovedRetailers = _context.Retailers.Count(r => r.User != null && r.User.IsApproved);
                ViewBag.RejectedRetailers = _context.Retailers.Count(r => r.User != null && r.User.AccountStatus == "Rejected");

                // Product and Order Statistics
                ViewBag.TotalProducts = _context.Products.Count();
                ViewBag.TotalOrders = _context.Orders.Count();
                ViewBag.PendingOrders = _context.Orders.Count(o => o.OrderStatus == "Processing" || o.OrderStatus == "Pending");

                // Get recent suppliers with FULL User object included
                var recentSuppliers = _context.Suppliers
                    .Include(s => s.User)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToList();

                ViewBag.RecentSuppliers = recentSuppliers;

                // Get recent retailers with FULL User object included
                var recentRetailers = _context.Retailers
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToList();

                ViewBag.RecentRetailers = recentRetailers;

                // Commission Statistics
                ViewBag.TotalCommissionRevenue = _context.Commissions
                    .Where(c => c.Status == "Paid")
                    .Sum(c => (decimal?)c.CommissionAmount) ?? 0;

                ViewBag.PendingCommissions = _context.Commissions
                    .Where(c => c.Status == "Pending")
                    .Sum(c => (decimal?)c.CommissionAmount) ?? 0;

                // Recent Commissions
                ViewBag.RecentCommissions = _context.Commissions
                    .Include(c => c.Supplier)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList();

                // Notification data
                var adminUser = _context.Users.FirstOrDefault(u => u.Role == "Admin");
                if (adminUser != null)
                {
                    ViewBag.UnreadNotifications = _context.Notifications
                        .Count(n => n.UserId == adminUser.Id && !n.IsRead);

                    ViewBag.RecentNotifications = _context.Notifications
                        .Where(n => n.UserId == adminUser.Id)
                        .OrderByDescending(n => n.CreatedAt)
                        .Take(5)
                        .ToList();
                }

                // Progress bar calculations
                ViewBag.SupplierProgress = ViewBag.TotalSuppliers > 0
                    ? (ViewBag.VerifiedSuppliers * 100 / ViewBag.TotalSuppliers)
                    : 0;

                ViewBag.RetailerProgress = ViewBag.TotalRetailers > 0
                    ? (ViewBag.ApprovedRetailers * 100 / ViewBag.TotalRetailers)
                    : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Dashboard Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading dashboard data.";
            }

            return View();
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
                        LicenseNumber = s.LicenseNumber,
                        LicenseFilePath = s.LicenseFilePath,
                        TaxIdentificationNumber = s.TaxIdentificationNumber,
                        CompanyAddress = s.CompanyAddress,
                        City = s.City,
                        Country = s.Country,
                        Website = s.Website,
                        Description = s.Description,
                        CreatedAt = s.CreatedAt,
                        FullName = s.User != null ? s.User.FullName : string.Empty,
                        Email = s.User != null ? s.User.Email : string.Empty,
                        PhoneNumber = s.User != null ? s.User.PhoneNumber : string.Empty
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
                        BusinessLicenseNumber = r.BusinessLicenseNumber,
                        TaxIdentificationNumber = r.TaxIdentificationNumber,
                        BusinessAddress = r.BusinessAddress,
                        City = r.City,
                        Country = r.Country,
                        StoreSize = r.StoreSize,
                        Description = r.Description,
                        CreatedAt = r.CreatedAt,
                        FullName = r.User != null ? r.User.FullName : string.Empty,
                        Email = r.User != null ? r.User.Email : string.Empty,
                        PhoneNumber = r.User != null ? r.User.PhoneNumber : string.Empty
                    })
                    .ToListAsync();

                ViewBag.PendingRetailers = pendingRetailers;
                ViewBag.PendingSuppliersCount = pendingSuppliers.Count;
                ViewBag.PendingRetailersCount = pendingRetailers.Count;

                return View(pendingSuppliers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PendingSuppliers Error: {ex.Message}");
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
                    return NotFound();
                }

                string filePath = Path.Combine(_webHostEnvironment.WebRootPath,
                    supplier.LicenseFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("License file not found.");
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
                Console.WriteLine($"❌ ViewLicense Error: {ex.Message}");
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
                        ActionUrl = "/Admin/PendingSuppliers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Supplier '{supplier.CompanyName}' has been approved successfully!";
                Console.WriteLine($"✅ Supplier {id} approved successfully - User Approved: {supplier.User.IsApproved}, Supplier Verified: {supplier.VerificationStatus}");

                return RedirectToAction("PendingSuppliers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error approving supplier: {ex.Message}");
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
                        ActionUrl = "/Admin/PendingSuppliers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Retailer '{retailer.BusinessName}' has been approved successfully!";
                Console.WriteLine($"✅ Retailer {id} approved successfully - User Approved: {retailer.User.IsApproved}, Retailer Verified: {retailer.IsVerified}");

                return RedirectToAction("RetailerDetails", new { id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error approving retailer: {ex.Message}");
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
                        ActionUrl = "/Admin/PendingSuppliers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Supplier '{supplier.CompanyName}' has been rejected.";
                Console.WriteLine($"✅ Supplier {id} rejected successfully - User Approved: {supplier.User.IsApproved}, Supplier Status: {supplier.VerificationStatus}");

                return RedirectToAction("SupplierDetails", new { id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error rejecting supplier: {ex.Message}");
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
                        ActionUrl = "/Admin/PendingSuppliers"
                    };
                    _context.Notifications.Add(adminNotification);
                }

                // Save changes
                await _context.SaveChangesAsync();

                // IMPORTANT: Log after changes to verify
                Console.WriteLine($"After Rejection - User ID: {retailer.User.Id}, IsApproved: {retailer.User.IsApproved}, AccountStatus: {retailer.User.AccountStatus}");

                TempData["SuccessMessage"] = $"✅ Retailer '{retailer.BusinessName}' has been rejected successfully.";

                // Redirect to the same details page to see the updated status
                return RedirectToAction("RetailerDetails", new { id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error rejecting retailer: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner error: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "❌ An error occurred while rejecting the retailer. Please try again.";
                return RedirectToAction("RetailerDetails", new { id });
            }
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
                Console.WriteLine($"❌ VerifiedSuppliers Error: {ex.Message}");
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
                Console.WriteLine($"❌ RejectedSuppliers Error: {ex.Message}");
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
                Console.WriteLine($"❌ AllRetailers Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading retailers.";
                return View(new List<Retailer>());
            }
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
                return NotFound();
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
                return NotFound();
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
                Console.WriteLine($"❌ Notifications Error: {ex.Message}");
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
                Console.WriteLine($"❌ MarkNotificationRead Error: {ex.Message}");
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
                Console.WriteLine($"❌ MarkAllNotificationsRead Error: {ex.Message}");
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
                Console.WriteLine($"❌ GetUnreadCount Error: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        // GET: /Admin/Settings
        public IActionResult Settings()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = _context.Users.Find(userId);

            if (user != null)
            {
                ViewBag.UserName = user.FullName;
                ViewBag.UserEmail = user.Email;
            }

            return View();
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
                Console.WriteLine($"❌ ChangePassword Error: {ex.Message}");
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
                ViewBag.NewRetailersThisYear = _context.Retailers.Count(r => r.CreatedAt >= startOfYear);
                ViewBag.TotalRevenue = _context.Orders.Sum(o => (decimal?)o.PurchaseOrder.TotalAmount) ?? 0;

                // Approval stats
                ViewBag.ApprovalRate = _context.Suppliers.Count() > 0
                    ? (_context.Suppliers.Count(s => s.VerificationStatus == "Verified") * 100 / _context.Suppliers.Count())
                    : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Reports Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading report data.";
            }

            return View();
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
        
    }
}