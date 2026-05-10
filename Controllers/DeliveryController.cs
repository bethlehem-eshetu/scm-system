using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;
using SCM_System.Services;
using System.Security.Claims;
using System.IO;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "DeliveryAgent,Supplier")]
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IPurchaseOrderService _poService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DeliveryController(ApplicationDbContext context, IOrderService orderService, IPurchaseOrderService poService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _orderService = orderService;
            _poService = poService;
            _webHostEnvironment = webHostEnvironment;
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
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return NotFound();

            // Stats for Performance Section
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            
            var deliveries = await _context.PurchaseOrders
                .Where(po => po.DeliveryAgentId == employee.Id && po.DeliveredAt >= startOfMonth)
                .ToListAsync();

            var totalDeliveries = deliveries.Count;
            var onTimeDeliveries = deliveries.Count(po => po.DeliveredAt.HasValue && po.DeliveredAt <= po.ExpectedDeliveryDate);
            var onTimePercentage = totalDeliveries > 0 ? (double)onTimeDeliveries / totalDeliveries * 100 : 100;

            var ratings = await _context.Ratings
                .Where(r => r.PurchaseOrder.DeliveryAgentId == employee.Id)
                .Select(r => r.RatingValue)
                .ToListAsync();
            
            var averageRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 5.0;

            var model = new SCM_System.Models.ViewModels.DeliverySettingsViewModel
            {
                FullName = employee.User.FullName ?? "",
                Email = employee.User.Email ?? "",
                Phone = employee.User.PhoneNumber ?? employee.Phone ?? "",
                ExistingProfilePicture = employee.ProfilePhotoPath,
                VehicleId = employee.VehicleId,
                IsOnDuty = employee.IsOnDuty,
                WorkingHoursStart = employee.WorkingHoursStart,
                WorkingHoursEnd = employee.WorkingHoursEnd,
                MaxDailyDeliveries = employee.MaxDailyDeliveries,
                RequireProofPhoto = employee.RequireProofPhoto,
                RequireSignature = employee.RequireSignature,
                AutoAcceptAssignments = employee.AutoAcceptAssignments,
                AllowNightDeliveries = employee.AllowNightDeliveries,
                NotifyNewAssignment = employee.NotifyNewAssignment,
                SmsNotificationNumber = employee.SmsNotificationNumber,
                TotalDeliveriesMonth = totalDeliveries,
                AverageRating = averageRating,
                OnTimePercentage = Math.Round(onTimePercentage, 1)
            };

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == employee.SupplierId && v.IsActive && !v.IsDeleted)
                .ToListAsync();

            return View(model);
        }

        [Authorize(Roles = "DeliveryAgent")]
        public async Task<IActionResult> RouteItinerary()
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            var activeDeliveries = await _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
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
        public async Task<IActionResult> Dashboard()
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            // Get statistics for the delivery agent
            var assignedDeliveries = await _context.PurchaseOrders
                .Where(po => po.DeliveryAgentId == employeeId && po.Status != "Delivered" && po.Status != "Completed")
                .CountAsync();

            var completedDeliveries = await _context.PurchaseOrders
                .Where(po => po.DeliveryAgentId == employeeId && (po.Status == "Delivered" || po.Status == "Completed"))
                .CountAsync();

            var inTransitDeliveries = await _context.PurchaseOrders
                .Where(po => po.DeliveryAgentId == employeeId && po.Status == "In Transit")
                .CountAsync();

            // Get the delivery agent details
            var employee = await _context.SupplierEmployees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null) return NotFound();

            ViewBag.AssignedDeliveries = assignedDeliveries;
            ViewBag.CompletedDeliveries = completedDeliveries;
            ViewBag.InTransitDeliveries = inTransitDeliveries;

            // Get recent assigned deliveries for the table
            var recentDeliveries = await _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
                .Where(po => po.DeliveryAgentId == employeeId && po.Status != "Delivered" && po.Status != "Completed")
                .OrderByDescending(po => po.CreatedAt)
                .Take(5)
                .Select(po => new
                {
                    po.PONumber,
                    Destination = po.Order != null ? po.Order.Retailer.BusinessName : "N/A",
                    po.Status,
                    po.Id
                })
                .ToListAsync();

            ViewBag.RecentDeliveries = recentDeliveries;
            var activeDeliveries = await _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
                .Where(po => po.DeliveryAgentId == employeeId && po.Status != "Delivered" && po.Status != "Completed")
                .ToListAsync();

            ViewBag.ActiveDeliveries = activeDeliveries;

            var mapDataList = new List<object>();
            var random = new Random();
            var index = 0;
            foreach (var po in activeDeliveries)
            {
                mapDataList.Add(new {
                    id = po.Id,
                    poNumber = po.PONumber,
                    businessName = po.Order?.Retailer?.BusinessName ?? "Retailer",
                    address = po.DeliveryAddress ?? (po.Order?.Retailer?.BusinessAddress ?? "Address not set"),
                    lat = 9.02 + (index * 0.12) + (random.NextDouble() * 0.05),
                    lng = 38.75 + (index * 0.09) + (random.NextDouble() * 0.03),
                    priority = po.Status == "In Transit" ? "High" : "Normal",
                    status = po.Status
                });
                index++;
            }
            ViewBag.MapData = Newtonsoft.Json.JsonConvert.SerializeObject(mapDataList);

            return View(employee);
        }

        public async Task<IActionResult> MyDeliveries(string? searchTerm = null, string? status = null)
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            // Fetch POs assigned to this delivery agent with filtering
            var query = _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
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
    }

    public class QRVerificationRequest
    {
        public int PurchaseOrderId { get; set; }
        public string QRCode { get; set; } = string.Empty;
        public bool IsManual { get; set; }
    }
}
