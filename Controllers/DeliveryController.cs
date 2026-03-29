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

        public DeliveryController(ApplicationDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
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
        public async Task<IActionResult> UpdateStatus(int purchaseOrderId, string status, string proofData = null)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);
            
            if (po == null) return NotFound();

            var employeeId = await GetEmployeeIdAsync();
            if (employeeId != 0 && po.DeliveryAgentId != employeeId) return Unauthorized();

            if (status == "Delivered")
            {
                if (string.IsNullOrWhiteSpace(proofData) && string.IsNullOrWhiteSpace(po.ProofOfDelivery))
                {
                    TempData["ErrorMessage"] = "Proof of Delivery is required to mark a PO as Delivered.";
                    return RedirectToAction(nameof(MyDeliveries));
                }
                
                if (!string.IsNullOrWhiteSpace(proofData))
                {
                    po.ProofOfDelivery = proofData;
                }
                po.DeliveredAt = DateTime.Now;
            }

            po.Status = status;
            await _context.SaveChangesAsync();
            
            // Check if ALL POs for the master order are Delivered
            if (status == "Delivered")
            {
                var allPOs = await _context.PurchaseOrders.Where(p => p.OrderId == po.OrderId).ToListAsync();
                bool allDelivered = allPOs.All(p => p.Status == "Delivered" || p.Status == "Completed");
                
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (allDelivered)
                {
                    await _orderService.UpdateOrderStatusAsync(po.OrderId, "Delivered", "All warehouse deliveries have been completed.", userId);
                }
                else
                {
                    await _orderService.UpdateOrderStatusAsync(po.OrderId, "Partially Delivered", $"Delivery of PO {po.PONumber} complete. Awaiting other warehouse shipments.", userId);
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
    }
}
