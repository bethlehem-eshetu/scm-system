using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "DeliveryAgent,Supplier")]
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IPurchaseOrderService _poService;

        public DeliveryController(ApplicationDbContext context, IOrderService orderService, IPurchaseOrderService poService)
        {
            _context = context;
            _orderService = orderService;
            _poService = poService;
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

            return View(employee);
        }

        public async Task<IActionResult> MyDeliveries()
        {
            var employeeId = await GetEmployeeIdAsync();
            if (employeeId == 0) return Unauthorized();

            // Fetch POs assigned to this delivery agent
            var assignedPOs = await _context.PurchaseOrders
                .Include(po => po.Order)
                    .ThenInclude(o => o.Retailer)
                        .ThenInclude(r => r.User)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(po => po.DeliveryAgentId == employeeId && po.Status != "Completed")
                .OrderByDescending(po => po.CreatedAt)
                .ToListAsync();

            return View(assignedPOs);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int purchaseOrderId, string status, IFormFile? proofImage = null, string proofData = null)
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
                // Handle image upload
                string imagePath = null;
                if (proofImage != null && proofImage.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                    var fileExtension = Path.GetExtension(proofImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Invalid file type. Please upload JPG, PNG, GIF, or PDF files only.";
                        return RedirectToAction(nameof(MyDeliveries));
                    }

                    // Validate file size (max 5MB)
                    if (proofImage.Length > 5 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "File size exceeds 5MB limit.";
                        return RedirectToAction(nameof(MyDeliveries));
                    }

                    // Create unique filename
                    var fileName = $"delivery_proof_{po.PONumber}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "delivery_proofs");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await proofImage.CopyToAsync(stream);
                    }

                    imagePath = $"/uploads/delivery_proofs/{fileName}";
                    po.ProofOfDelivery = imagePath;
                }
                else if (!string.IsNullOrWhiteSpace(proofData))
                {
                    po.ProofOfDelivery = proofData;
                }
                else
                {
                    TempData["ErrorMessage"] = "Proof of Delivery (image or text) is required to mark a PO as Delivered.";
                    return RedirectToAction(nameof(MyDeliveries));
                }

                po.DeliveredAt = DateTime.Now;
            }

            // Update PO status
            po.Status = status;

            // Reset Vehicle status to Available when delivery is complete
            if (status == "Delivered" || status == "Completed")
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

            await _poService.UpdatePurchaseOrderStatusAsync(purchaseOrderId, status, userId);

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

                if (po.Order != null && !string.IsNullOrEmpty(po.Order.QRCodeValue))
                {
                    isValid = (po.Order.QRCodeValue == request.QRCode);
                }

                // Also check against a simple expected value for testing
                if (!isValid && request.QRCode == $"ORDER-{po.OrderId}-DELIVERY")
                {
                    isValid = true;
                }

                if (isValid)
                {
                    // Update delivery with QR verification
                    if (po.Order?.Delivery != null)
                    {
                        po.Order.Delivery.IsQRVerified = true;
                        po.Order.Delivery.QRVerifiedAt = DateTime.Now;
                        po.Order.Delivery.QRVerificationMethod = request.IsManual ? "Manual" : "QRScan";
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "QR Code verified successfully" });
                }

                return Json(new { success = false, message = "Invalid QR Code. Please check and try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Add this request class at the end of the file or in a separate file
        public class QRVerificationRequest
        {
            public int PurchaseOrderId { get; set; }
            public string QRCode { get; set; }
            public bool IsManual { get; set; } = false;
        }

    }
}
