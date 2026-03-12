using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public OrderController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.Orders
                .Include(o => o.PurchaseOrder)
                    .ThenInclude(po => po.Retailer)
                .Include(o => o.Supplier)
                .AsQueryable();

            if (role == "Retailer")
            {
                query = query.Where(o => o.PurchaseOrder.RetailerId == userId);
            }
            else if (role == "Supplier")
            {
                query = query.Where(o => o.SupplierId == userId);
            }
            else
            {
                return Unauthorized();
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orders);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            var order = await _context.Orders
                .Include(o => o.PurchaseOrder)
                    .ThenInclude(po => po.Retailer)
                .Include(o => o.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            // Security Rule
            if (role == "Retailer" && order.PurchaseOrder.RetailerId != userId) return Unauthorized();
            if (role == "Supplier" && order.SupplierId != userId) return Unauthorized();

            ViewBag.AvailableStatuses = GetAllowedNextStatuses(order.OrderStatus);
            return View(order);
        }

        // POST: Order/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders
                .Include(o => o.PurchaseOrder)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Security Rule: Only the designated supplier
            if (order.SupplierId != GetCurrentUserId()) return Unauthorized();

            // Enforce Strict State Progression Sequence Rules
            var allowedNext = GetAllowedNextStatuses(order.OrderStatus);
            
            if (order.OrderStatus == "Delivered" || order.OrderStatus == "Cancelled")
            {
                 TempData["ErrorMessage"] = "This order is completely finalized and cannot change status.";
                 return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            if (!allowedNext.Contains(status))
            {
                TempData["ErrorMessage"] = $"Invalid status transition from {order.OrderStatus} to {status}.";
                 return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            order.OrderStatus = status;
            await _context.SaveChangesAsync();
            
            await _notificationService.SendNotificationAsync(
                order.PurchaseOrder.RetailerId, 
                $"Order #ORD-{order.Id.ToString("D5")} for {order.PurchaseOrder.ProductName} has been updated to '{status}'.", 
                "OrderStatusUpdated");

            TempData["SuccessMessage"] = $"Order status successfully updated to {status}.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // Helper method to enforce progression
        private List<string> GetAllowedNextStatuses(string currentStatus)
        {
            var allowed = new List<string>();
            
            // At any point BEFORE Delivered, a Supplier could Cancel (if there's a critical supply chain failure)
            if (currentStatus != "Delivered" && currentStatus != "Cancelled")
            {
                allowed.Add("Cancelled");
            }

            switch (currentStatus)
            {
                case "Processing":
                    allowed.Insert(0, "Packed");
                    break;
                case "Packed":
                    allowed.Insert(0, "Shipped");
                    break;
                case "Shipped":
                    allowed.Insert(0, "Delivered");
                    break;
            }

            return allowed;
        }
    }
}
