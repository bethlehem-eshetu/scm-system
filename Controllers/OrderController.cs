using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;

        public OrderController(IOrderService orderService, ApplicationDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Retailer"))
            {
                var rId = await GetRetailerIdAsync();
                return View(await _orderService.GetOrdersByRetailerAsync(rId));
            }
            else if (User.IsInRole("Supplier"))
            {
                var sId = await GetSupplierIdAsync();
                return View(await _orderService.GetOrdersBySupplierAsync(sId));
            }
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            if (User.IsInRole("Supplier"))
            {
                var sId = await GetSupplierIdAsync();
                ViewBag.Warehouses = await _context.Warehouses.Where(w => w.SupplierId == sId && w.Status == SCM_System.Models.Enums.WarehouseStatus.Active).ToListAsync();
                
                // Pre-acceptance stock validation
                var stockStatus = new Dictionary<int, bool>();
                bool allAvailable = true;
                foreach (var item in order.OrderItems)
                {
                    var globalStock = await _context.Inventories
                        .Where(i => i.ProductId == item.ProductId && i.Warehouse.SupplierId == sId)
                        .SumAsync(i => i.QuantityOnHand - i.QuantityReserved);
                    
                    stockStatus[item.ProductId] = globalStock >= item.Quantity;
                    if (globalStock < item.Quantity) allAvailable = false;
                }
                ViewBag.StockStatus = stockStatus;
                ViewBag.CanAccept = allAvailable;
            }

            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = "Supplier, Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string comments)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                await _orderService.UpdateOrderStatusAsync(id, status, comments, userId);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            bool success = await _orderService.CancelOrderAsync(id);
            if (!success) TempData["ErrorMessage"] = "Could not cancel Order. It may have already been shipped or completed.";
            else TempData["SuccessMessage"] = "Order Cancelled Successfully. Vendor stock has been released.";
            
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusAction(int id, string actionType, string? rejectionReason, int? warehouseId = null)
        {
            try
            {
                if (actionType == "Accept")
                {
                    bool success = await _orderService.AcceptOrderAsync(id, warehouseId);
                    if (!success) TempData["ErrorMessage"] = "Could not accept Order. It may already be processed.";
                    else TempData["SuccessMessage"] = "Order Accepted. A Purchase Order has been automatically generated.";
                }
                else if (actionType == "Reject")
                {
                    if (string.IsNullOrWhiteSpace(rejectionReason))
                    {
                        TempData["ErrorMessage"] = "A rejection reason is mandatory.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    bool success = await _orderService.RejectOrderAsync(id, rejectionReason);
                    if (!success) TempData["ErrorMessage"] = "Could not reject Order.";
                    else TempData["SuccessMessage"] = "Order Rejected.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<int> GetRetailerIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var r = await _context.Retailers.FirstOrDefaultAsync(x => x.UserId == userId);
                return r?.Id ?? 0;
            }
            return 0;
        }

        private async Task<int> GetSupplierIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var s = await _context.Suppliers.FirstOrDefaultAsync(x => x.UserId == userId);
                return s?.Id ?? 0;
            }
            return 0;
        }
        [HttpGet]
        public async Task<IActionResult> GetStatus(int id)
        {
            var order = await _context.Orders
                .Include(o => o.PurchaseOrders)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null) return NotFound();

            // Check if retailer/supplier has access
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            if (User.IsInRole("Retailer"))
            {
                var rId = await GetRetailerIdAsync();
                if (order.RetailerId != rId) return Forbid();
            }
            else if (User.IsInRole("Supplier"))
            {
                var sId = await GetSupplierIdAsync();
                if (order.SupplierId != sId) return Forbid();
            }

            return Json(new {
                status = order.OrderStatus,
                poStatuses = order.PurchaseOrders.Select(p => new { id = p.Id, status = p.Status }),
                lastUpdated = DateTime.Now.ToString("HH:mm:ss")
            });
        }
    }
}
