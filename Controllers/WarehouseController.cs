using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Enums;
using SCM_System.Models.Constants;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "WarehouseManager")]
    [Route("Warehouse")]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPurchaseOrderService _poService;
        private readonly IAuditLogService _auditLogService;
        private readonly ISupplierService _supplierService;

        public WarehouseController(ApplicationDbContext context, IPurchaseOrderService poService, IAuditLogService auditLogService, ISupplierService supplierService)
        {
            _context = context;
            _poService = poService;
            _auditLogService = auditLogService;
            _supplierService = supplierService;
        }

        [Route("")]
        [Route("Index")]
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));


        private async Task<int> GetWarehouseIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var emp = await _context.SupplierEmployees.FirstOrDefaultAsync(x => x.UserId == userId);
                return emp?.WarehouseId ?? 0;
            }
            return 0;
        }

        private async Task<SupplierEmployee?> GetCurrentManagerAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                return await _context.SupplierEmployees
                    .Include(e => e.User)
                    .Include(e => e.HubAccesses)
                        .ThenInclude(a => a.Warehouse)
                    .Include(e => e.WarehouseAssignments)
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.EmployeeRole == "WarehouseManager");
            }
            return null;
        }

        [Route("Dashboard/{warehouseId?}")]
        public async Task<IActionResult> Dashboard(int? warehouseId)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            // Multi-hub Logic: If no ID provided and manager has multiple, show selection
            if (warehouseId == null && manager.WarehouseAssignments.Count(wa => wa.IsActive) > 1)
            {
                var accessibleHubs = await _context.Warehouses
                    .Where(w => manager.WarehouseAssignments.Any(wa => wa.WarehouseId == w.Id && wa.IsActive))
                    .ToListAsync();
                return View("HubSelection", accessibleHubs);
            }

            int wId = warehouseId ?? manager.WarehouseId ?? 0;
            if (wId == 0 && manager.WarehouseAssignments.Any(wa => wa.IsActive)) wId = manager.WarehouseAssignments.First(wa => wa.IsActive).WarehouseId;

            // Strict Guard: Check if manager has access to this warehouse
            bool hasAccess = manager.WarehouseId == wId || 
                 manager.WarehouseAssignments.Any(wa => wa.WarehouseId == wId && wa.IsActive) ||
                 manager.HubAccesses.Any(a => a.WarehouseId == wId);

            if (!hasAccess)
            {
                return Unauthorized("You do not have access to this warehouse hub.");
            }

            var warehouse = await _context.Warehouses.FindAsync(wId);
            if (warehouse == null) return NotFound();

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == wId && p.Status != POStatus.Cancelled)
                .ToListAsync();

            ViewBag.Manager = manager;
            ViewBag.Warehouse = warehouse;
            ViewBag.SelectedWarehouseId = wId;
            
            // Comprehensive Metrics
            ViewBag.TotalOrders = pos.Count;
            ViewBag.PendingPrep = pos.Count(p => p.Status == POStatus.Issued || p.Status == POStatus.Accepted || p.Status == POStatus.Processing);
            ViewBag.ReadyForPickup = pos.Count(p => p.Status == POStatus.Packed || p.Status == POStatus.Ready);
            ViewBag.InProgress = pos.Count(p => p.Status == POStatus.InTransit);
            ViewBag.Delivered = pos.Count(p => p.Status == POStatus.Delivered || p.Status == POStatus.Completed);
            
            // Stock Summary
            var inventories = await _context.Inventories
                .Where(i => i.WarehouseId == wId)
                .ToListAsync();
            
            ViewBag.TotalReserved = inventories.Sum(i => i.QuantityReserved);
            ViewBag.TotalOnHand = inventories.Sum(i => i.QuantityOnHand);
            ViewBag.LowStockCount = inventories.Count(i => (i.QuantityOnHand - i.QuantityReserved) < 20);

            // Audit Logs for this Warehouse
            ViewBag.RecentLogs = await _auditLogService.GetLogsForEntityAsync("Warehouse", wId.ToString());

            return View(pos);
        }

        [Route("AssignDelivery/{id?}")]
        public async Task<IActionResult> AssignDelivery(int? id)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            // For now use first accessible warehouse if not specified
            int wId = manager.WarehouseId ?? manager.HubAccesses.FirstOrDefault()?.WarehouseId ?? 0;
            
            var pos = await _context.PurchaseOrders
                .Include(p => p.Order)
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == wId && p.Status == POStatus.Packed)
                .ToListAsync();

            // Smart Dispatch Suggestions
            ViewBag.SuggestedVehicles = await _supplierService.GetSmartDispatchSuggestionsAsync(wId);
            ViewBag.SuggestedDrivers = await _supplierService.GetSmartDriverSuggestionsAsync(wId);

            ViewBag.Agents = await _context.SupplierEmployees
                .Include(e => e.User)
                .Where(e => e.SupplierId == manager.SupplierId && e.EmployeeRole == "DeliveryAgent" && e.IsActive && e.WarehouseId == wId)
                .ToListAsync();

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == manager.SupplierId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available && v.WarehouseId == wId)
                .ToListAsync();

            ViewBag.SelectedWarehouseId = wId;
            return View(pos);
        }

        [Route("Packing")]
        public async Task<IActionResult> Packing()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            int wId = manager.WarehouseId ?? manager.HubAccesses.FirstOrDefault()?.WarehouseId ?? 0;

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Where(p => p.WarehouseId == wId && p.Status == POStatus.Picked)
                .ToListAsync();

            return View(pos);
        }

        [Route("Ready")]
        public async Task<IActionResult> Ready()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            int wId = manager.WarehouseId ?? manager.HubAccesses.FirstOrDefault()?.WarehouseId ?? 0;

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Where(p => p.WarehouseId == wId && p.Status == POStatus.Packed)
                .ToListAsync();

            return View(pos);
        }

        [Route("OrdersToPick")]
        public async Task<IActionResult> OrdersToPick()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            int wId = manager.WarehouseId ?? manager.HubAccesses.FirstOrDefault()?.WarehouseId ?? 0;

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Where(p => p.WarehouseId == wId && (p.Status == POStatus.Issued || p.Status == POStatus.Accepted || p.Status == POStatus.Processing))
                .ToListAsync();

            return View(pos);
        }

        [Route("Alerts")]
        public async Task<IActionResult> Alerts()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var lowStock = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.WarehouseId == wId.Value && i.QuantityOnHand < 20)
                .ToListAsync();

            return View(lowStock);
        }

        [Route("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();
            
            if (id == null)
            {
                // Show Warehouse Profile
                var warehouse = await _context.Warehouses
                    .Include(w => w.Supplier)
                    .Include(w => w.Inventories)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(w => w.Id == wId.Value);
                
                if (warehouse == null) return NotFound();
                return View("Profile", warehouse);
            }

            // Show Purchase Order Details
            var po = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .ThenInclude(i => i.Product)
                .Include(p => p.Order)
                .ThenInclude(o => o.Retailer)
                .Include(p => p.DeliveryAgent)
                    .ThenInclude(e => e.User)
                .Include(p => p.Vehicle)
                .FirstOrDefaultAsync(p => p.Id == id.Value && p.WarehouseId == wId.Value);

            if (po == null) return NotFound();

            return View(po);
        }

        [Route("History")]
        public async Task<IActionResult> History()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var history = await _context.PurchaseOrders
                .Where(p => p.WarehouseId == wId.Value)
                .Include(p => p.Order)
                .ThenInclude(o => o.Retailer)
                .Include(p => p.DeliveryAgent)
                    .ThenInclude(e => e.User)
                .Include(p => p.Vehicle)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();

            return View(history);
        }

        [Route("Reports")]
        public async Task<IActionResult> Reports()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var warehouse = await _context.Warehouses.FindAsync(wId.Value);
            
            var stats = new
            {
                TotalFulfilled = await _context.PurchaseOrders.CountAsync(p => p.WarehouseId == wId.Value && p.Status == POStatus.Completed),
                CurrentDispatches = await _context.PurchaseOrders.CountAsync(p => p.WarehouseId == wId.Value && p.Status == POStatus.InTransit),
                LowStockItems = await _context.Inventories.CountAsync(i => i.WarehouseId == wId.Value && i.QuantityOnHand < 20),
                WarehouseName = warehouse?.Name ?? "Main Warehouse"
            };

            return View(stats);
        }

        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string returnAction = "Dashboard")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            // Verify PO belongs to this manager's warehouse
            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && p.WarehouseId == wId.Value);
            if (po == null) return Unauthorized();

            await _poService.UpdatePurchaseOrderStatusAsync(id, status, userId);

            // AUDIT LOG
            await _auditLogService.LogActionAsync(
                "PurchaseOrder", 
                id.ToString(), 
                "StatusUpdate", 
                notes: $"Order #{po.PONumber} moved to {status} at {manager.Warehouse?.Name ?? wId.Value.ToString()}",
                performedByUserId: userId
            );

            TempData["SuccessMessage"] = $"Order #{po.PONumber} status updated to {status}.";
            
            // Logic-based redirects for better UX
            if (status == POStatus.Picked) return RedirectToAction(nameof(Packing));
            if (status == POStatus.Packed) return RedirectToAction(nameof(Ready));
            if (status == POStatus.Ready) return RedirectToAction(nameof(AssignDelivery));
            
            return RedirectToAction(returnAction);
        }

        [Route("Inventory")]
        public async Task<IActionResult> Inventory()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.WarehouseId == wId.Value)
                .ToListAsync();

            ViewBag.WarehouseName = (await _context.Warehouses.FindAsync(wId.Value))?.Name;
            return View(inventory);
        }

        [HttpPost("UpdateInventory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(int productId, int change, string action)
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.WarehouseId == wId.Value && i.ProductId == productId);
            if (inv != null)
            {
                if (action == "add") inv.QuantityOnHand += change;
                else if (action == "sub") inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - change);
                
                inv.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();

                // AUDIT LOG
                await _auditLogService.LogActionAsync(
                    "Inventory", 
                    inv.Id.ToString(), 
                    "Update", 
                    notes: $"Manual inventory {action}: {change} units of {inv.Product?.ProductName ?? "Product ID: " + productId}",
                    performedByUserId: manager.UserId
                );

                TempData["SuccessMessage"] = "Inventory updated successfully.";
            }

            return RedirectToAction(nameof(Inventory));
        }

        [HttpPost("AssignDelivery")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDelivery(int id, int agentId, int vehicleId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id && p.WarehouseId == wId.Value);

            if (po == null) return NotFound();

            var agent = await _context.SupplierEmployees.FirstOrDefaultAsync(e => e.Id == agentId && e.EmployeeRole == "DeliveryAgent" && e.WarehouseId == wId.Value);
            if (agent == null)
            {
                TempData["ErrorMessage"] = "Invalid delivery agent or agent not assigned to this hub.";
                return RedirectToAction(nameof(AssignDelivery));
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available && v.WarehouseId == wId.Value);
            if (vehicle == null)
            {
                TempData["ErrorMessage"] = "Selected vehicle is unavailable or not assigned to this hub.";
                return RedirectToAction(nameof(AssignDelivery));
            }

            // Assign Agent & Vehicle
            po.DeliveryAgentId = agentId;
            po.VehicleId = vehicleId;

            vehicle.Status = SCM_System.Models.Enums.VehicleStatus.InUse;
            vehicle.UpdatedAt = DateTime.Now;

            await _poService.UpdatePurchaseOrderStatusAsync(id, POStatus.InTransit, userId);

            // AUDIT LOG
            await _auditLogService.LogActionAsync(
                "PurchaseOrder", 
                id.ToString(), 
                "Dispatch", 
                notes: $"Order #{po.PONumber} dispatched with Driver {agent.User?.FullName} and Vehicle {vehicle.LicensePlate}",
                performedByUserId: userId
            );

            // ✅ FIX: Update main Order status when PO is In Transit
            var order = po.Order;
            if (order != null && order.OrderStatus != "In Transit")
            {
                // Check if any other POs are already In Transit or Delivered
                var otherPOs = await _context.PurchaseOrders
                    .Where(p => p.OrderId == order.Id && p.Id != po.Id)
                    .Select(p => p.Status)
                    .ToListAsync();

                var hasInTransitOrDelivered = otherPOs.Any(s => s == POStatus.InTransit || s == POStatus.Delivered || s == POStatus.Completed);

                if (hasInTransitOrDelivered)
                {
                    order.OrderStatus = "Partially In Transit";
                }
                else
                {
                    order.OrderStatus = "In Transit";
                }

                // Add status history
                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = order.OrderStatus,
                    Comments = $"PO #{po.PONumber} is now In Transit with delivery agent {agent.User?.FullName}",
                    ChangedByUserId = userId,
                    ChangedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Order #{po.PONumber} assigned to {agent.User?.FullName} and Vehicle {vehicle.LicensePlate}. Status: In Transit.";
            return RedirectToAction(nameof(History));
        }
    }
}
