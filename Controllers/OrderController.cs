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

        [Route("Order/Details/{id}")]
        [Route("Retailer/OrderDetails/{id}")]
        public async Task<IActionResult> Details(int id, int? poId = null)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Retailer)
                    .ThenInclude(r => r.User)
                .Include(o => o.Supplier)
                    .ThenInclude(s => s.User)
                .Include(o => o.StatusHistory)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(p => p.Warehouse)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(p => p.PurchaseOrderItems)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(p => p.DeliveryAgent)
                        .ThenInclude(da => da.User)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(p => p.Vehicle)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Find the active PO for context (default to first or poId)
            var primaryPO = poId.HasValue 
                ? order.PurchaseOrders.FirstOrDefault(p => p.Id == poId.Value) 
                : order.PurchaseOrders.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            
            ViewBag.PrimaryPO = primaryPO;

            if (User.IsInRole("Supplier") || User.IsInRole("WarehouseManager"))
            {
                var sId = await GetSupplierIdAsync();
                
                // Security check
                if (order.SupplierId != sId) return Forbid();

                ViewBag.Warehouses = await _context.Warehouses
                    .Where(w => w.SupplierId == sId && w.IsActive)
                    .Select(w => new {
                        w.Id,
                        w.Name,
                        w.City,
                        TotalStock = _context.Inventories
                            .Where(i => i.WarehouseId == w.Id)
                            .Sum(i => (int?)(i.QuantityOnHand - i.QuantityReserved)) ?? 0
                    })
                    .ToListAsync();

                ViewBag.Agents = await _context.SupplierEmployees
                    .Include(a => a.User)
                    .Where(a => a.SupplierId == sId && a.IsActive && 
                               (a.EmployeeRole == "delivery_person" || a.EmployeeRole == "DeliveryAgent"))
                    .ToListAsync();
            }

            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPO(int orderId, int poId, int warehouseId)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == poId && p.OrderId == orderId);
        
            if (po == null) return NotFound();

            // Smart Auto Select Logic
            if (warehouseId == -1)
            {
                var warehouses = await _context.Warehouses
                    .Include(w => w.Inventories)
                    .Where(w => w.SupplierId == po.SupplierId && w.Status == SCM_System.Models.Enums.WarehouseStatus.Active)
                    .ToListAsync();

                // Find best warehouse: Enough stock -> Highest remaining stock -> Closest (as proxy, using coverage)
                var bestHub = warehouses
                    .Where(w => w.Inventories.Any(i => po.PurchaseOrderItems.All(poi => 
                        w.Inventories.Any(inv => inv.ProductId == poi.ProductId && (inv.QuantityOnHand - inv.QuantityReserved) >= poi.Quantity))))
                    .OrderByDescending(w => w.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved))
                    .FirstOrDefault();

                if (bestHub == null)
                {
                    // Fallback: Highest overall stock even if not all items match
                    bestHub = warehouses
                        .OrderByDescending(w => w.Inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved))
                        .FirstOrDefault();
                }

                if (bestHub != null)
                {
                    warehouseId = bestHub.Id;
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not find any suitable warehouse for auto-selection. Please select manually.";
                    return RedirectToAction("Details", new { id = orderId, poId = poId });
                }
            }

            // Atomic Status Sync & Warehouse Assignment
            po.WarehouseId = warehouseId;
            po.Status = "Accepted";
            po.UpdatedAt = DateTime.Now;

            if (po.Order != null)
            {
                po.Order.OrderStatus = "Accepted";
                po.Order.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Reserve stock via Inventory Service
            var inventoryService = (IInventoryService)HttpContext.RequestServices.GetService(typeof(IInventoryService));
            if (inventoryService != null)
            {
                await inventoryService.BulkReserveStockForPOAsync(po.Id, po.SupplierId, warehouseId);
            }

            TempData["SuccessMessage"] = warehouseId == -1 ? "Smart Auto Select successful. Fulfillment Hub assigned." : "Warehouse assigned and PO accepted successfully.";

            return RedirectToAction("Details", new { id = orderId, poId = poId });
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
                // check if user is the Supplier entity owner
                var s = await _context.Suppliers.FirstOrDefaultAsync(x => x.UserId == userId);
                if (s != null) return s.Id;

                // check if user is a SupplierEmployee (e.g. WarehouseManager)
                var se = await _context.SupplierEmployees.FirstOrDefaultAsync(x => x.UserId == userId);
                if (se != null) return se.SupplierId;
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
