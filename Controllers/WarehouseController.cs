using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
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

        public WarehouseController(ApplicationDbContext context, IPurchaseOrderService poService)
        {
            _context = context;
            _poService = poService;
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
                    .Include(e => e.Warehouse)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.EmployeeRole == "WarehouseManager");
            }
            return null;
        }

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            int wId = manager.WarehouseId.Value;
            int sId = manager.SupplierId;

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == wId && p.Status != POStatus.Cancelled)
                .ToListAsync();

            ViewBag.Manager = manager;
            ViewBag.Warehouse = manager.Warehouse;
            
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
            ViewBag.LowStockCount = inventories.Count(i => i.QuantityOnHand < 20); // Threshold 20

            // Quick Stats (Fleet & Staff)
            ViewBag.ActiveAgents = await _context.SupplierEmployees
                .CountAsync(e => e.SupplierId == sId && e.EmployeeRole == "DeliveryAgent" && e.IsActive);
            
            ViewBag.AvailableVehicles = await _context.Vehicles
                .CountAsync(v => v.SupplierId == sId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available);

            // Notifications (Recent updates)
            ViewBag.RecentAlerts = pos.OrderByDescending(p => p.UpdatedAt).Take(5).ToList();

            return View(pos);
        }

        [Route("OrdersToPick")]
        public async Task<IActionResult> OrdersToPick()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == manager.WarehouseId && (p.Status == POStatus.Processing || p.Status == POStatus.Issued || p.Status == POStatus.Accepted))
                .ToListAsync();

            return View(pos);
        }

        [Route("Packing")]
        public async Task<IActionResult> Packing()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == manager.WarehouseId && p.Status == POStatus.Picked)
                .ToListAsync();

            return View(pos);
        }

        [Route("Ready")]
        public async Task<IActionResult> Ready()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == manager.WarehouseId && p.Status == POStatus.Packed)
                .ToListAsync();

            return View(pos);
        }

        [Route("AssignDelivery")]
        public async Task<IActionResult> AssignDelivery()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var pos = await _context.PurchaseOrders
                .Include(p => p.Order)
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == manager.WarehouseId && p.Status == POStatus.Packed)
                .ToListAsync();

            ViewBag.Agents = await _context.SupplierEmployees
                .Include(e => e.User)
                .Where(e => e.SupplierId == manager.SupplierId && e.EmployeeRole == "DeliveryAgent" && e.IsActive)
                .ToListAsync();

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == manager.SupplierId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available)
                .ToListAsync();

            return View(pos);
        }

        [Route("Alerts")]
        public async Task<IActionResult> Alerts()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var lowStock = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.WarehouseId == manager.WarehouseId && i.QuantityOnHand < 20)
                .ToListAsync();

            return View(lowStock);
        }

        [Route("Details/{id?}")]
        public async Task<IActionResult> Details(int? id)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();
            
            if (id == null)
            {
                // Show Warehouse Profile
                var warehouse = await _context.Warehouses
                    .Include(w => w.Supplier)
                    .Include(w => w.Inventories)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(w => w.Id == manager.WarehouseId.Value);
                
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
                .FirstOrDefaultAsync(p => p.Id == id.Value && p.WarehouseId == manager.WarehouseId.Value);

            if (po == null) return NotFound();

            return View(po);
        }

        [Route("History")]
        public async Task<IActionResult> History()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var history = await _context.PurchaseOrders
                .Where(p => p.WarehouseId == manager.WarehouseId.Value)
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
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var warehouse = await _context.Warehouses.FindAsync(manager.WarehouseId.Value);
            
            var stats = new
            {
                TotalFulfilled = await _context.PurchaseOrders.CountAsync(p => p.WarehouseId == manager.WarehouseId.Value && p.Status == POStatus.Completed),
                CurrentDispatches = await _context.PurchaseOrders.CountAsync(p => p.WarehouseId == manager.WarehouseId.Value && p.Status == POStatus.InTransit),
                LowStockItems = await _context.Inventories.CountAsync(i => i.WarehouseId == manager.WarehouseId.Value && i.QuantityOnHand < 20),
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
            if (manager == null) return Unauthorized();

            // Verify PO belongs to this manager's warehouse
            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && p.WarehouseId == manager.WarehouseId);
            if (po == null) return Unauthorized();

            await _poService.UpdatePurchaseOrderStatusAsync(id, status, userId);

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
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.WarehouseId == manager.WarehouseId.Value)
                .ToListAsync();

            ViewBag.WarehouseName = manager.Warehouse?.Name;
            return View(inventory);
        }

        [HttpPost("UpdateInventory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(int productId, int change, string action)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.WarehouseId == manager.WarehouseId && i.ProductId == productId);
            if (inv != null)
            {
                if (action == "add") inv.QuantityOnHand += change;
                else if (action == "sub") inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - change);
                
                inv.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
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
            if (manager == null) return Unauthorized();

            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && p.WarehouseId == manager.WarehouseId);
            if (po == null) return NotFound();

            var agent = await _context.SupplierEmployees.FirstOrDefaultAsync(e => e.Id == agentId && e.EmployeeRole == "DeliveryAgent");
            if (agent == null)
            {
                TempData["ErrorMessage"] = "Invalid delivery agent.";
                return RedirectToAction(nameof(AssignDelivery));
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available);
            if (vehicle == null)
            {
                TempData["ErrorMessage"] = "The selected vehicle is no longer available.";
                return RedirectToAction(nameof(AssignDelivery));
            }

            // Assign Agent & Vehicle
            po.DeliveryAgentId = agentId;
            po.VehicleId = vehicleId;
            
            vehicle.Status = SCM_System.Models.Enums.VehicleStatus.InUse;
            vehicle.UpdatedAt = DateTime.Now;

            await _poService.UpdatePurchaseOrderStatusAsync(id, POStatus.InTransit, userId);

            TempData["SuccessMessage"] = $"Order #{po.PONumber} assigned to {agent.User?.FullName} and Vehicle {vehicle.LicensePlate}. Status: In Transit.";
            return RedirectToAction(nameof(History));
        }
    }
}
