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
    public class WarehouseController(
        ApplicationDbContext context, 
        IPurchaseOrderService poService, 
        IAuditLogService auditLogService, 
        ISupplierService supplierService, 
        IInventoryService inventoryService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IPurchaseOrderService _poService = poService;
        private readonly IAuditLogService _auditLogService = auditLogService;
        private readonly ISupplierService _supplierService = supplierService;
        private readonly IInventoryService _inventoryService = inventoryService;

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
                .Include(p => p.InventoryReservations)
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

            int wId = manager.WarehouseId ?? manager.HubAccesses.FirstOrDefault()?.WarehouseId ?? 0;

            if (id.HasValue)
            {
                var targetPO = await _context.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id.Value);
                if (targetPO != null)
                {
                    wId = targetPO.WarehouseId;
                }
            }
            
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
                .Where(e => e.SupplierId == manager.SupplierId && 
                            e.IsActive && !e.IsDeleted &&
                            !string.IsNullOrEmpty(e.EmployeeRole) &&
                            (e.EmployeeRole.ToLower().Contains("driver") || e.EmployeeRole.ToLower().Contains("deliver")))
                .ToListAsync();

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == manager.SupplierId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available && v.WarehouseId == wId)
                .ToListAsync();

            ViewBag.SelectedWarehouseId = wId;
            return View(pos);
        }

        [HttpGet("GetVehicleByAgent")]
        public async Task<IActionResult> GetVehicleByAgent(int agentId)
        {
            var vehicle = await _context.Vehicles
                .Where(v => v.PrimaryDriverId == agentId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available)
                .FirstOrDefaultAsync();

            if (vehicle == null)
                return Json(new { vehicleId = 0 });

            return Json(new
            {
                vehicleId = vehicle.Id,
                plate = vehicle.LicensePlate,
                type = vehicle.VehicleType.ToString()
            });
        }

        [Route("Packing")]
        public async Task<IActionResult> Packing()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            int wId = manager.WarehouseId ?? manager.HubAccesses.FirstOrDefault()?.WarehouseId ?? 0;

            var pos = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(p => p.InventoryReservations)
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
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(p => p.InventoryReservations)
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
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(p => p.InventoryReservations)
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
                .Where(i => i.WarehouseId == wId.Value && (i.QuantityOnHand - i.QuantityReserved) < 20)
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
                LowStockItems = await _context.Inventories.CountAsync(i => i.WarehouseId == wId.Value && (i.QuantityOnHand - i.QuantityReserved) < 20),
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

            // Phase 2: Permanent stock deduction on Pick
            if (status == POStatus.Picked)
            {
                await _inventoryService.DeductStockOnPickAsync(id);
            }

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
        public async Task<IActionResult> UpdateInventory(int productId, int change, string action, string? reason, string? adjustmentType)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var adj = new InventoryAdjustment
            {
                ProductId = productId,
                WarehouseId = wId.Value,
                QuantityChange = action == "add" ? Math.Abs(change) : -Math.Abs(change),
                AdjustmentType = adjustmentType ?? (action == "add" ? "Correction" : "Loss"),
                Reason = reason ?? $"Manual {action} of {change} units",
                PerformedById = userId,
                CreatedAt = DateTime.Now
            };

            try
            {
                await _inventoryService.AdjustInventoryAsync(adj);

                // AUDIT LOG
                await _auditLogService.LogActionAsync(
                    "Inventory", 
                    productId.ToString(), 
                    "Adjust", 
                    notes: $"Manual adjustment ({action}): {change} units. Type: {adj.AdjustmentType}. Reason: {adj.Reason}",
                    performedByUserId: userId
                );

                TempData["SuccessMessage"] = "Inventory adjusted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Could not adjust inventory: " + ex.Message;
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

            // Load PO first to determine which hub we are dealing with
            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return NotFound();

            // Verify Manager has access to this Hub
            var hubAccesses = manager.WarehouseAssignments.Where(a => a.IsActive).Select(a => a.WarehouseId).ToList();
            if (manager.WarehouseId.HasValue) hubAccesses.Add(manager.WarehouseId.Value);

            if (!hubAccesses.Contains(po.WarehouseId))
            {
                TempData["ErrorMessage"] = "You do not have authorization to dispatch orders from this warehouse.";
                return RedirectToAction(nameof(Ready));
            }

            int wId = po.WarehouseId;

            var agent = await _context.SupplierEmployees
                .FirstOrDefaultAsync(e => e.Id == agentId && 
                                          e.SupplierId == manager.SupplierId &&
                                          e.IsActive && !e.IsDeleted &&
                                          !string.IsNullOrEmpty(e.EmployeeRole) &&
                                          (e.EmployeeRole.ToLower().Contains("deliver") || e.EmployeeRole.ToLower().Contains("driver")));
                                          
            if (agent == null)
            {
                TempData["ErrorMessage"] = "Invalid delivery agent or agent is not authorized for this supplier.";
                return RedirectToAction(nameof(AssignDelivery), new { id = id });
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && 
                                                                           v.Status == SCM_System.Models.Enums.VehicleStatus.Available && 
                                                                           v.WarehouseId == wId);
            if (vehicle == null)
            {
                TempData["ErrorMessage"] = "Selected vehicle is unavailable or not assigned to this hub.";
                return RedirectToAction(nameof(AssignDelivery), new { id = id });
            }

            // ENFORCE PAIRING (Critical Rule)
            if (vehicle.PrimaryDriverId.HasValue && vehicle.PrimaryDriverId != agentId)
            {
                TempData["ErrorMessage"] = "Selected vehicle is primarily assigned to another driver. Please use the driver's designated vehicle.";
                return RedirectToAction(nameof(AssignDelivery), new { id = id });
            }

            // Assign Agent & Vehicle
            po.DeliveryAgentId = agentId;
            po.VehicleId = vehicleId;

            vehicle.Status = SCM_System.Models.Enums.VehicleStatus.InUse;
            vehicle.UpdatedAt = DateTime.Now;

            await _poService.UpdatePurchaseOrderStatusAsync(id, POStatus.InTransit, userId);
            
            TempData["SuccessMessage"] = $"Order #{po.PONumber} has been successfully dispatched.";
            return RedirectToAction(nameof(Ready));

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
            TempData["SuccessMessage"] = $"Order #{po.PONumber} assigned to {agent.User?.FullName} and Vehicle {vehicle.LicensePlate}. Status: In Transit.";
            return RedirectToAction(nameof(History));
        }

        [Route("Inbound")]
        public async Task<IActionResult> Inbound()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var shipments = await _context.InboundShipments
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Where(s => s.WarehouseId == wId.Value)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedWarehouseId = wId.Value;
            return View(shipments);
        }

        [Route("ReceiveInbound/{id}")]
        public async Task<IActionResult> ReceiveInbound(int id)
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var shipment = await _context.InboundShipments
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id && s.WarehouseId == wId.Value);

            if (shipment == null) return NotFound();
            if (shipment.Status == "Received") return RedirectToAction(nameof(Inbound));

            return View(shipment);
        }

        [HttpPost("ProcessInbound")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessInbound(int id, List<InboundItemReceiptViewModel> receipts)
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            foreach (var r in receipts)
            {
                await _inventoryService.ReceiveInboundItemAsync(id, r.ProductId, r.ReceivedQty, r.DamagedQty, r.BatchNumber, r.ExpiryDate);
            }

            TempData["SuccessMessage"] = "Shipment items updated. Click Finalize to putaway.";
            return RedirectToAction(nameof(ReceiveInbound), new { id });
        }

        [HttpPost("FinalizeInbound")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeInbound(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var success = await _inventoryService.FinalizeInboundShipmentAsync(id, userId);

            if (success)
            {
                TempData["SuccessMessage"] = "Shipment received and inventory updated.";
                return RedirectToAction(nameof(Inbound));
            }

            TempData["ErrorMessage"] = "Failed to finalize shipment.";
            return RedirectToAction(nameof(ReceiveInbound), new { id });
        }

        [HttpPost("ReconcileInventory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReconcileInventory()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            await _inventoryService.RecalculateInventoryAsync(wId.Value);

            TempData["SuccessMessage"] = "Inventory reconciliation complete. QuantityReserved has been synced with the reservation ledger.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    public class InboundItemReceiptViewModel
    {
        public int ProductId { get; set; }
        public int ReceivedQty { get; set; }
        public int DamagedQty { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
