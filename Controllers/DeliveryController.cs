using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;
using SCM_System.Services;
using System.Security.Claims;
using System.IO;
using SCM_System.Models.Enums;
using SCM_System.Models.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "DeliveryAgent,Supplier")]
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IPurchaseOrderService _poService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notificationService;

        public DeliveryController(ApplicationDbContext context, IOrderService orderService, IPurchaseOrderService poService, IWebHostEnvironment webHostEnvironment, INotificationService notificationService)
        {
            _context = context;
            _orderService = orderService;
            _poService = poService;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
        }

        private async Task<int> GetEmployeeIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var employee = await _context.SupplierEmployees.FirstOrDefaultAsync(e => e.UserId == userId);
                return employee?.Id ?? 0;
            }
            return 0;
        }

        [Authorize(Roles = "DeliveryAgent")]
        public async Task<IActionResult> Settings()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var employee = await _context.SupplierEmployees
                .Include(e => e.User)
                .Include(e => e.Vehicle)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound();

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == employee.SupplierId && v.IsActive && !v.IsDeleted)
                .ToListAsync();

            return View(employee);
        }

        [Authorize(Roles = "DeliveryAgent")]
        public async Task<IActionResult> RouteItinerary()
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            var activeDeliveries = await _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
                .Include(po => po.Warehouse)
                .Include(po => po.Retailer)
                .Where(po => po.DeliveryAgentId == employeeId && po.Status != "Delivered" && po.Status != "Completed")
                .ToListAsync();

            return View(activeDeliveries);
        }

        [HttpPost]
        [Authorize(Roles = "DeliveryAgent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SCM_System.Models.ViewModels.DeliverySettingsViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var employee = await _context.SupplierEmployees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Vehicles = await _context.Vehicles
                    .Where(v => v.SupplierId == employee.SupplierId && v.IsActive && !v.IsDeleted)
                    .ToListAsync();
                return View(model);
            }

            // Update User Profile
            employee.User.FullName = model.FullName;
            employee.User.Email = model.Email;
            employee.User.PhoneNumber = model.Phone;

            // Handle Profile Picture
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var fileName = $"profile_{userId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(model.ProfilePicture.FileName)}";
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(stream);
                }
                employee.ProfilePhotoPath = $"/uploads/profiles/{fileName}";
                employee.User.ProfileImage = employee.ProfilePhotoPath;

                // Sync Session for real-time header update
                HttpContext.Session.SetString("ProfileImg", employee.User.ProfileImage ?? "/img/avatars/default.png");
            }

            // Update Password if requested
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword) || employee.User.PasswordHash != HashPassword(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    ViewBag.Vehicles = await _context.Vehicles
                        .Where(v => v.SupplierId == employee.SupplierId && v.IsActive && !v.IsDeleted)
                        .ToListAsync();
                    return View(model);
                }
                employee.User.PasswordHash = HashPassword(model.NewPassword);
            }

            // Update Delivery Preferences & Availability
            employee.VehicleId = model.VehicleId;
            employee.IsOnDuty = model.IsOnDuty;
            employee.WorkingHoursStart = model.WorkingHoursStart;
            employee.WorkingHoursEnd = model.WorkingHoursEnd;
            employee.MaxDailyDeliveries = model.MaxDailyDeliveries;
            employee.RequireProofPhoto = model.RequireProofPhoto;
            employee.RequireSignature = model.RequireSignature;
            employee.AutoAcceptAssignments = model.AutoAcceptAssignments;
            employee.AllowNightDeliveries = model.AllowNightDeliveries;
            employee.NotifyNewAssignment = model.NotifyNewAssignment;
            employee.SmsNotificationNumber = model.SmsNotificationNumber;

            employee.UpdatedAt = DateTime.Now;
            employee.UpdatedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            // Update Session for real-time UI reflect
            HttpContext.Session.SetString("UserName", employee.User.FullName ?? "");
            if (!string.IsNullOrEmpty(employee.ProfilePhotoPath))
            {
                HttpContext.Session.SetString("ProfileImg", employee.ProfilePhotoPath);
            }

            TempData["SuccessMessage"] = "Settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }


        // GET: /Delivery/Dashboard
        [Authorize(Roles = "DeliveryAgent")]
        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var employee = await _context.SupplierEmployees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound();

            // Statistics
            ViewBag.AssignedDeliveries = await _context.PurchaseOrders
                .CountAsync(po => po.DeliveryAgentId == employee.Id && (po.Status == POStatus.Accepted || po.Status == POStatus.Ready));

            ViewBag.InTransitDeliveries = await _context.PurchaseOrders
                .CountAsync(po => po.DeliveryAgentId == employee.Id && po.Status == POStatus.InTransit);

            ViewBag.CompletedDeliveries = await _context.PurchaseOrders
                .CountAsync(po => po.DeliveryAgentId == employee.Id && (po.Status == POStatus.Delivered || po.Status == POStatus.Completed));

            // Active Deliveries for the table
            ViewBag.ActiveDeliveries = await _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
                .Include(po => po.Warehouse)
                .Include(po => po.Retailer)
                .Where(po => po.DeliveryAgentId == employee.Id && po.Status != POStatus.Delivered && po.Status != POStatus.Completed)
                .OrderByDescending(po => po.CreatedAt)
                .ToListAsync();

            return View(employee);
        }

        // GET: /Delivery/Tracking/20
        [Authorize(Roles = "DeliveryAgent")]
        public async Task<IActionResult> Tracking(int id)
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                    .ThenInclude(o => o.Retailer)
                .Include(p => p.Warehouse)
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id && p.DeliveryAgentId == employeeId);

            if (po == null) return NotFound();

            return View(po);
        }

        [HttpPost]
        [Authorize(Roles = "DeliveryAgent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVehicleSettings(string status, string gpsMode)
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            var employee = await _context.SupplierEmployees
                .Include(e => e.Vehicle)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null || employee.Vehicle == null) return NotFound();

            // Example telemetry update logic
            // In a real app, this might update a Telemetry table or the Vehicle status
            if (status == "critical")
            {
                employee.Vehicle.Status = VehicleStatus.Maintenance;
                employee.IsOnDuty = false;
            }
            
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Vehicle telemetry settings updated.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> MyDeliveries(string? searchTerm = null, string? status = null)
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            // Fetch POs assigned to this delivery agent with filtering
            var query = _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
                .Include(po => po.Warehouse)
                .Include(po => po.Retailer)
                        .ThenInclude(r => r.User)
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(po => po.DeliveryAgentId == employeeId && po.Status != "Completed");

            // Apply Search Filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(po => po.PONumber.ToLower().Contains(search) || 
                                        po.Order.Retailer.BusinessName.ToLower().Contains(search) ||
                                        po.Supplier.CompanyName.ToLower().Contains(search));
            }

            // Apply Status Filter
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(po => po.Status == status);
            }

            var assignedPOs = await query
                .OrderByDescending(po => po.CreatedAt)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;

            return View(assignedPOs);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int purchaseOrderId, string status, IFormFile? proofImage = null, string? signatureData = null, bool checklistVerified = false)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                    .ThenInclude(o => o.Retailer)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

            if (po == null) return NotFound();

            var employeeId = await GetEmployeeIdAsync();
            if (employeeId != 0 && po.DeliveryAgentId != employeeId) return Unauthorized();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (status == "Delivered")
            {
                // 1. Enforce Checklist
                if (!checklistVerified)
                {
                    TempData["ErrorMessage"] = "You must verify the delivery checklist before completing the order.";
                    return RedirectToAction(nameof(MyDeliveries));
                }
                po.ChecklistVerified = true;

                // 2. Handle Signature (File storage)
                if (!string.IsNullOrWhiteSpace(signatureData))
                {
                    try
                    {
                        var base64Data = signatureData.Contains(",") ? signatureData.Split(',')[1] : signatureData;
                        var bytes = Convert.FromBase64String(base64Data);

                        // Validate Size (Max 1MB for signature)
                        if (bytes.Length > 1024 * 1024)
                        {
                            TempData["ErrorMessage"] = "Signature data is too large.";
                            return RedirectToAction(nameof(MyDeliveries));
                        }

                        var fileName = $"sig_{po.PONumber}_{DateTime.Now:yyyyMMddHHmmss}.png";
                        var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "signatures");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        var filePath = Path.Combine(uploadPath, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                        po.SignaturePath = $"/uploads/signatures/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "Failed to save signature: " + ex.Message;
                        return RedirectToAction(nameof(MyDeliveries));
                    }
                }

                // 3. Handle Photo Proof
                if (proofImage != null && proofImage.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(proofImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Invalid photo type. Please upload JPG or PNG.";
                        return RedirectToAction(nameof(MyDeliveries));
                    }

                    if (proofImage.Length > 5 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "Photo proof exceeds 5MB.";
                        return RedirectToAction(nameof(MyDeliveries));
                    }

                    var fileName = $"proof_{po.PONumber}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                    var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "delivery_proofs");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await proofImage.CopyToAsync(stream);
                    }
                    po.ProofOfDelivery = $"/uploads/delivery_proofs/{fileName}";
                }

                // 4. Validate Proof Requirement (At least ONE of Signature, Photo, or QR)
                bool hasProof = !string.IsNullOrEmpty(po.SignaturePath) || 
                                !string.IsNullOrEmpty(po.ProofOfDelivery) || 
                                po.IsQRVerified;

                if (!hasProof)
                {
                    TempData["ErrorMessage"] = "At least one proof of delivery (Signature, Photo, or QR Code) is required.";
                    return RedirectToAction(nameof(MyDeliveries));
                }

                po.DeliveredAt = DateTime.Now;
            }

            // Update PO status via Service (Strict guardrails will be checked there)
            try 
            {
                await _poService.UpdatePurchaseOrderStatusAsync(purchaseOrderId, status, userId);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(MyDeliveries));
            }

            // Reset Vehicle status to Available when delivery is complete
            if (status == "Delivered")
            {
                if (po.VehicleId.HasValue)
                {
                    var vehicle = await _context.Vehicles.FindAsync(po.VehicleId.Value);
                    if (vehicle != null)
                    {
                        vehicle.Status = SCM_System.Models.Enums.VehicleStatus.Available;
                        vehicle.UpdatedAt = DateTime.Now;
                    }
                }
            }
            
            await _context.SaveChangesAsync();

            // SYNC ORDER STATUS FOR IN TRANSIT
            if (status == "In Transit")
            {
                var order = po.Order;
                if (order != null && order.OrderStatus != "In Transit")
                {
                    var otherPOs = await _context.PurchaseOrders
                        .Where(p => p.OrderId == order.Id && p.Id != po.Id)
                        .Select(p => p.Status)
                        .ToListAsync();

                    var hasInTransitOrDelivered = otherPOs.Any(s => s == "In Transit" || s == "Delivered" || s == "Completed");

                    if (hasInTransitOrDelivered)
                    {
                        order.OrderStatus = "Partially In Transit";
                    }
                    else
                    {
                        order.OrderStatus = "In Transit";
                    }

                    _context.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = order.OrderStatus,
                        Comments = $"PO {po.PONumber} is now In Transit with delivery agent",
                        ChangedByUserId = employeeId,
                        ChangedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                }
            }

            // Re-fetch to ensure we have updated status for parent check
            po = await _context.PurchaseOrders.FindAsync(purchaseOrderId);

            if (status == "Delivered")
            {
                var allPOs = await _context.PurchaseOrders
                    .Where(p => p.OrderId == po.OrderId)
                    .ToListAsync();

                bool allDelivered = allPOs.All(p => p.Status == "Delivered" || p.Status == "Completed");

                string orderComment;

                if (allDelivered)
                {
                    orderComment = "All warehouse deliveries have been completed.";
                }
                else
                {
                    orderComment = $"Delivery of PO {po.PONumber} complete. Awaiting other warehouse shipments.";
                }

                // ✅ Always pass "Delivered" to ensure commissions are created
                await _orderService.UpdateOrderStatusAsync(po.OrderId, "Delivered", orderComment, userId);

                // Send notification to retailer to rate delivery if order and retailer exist
                if (po.Order?.Retailer?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        po.Order.Retailer.UserId,
                        "Rate Your Delivery Experience",
                        $"Your order #{po.PONumber} has been delivered. Please rate your delivery experience.",
                        "DeliveryRating",
                        $"/Rating/RateDelivery?purchaseOrderId={po.Id}"
                    );
                }
            }

            TempData["SuccessMessage"] = $"PO {po.PONumber} status updated to {status}.";
            return RedirectToAction(nameof(MyDeliveries));
        }


      

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProof(int purchaseOrderId, string proofData)
        {
            var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
            if (po == null) return NotFound();

            po.ProofOfDelivery = proofData; 
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proof of delivery uploaded successfully.";
            return RedirectToAction(nameof(MyDeliveries));
        }

        // Add this method to DeliveryController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyQRCode([FromBody] QRVerificationRequest request)
        {
            try
            {
                var po = await _context.PurchaseOrders
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId);

                if (po == null)
                    return Json(new { success = false, message = "Purchase Order not found" });

                var employeeId = await GetEmployeeIdAsync();
                if (employeeId != 0 && po.DeliveryAgentId != employeeId)
                    return Json(new { success = false, message = "Unauthorized" });

                // For manual "VERIFY" entry - always accept
                if (request.IsManual && request.QRCode.ToUpper() == "VERIFY")
                {
                    return Json(new { success = true, message = "Manual delivery confirmed" });
                }

                // Check if QR code matches the order
                bool isValid = false;

                if (po.Order != null)
                {
                    // Check against OrderNumber (what is displayed to user)
                    if (request.QRCode == $"ORDER-{po.Order.OrderNumber}")
                    {
                        isValid = true;
                    }
                    // Check against the stored QRCodeValue if exists
                    else if (!string.IsNullOrEmpty(po.Order.QRCodeValue) && po.Order.QRCodeValue == request.QRCode)
                    {
                        isValid = true;
                    }
                }

                // Also check against common patterns for robustness
                if (!isValid && (request.QRCode == $"ORDER-{po.OrderId}-DELIVERY" || request.QRCode == $"PO-{po.Id}"))
                {
                    isValid = true;
                }

                if (isValid)
                {
                    // Update delivery with QR verification on PO level
                    po.IsQRVerified = true;
                    po.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "QR Code verified successfully" });
                }

                return Json(new { success = false, message = "Invalid QR Code. Please check and try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "DeliveryAgent")]
        public async Task<IActionResult> OptimizeRoute([FromBody] OptimizeRouteRequest request)
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Json(new { success = false, message = "Unauthorized" });

            // In a real application, this would call a routing engine API (like Google Maps Distance Matrix or OSRM)
            // For now, we simulate an optimization delay and return an optimized order of points
            await Task.Delay(1000);

            if (request.CurrentPoints == null || !request.CurrentPoints.Any())
            {
                return Json(new { success = false, message = "No points provided" });
            }

            // Simulate optimization (e.g. reversing the points or just returning them as optimized)
            var optimizedPoints = request.CurrentPoints.OrderBy(p => p.Lat).ToList();

            return Json(new 
            { 
                success = true, 
                message = "Route optimized successfully.",
                optimizedRoute = optimizedPoints,
                estimatedDistanceKm = (optimizedPoints.Count * 3.8).ToString("0.0")
            });
        }

        public class OptimizeRouteRequest
        {
            public List<RoutePoint> CurrentPoints { get; set; } = new List<RoutePoint>();
        }

        public class RoutePoint
        {
            public decimal Lat { get; set; }
            public decimal Lng { get; set; }
            public int? PoId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsFailed(int purchaseOrderId, string reason)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
            if (po == null) return NotFound();

            try
            {
                po.FailureReason = reason;
                await _poService.UpdatePurchaseOrderStatusAsync(purchaseOrderId, POStatus.Failed, userId);
                TempData["SuccessMessage"] = "Delivery marked as failed. Recovery options available for manager.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(MyDeliveries));
        }

        // Recovery Actions (Manager/Supplier Level)
        [HttpPost]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> ReassignDelivery(int id, int agentId)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po == null) return NotFound();

            po.DeliveryAgentId = agentId;
            po.Status = POStatus.Packed; // Reset to packed so it can be shipped again
            po.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "PurchaseOrder", new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> RetryDelivery(int id)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po == null) return NotFound();

            po.Status = POStatus.Ready; // Move back to Ready Dispatch
            po.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "PurchaseOrder", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> RequestSupport([FromBody] SupportRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false, message = "Session expired." });
            int userId = int.Parse(userIdStr);

            var po = await _context.PurchaseOrders
                .Include(p => p.Warehouse)
                    .ThenInclude(w => w.PrimaryManager)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId);

            if (po == null || po.Warehouse == null || po.Warehouse.PrimaryManager == null)
            {
                return Json(new { success = false, message = "Unable to locate Warehouse Manager for this delivery." });
            }

            var agent = await _context.SupplierEmployees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            
            string messageBody = $"[SUPPORT REQUEST] Order #{po.PONumber}\n" +
                                 $"Issue: {request.IssueType}\n" +
                                 $"Agent: {agent?.User?.FullName ?? "Unknown"}\n" +
                                 $"Status: {po.Status}\n" +
                                 $"Notes: {request.Notes}";

            await _notificationService.SendNotificationAsync(
                po.Warehouse.PrimaryManager.UserId,
                "Immediate Support Requested",
                messageBody,
                "Support",
                $"/WarehouseManager/Dashboard/{po.WarehouseId}"
            );

            return Json(new { success = true, message = "Support request sent to Warehouse Manager." });
        }

        [HttpGet]
        public async Task<IActionResult> MyPerformance()
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return NotFound();

            var ratings = await _context.DeliveryRatings
                .Where(r => r.DriverEmployeeId == employeeId)
                .Include(r => r.PurchaseOrder)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new DriverPerformanceViewModel
            {

                TotalDeliveries = await _context.PurchaseOrders.CountAsync(po => po.DeliveryAgentId == employeeId && po.Status == "Delivered"),
                OverallRating = ratings.Any() ? ratings.Average(r => r.OverallRating) : 0,
                AverageTimeliness = ratings.Any() ? ratings.Average(r => r.Timeliness) : 0,
                AverageProfessionalism = ratings.Any() ? ratings.Average(r => r.Professionalism) : 0,
                AverageVehicleCondition = ratings.Any() ? ratings.Average(r => r.VehicleCondition) : 0,
                AverageCommunication = ratings.Any() ? ratings.Average(r => r.Communication) : 0,
                
                RecentRatings = ratings.Take(5).Select(r => new RecentRatingViewModel
                {
                    PONumber = r.PurchaseOrder?.PONumber ?? "N/A",
                    OverallRating = r.OverallRating,
                    Comment = r.Comment ?? "No comment provided.",
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            // Monthly Trend (Dummy data for now to populate chart labels)
            viewModel.MonthlyLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            viewModel.MonthlyRatings = ratings.Any() ? new List<double> { 4.2, 4.5, 4.3, 4.8, 4.6, viewModel.OverallRating } : new List<double> { 0, 0, 0, 0, 0, 0 };
            viewModel.MonthlyOnTimeRates = new List<double> { 90, 92, 88, 95, 94, 98 };
            viewModel.OnTimeRate = 95; // Placeholder

            return View(viewModel);
        }
    }

    public class QRVerificationRequest
    {
        public int PurchaseOrderId { get; set; }
        public string QRCode { get; set; } = string.Empty;
        public bool IsManual { get; set; }
    }

    public class SupportRequest
    {
        public int PurchaseOrderId { get; set; }
        public string IssueType { get; set; }
        public string Notes { get; set; }
    }
}
