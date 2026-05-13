using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Enums;
using SCM_System.Models.Constants;
using SCM_System.Services;
using System.Security.Claims;
using SCM_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Warehouse,WarehouseManager")]
    [Route("WarehouseManager")]
    public class WarehouseController(
        ApplicationDbContext context, 
        IPurchaseOrderService poService, 
        IAuditLogService auditLogService, 
        ISupplierService supplierService, 
        IInventoryService inventoryService,
        INotificationService notificationService,
        ILogger<WarehouseController> logger,
        IWebHostEnvironment env) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IPurchaseOrderService _poService = poService;
        private readonly IAuditLogService _auditLogService = auditLogService;
        private readonly ISupplierService _supplierService = supplierService;
        private readonly IInventoryService _inventoryService = inventoryService;
        private readonly INotificationService _notificationService = notificationService;
        private readonly ILogger<WarehouseController> _logger = logger;
        private readonly IWebHostEnvironment _env = env;

        [Route("")]
        [Route("Index")]
        [Route("Dashboard/{warehouseId?}")]
        public async Task<IActionResult> Dashboard(int? warehouseId)
        {
            try
            {
                var manager = await GetCurrentManagerAsync();
                if (manager == null) 
                {
                    _logger.LogWarning("Warehouse manager login failed: Record not found for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                    return Unauthorized();
                }

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

                int sId = manager.SupplierId;
                var warehouse = await _context.Warehouses.FindAsync(wId);
                if (warehouse == null) return NotFound();

                var pos = await _context.PurchaseOrders
                    .Include(p => p.Retailer)
                    .Include(p => p.PurchaseOrderItems)
                        .ThenInclude(i => i.Product)
                    .Include(p => p.DeliveryAgent)
                        .ThenInclude(da => da.User)
                    .Include(p => p.Vehicle)
                    .Where(p => p.WarehouseId == wId)
                    .ToListAsync();

                ViewBag.Manager = manager;
                ViewBag.Warehouse = warehouse;
                ViewBag.SelectedWarehouseId = wId;
                ViewBag.WarehouseLocation = manager.DefaultWarehouseLocation ?? warehouse?.Address ?? "Not Set";
                
                // Comprehensive Metrics for the New Premium Dashboard
                ViewBag.OrdersToPick = pos.Count(p => p.Status == POStatus.Issued || p.Status == POStatus.Accepted || p.Status == POStatus.Picking);
                ViewBag.PackingInProgress = pos.Count(p => p.Status == POStatus.Processing);
                ViewBag.PackedAndReady = pos.Count(p => p.Status == POStatus.Packed || p.Status == POStatus.Ready);
                ViewBag.InTransit = pos.Count(p => p.Status == POStatus.InTransit);
                ViewBag.DeliveredCount = pos.Count(p => p.Status == POStatus.Delivered || p.Status == POStatus.Completed);
                
                // Stock Summary & Financials
                var inventories = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.WarehouseId == wId)
                    .ToListAsync();
                
                ViewBag.TotalCommittedItems = inventories.Sum(i => i.QuantityReserved);
                ViewBag.OnHandUnits = inventories.Sum(i => i.QuantityOnHand);
                ViewBag.StockValue = inventories.Sum(i => i.QuantityOnHand * (i.Product.CostPrice ?? i.Product.BasePrice));
                
                // Hub Utilization (Gauge Data)
                ViewBag.TotalCapacity = warehouse?.MaxCapacity ?? 1000;
                ViewBag.HubUtilization = warehouse?.MaxCapacity > 0 
                    ? (int)Math.Round((double)ViewBag.OnHandUnits / warehouse.MaxCapacity * 100) 
                    : 0;

                // Low Stock Alerts (Replenishment)
                ViewBag.LowStockProducts = inventories
                    .Where(i => i.QuantityOnHand <= (manager?.LowStockThreshold ?? 20))
                    .Select(i => new { i.Id, Name = i.Product.ProductName, Stock = i.QuantityOnHand })
                    .ToList();

                // Logistics & Fleet Resources
                ViewBag.ActiveAgents = await _context.SupplierEmployees
                    .CountAsync(e => e.SupplierId == sId && (e.EmployeeRole == "DeliveryAgent" || e.EmployeeRole == "delivery_person") && e.IsActive);
                
                ViewBag.AvailableFleet = await _context.Vehicles
                    .CountAsync(v => v.SupplierId == sId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available);

                // Activity Log Formatting
                var rawLogs = await _auditLogService.GetLogsForEntityAsync("Warehouse", wId.ToString());
                ViewBag.ActivityLogs = rawLogs.Take(8).Select(l => {
                    dynamic expando = new System.Dynamic.ExpandoObject();
                    expando.Timestamp = l.PerformedAtUtc.ToLocalTime();
                    expando.Description = l.Notes;
                    expando.Icon = l.ActionType switch { "StatusUpdate" => "fa-sync", "Inventory" => "fa-boxes", _ => "fa-info-circle" };
                    return expando;
                }).ToList();

                // Hourly Picking Trend (Mock for Chart.js)
                ViewBag.HourlyLabels = new[] { "8 AM", "9 AM", "10 AM", "11 AM", "12 PM", "1 PM", "2 PM", "3 PM", "4 PM", "5 PM" };
                ViewBag.HourlyPicks = new[] { 12, 19, 3, 5, 2, 3, 10, 15, 20, 14 }; // In a real app, this would be queried from logs

                // Smart Dispatch Suggestion
                var nextReady = pos.FirstOrDefault(p => p.Status == POStatus.Packed);
                ViewBag.RecommendedDispatchMessage = nextReady != null 
                    ? $"Order #{nextReady.PONumber} for {nextReady.Retailer?.BusinessName} is ready for loading."
                    : "No pending dispatches. All orders are currently processing or delivered.";
                ViewBag.HasPendingDispatch = nextReady != null;

                // PurchaseOrders for Worklist
                ViewBag.PurchaseOrders = pos.OrderByDescending(p => p.UpdatedAt).Take(10).ToList();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Warehouse Dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard: " + ex.Message;
                return RedirectToAction("Login", "Account");
            }
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
                if (targetPO != null && targetPO.WarehouseId.HasValue)
                {
                    wId = targetPO.WarehouseId.Value;
                }
            }
            
            var pos = await _context.PurchaseOrders
                .Include(p => p.Order)
                .Include(p => p.Retailer)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.WarehouseId == wId && p.Status == POStatus.Packed)
                .ToListAsync();

            // Calculate Order Weights for each PO in the list
            // We'll store these in a temporary dictionary or dynamic object if needed, 
            // but for now, the View can handle it since we Inclulde Items.

            // Fetch agents with their vehicles for workload calculation
            var agents = await _context.SupplierEmployees
                .Include(e => e.User)
                .Include(e => e.VehicleAssignments)
                    .ThenInclude(va => va.Vehicle) // Fixed navigation path
                .Where(e => e.SupplierId == manager.SupplierId && 
                            e.IsActive && !e.IsDeleted &&
                            !string.IsNullOrEmpty(e.EmployeeRole) &&
                            (e.EmployeeRole.ToLower().Contains("driver") || e.EmployeeRole.ToLower().Contains("deliver")))
                .ToListAsync();

            // PROACTIVE SORTING LOGIC
            // For a single PO assignment (if id is passed), we sort by PROJECTED load.
            // If viewing the general list, we'll sort by current workload.
            decimal targetOrderWeight = 0;
            if (id.HasValue)
            {
                var targetPoForWeight = pos.FirstOrDefault(p => p.Id == id.Value);
                if (targetPoForWeight != null)
                {
                    targetOrderWeight = targetPoForWeight.PurchaseOrderItems.Sum(i => i.Quantity * (i.Product.ShippingWeight ?? 0));
                }
            }

            // Enhanced sorting with workload awareness
            var sortedAgents = new List<dynamic>();
            foreach (var agent in agents)
            {
                var activePOs = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderItems)
                        .ThenInclude(i => i.Product)
                    .Where(p => p.DeliveryAgentId == agent.Id && 
                                p.Status != POStatus.Failed)
                    .ToListAsync();

                decimal currentLoadKg = activePOs.Sum(p => p.PurchaseOrderItems.Sum(i => i.Quantity * (i.Product.ShippingWeight ?? 0)));
                
                // Get primary vehicle capacity
                var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.PrimaryDriverId == agent.Id || v.DeliveryAgents.Any(da => da.Id == agent.Id));
                decimal capacity = vehicle?.MaxLoadCapacity ?? 100; // fallback

                decimal projectedScore = capacity > 0 ? (currentLoadKg + targetOrderWeight) / capacity : 2; // high score if no capacity

                sortedAgents.Add(new { 
                    Agent = agent, 
                    ProjectedScore = projectedScore, 
                    CurrentLoad = currentLoadKg,
                    Capacity = capacity,
                    IsPhysicsViolation = targetOrderWeight > capacity,
                    VehicleId = vehicle?.Id ?? 0
                });
            }

            // Order by Projection (Safe -> Moderate -> High -> Violation), then by Rank
            ViewBag.SortedAgents = sortedAgents.OrderBy(a => a.IsPhysicsViolation).ThenBy(a => a.ProjectedScore).ToList();
            ViewBag.Agents = agents; // Keep original for backwards compatibility in view if needed

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == manager.SupplierId && (v.Status == SCM_System.Models.Enums.VehicleStatus.Available || v.Status == SCM_System.Models.Enums.VehicleStatus.InUse) && v.WarehouseId == wId)
                .ToListAsync();

            ViewBag.SelectedWarehouseId = wId;
            ViewBag.PicklistFormat = manager.PicklistFormat ?? "Detailed";
            return View(pos);
        }

        [HttpGet("GetVehicleByAgent")]
        public async Task<IActionResult> GetVehicleByAgent(int agentId)
        {
            var vehicle = await _context.Vehicles
                .Where(v => v.PrimaryDriverId == agentId && (v.Status == SCM_System.Models.Enums.VehicleStatus.Available || v.Status == SCM_System.Models.Enums.VehicleStatus.InUse))
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

        [HttpGet]
        [Route("AddStock")]
        public async Task<IActionResult> AddStock()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            var products = await _context.Products
                .Where(p => p.SupplierId == manager.SupplierId && !p.IsDeleted)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.Products = new SelectList(products, "Id", "ProductName");
            return View(new AddStockViewModel());
        }

        [HttpPost]
        [Route("AddStock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(AddStockViewModel model)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || !manager.WarehouseId.HasValue) return Unauthorized();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Update Inventory
                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == model.ProductId && i.WarehouseId == manager.WarehouseId);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            ProductId = model.ProductId,
                            WarehouseId = manager.WarehouseId.Value,
                            QuantityOnHand = model.QuantityToAdd,
                            QuantityReserved = 0,
                            WarehouseLocation = manager.DefaultWarehouseLocation ?? "Main Floor",
                            LastUpdated = DateTime.Now
                        };
                        _context.Inventories.Add(inventory);
                    }
                    else
                    {
                        inventory.QuantityOnHand += model.QuantityToAdd;
                        inventory.LastUpdated = DateTime.Now;
                    }

                    // 2. Log History
                    var history = new InventoryHistory
                    {
                        ProductId = model.ProductId,
                        WarehouseId = manager.WarehouseId.Value,
                        SupplierEmployeeId = manager.Id,
                        Quantity = model.QuantityToAdd,
                        BatchNumber = model.BatchNumber,
                        ExpiryDate = model.ExpiryDate,
                        Notes = model.Notes,
                        Timestamp = DateTime.Now
                    };
                    _context.InventoryHistories.Add(history);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Stock successfully added to inventory!";
                    return RedirectToAction(nameof(Dashboard));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating inventory: " + ex.Message);
                }
            }

            // If we got here, something failed; redisplay form
            var products = await _context.Products
                .Where(p => p.SupplierId == manager.SupplierId && !p.IsDeleted)
                .ToListAsync();
            ViewBag.Products = new SelectList(products, "Id", "ProductName", model.ProductId);
            return View(model);
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
                .Where(p => p.WarehouseId == wId && (p.Status == POStatus.Issued || p.Status == POStatus.Accepted || p.Status == POStatus.Picking))
                .ToListAsync();

            return View(pos);
        }

        [Route("Alerts")]
        public async Task<IActionResult> Alerts()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var threshold = manager.LowStockThreshold;
            var lowStock = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.WarehouseId == wId.Value && (i.QuantityOnHand - i.QuantityReserved) < (threshold > 0 ? threshold : 20))
                .ToListAsync();

            ViewBag.Threshold = threshold;
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
                LowStockItems = await _context.Inventories.CountAsync(i => i.WarehouseId == wId.Value && (i.QuantityOnHand - i.QuantityReserved) < (manager.LowStockThreshold > 0 ? manager.LowStockThreshold : 20)),
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
            
            if (po == null) return NotFound();

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
        public async Task<IActionResult> AssignDelivery(int id, int agentId, int vehicleId, bool isOverride = false, string overrideReason = null)
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

            if (!po.WarehouseId.HasValue || !hubAccesses.Contains(po.WarehouseId.Value))
            {
                TempData["ErrorMessage"] = "You do not have authorization to dispatch orders from this warehouse.";
                return RedirectToAction(nameof(Ready));
            }

            int wId = po.WarehouseId ?? 0;

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
                                                                           (v.Status == SCM_System.Models.Enums.VehicleStatus.Available || v.Status == SCM_System.Models.Enums.VehicleStatus.InUse) && 
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
            // === SMART LOAD OVERRIDE LOGIC (Strict KG Physics) ===
            var activeOrders = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(p => p.DeliveryAgentId == agentId && 
                            p.Status != POStatus.Delivered &&
                            p.Status != POStatus.Completed &&
                            p.Status != POStatus.Failed &&
                            p.Status != POStatus.Rejected &&
                            p.Status != POStatus.Expired)
                .ToListAsync();

            decimal currentLoadKg = activeOrders.Sum(p => p.PurchaseOrderItems.Sum(i => i.Quantity * (i.Product.ShippingWeight ?? 0)));
            decimal newOrderWeightKg = po.PurchaseOrderItems != null ? po.PurchaseOrderItems.Sum(i => i.Quantity * (i.Product.ShippingWeight ?? 0)) : 10m;
            decimal totalProjectedKg = currentLoadKg + newOrderWeightKg;
            decimal vehicleCapacityKg = vehicle.MaxLoadCapacity;

            // 1. REJECT (Structural Physics Violation)
            // If the single order is heavier than the truck can physically handle.
            if (newOrderWeightKg > vehicleCapacityKg)
            {
                TempData["ErrorMessage"] = $"PHYSICAL CONSTRAINT ERR: This order ({newOrderWeightKg} KG) is physically too heavy for the selected vehicle ({vehicleCapacityKg} KG). Dispatch restricted.";
                return RedirectToAction(nameof(AssignDelivery), new { id = id });
            }

            // 2. OVERLOAD (Operational Efficiency Violation)
            // If the combined load exceeds capacity, but the single order fits.
            if (totalProjectedKg > vehicleCapacityKg && !isOverride)
            {
                int projectedScore = (int)((totalProjectedKg / vehicleCapacityKg) * 100);
                TempData["ErrorMessage"] = $"Agent will be overloaded (Projected: {totalProjectedKg} / {vehicleCapacityKg} KG - {projectedScore}%). Assignment restricted. Please select another agent or explicitly override with a stated reason.";
                return RedirectToAction(nameof(AssignDelivery), new { id = id });
            }

            // Register the Override
            if (totalProjectedKg > vehicleCapacityKg && isOverride)
            {
                po.IsDispatchOverride = true;
                po.DispatchOverrideReason = overrideReason;
                
                var overrideLog = new DispatchOverrideLog
                {
                    PurchaseOrderId = po.Id,
                    AgentId = agentId,
                    PerformedByUserId = userId,
                    Reason = overrideReason ?? "Emergency Capacity Override",
                    CurrentLoad = (int)totalProjectedKg,
                    CreatedAt = DateTime.Now
                };
                _context.DispatchOverrideLogs.Add(overrideLog);

                await _auditLogService.LogActionAsync(
                    "DispatchOverride",
                    po.Id.ToString(),
                    "Override",
                    notes: $"Agent overloaded. Dispatch forced. Reason: {overrideReason}",
                    performedByUserId: userId
                );
            }
            // ================================

            // Assign Agent & Vehicle
            po.DeliveryAgentId = agentId;
            po.VehicleId = vehicleId;

            vehicle.Status = SCM_System.Models.Enums.VehicleStatus.InUse;
            vehicle.UpdatedAt = DateTime.Now;

            // Save assignments first so the Service can see them in its own DB lookup
            await _context.SaveChangesAsync();

            await _poService.UpdatePurchaseOrderStatusAsync(id, POStatus.InTransit, userId);
            
            // Notify Delivery Agent
            if (agent.UserId != 0)
            {
                await _notificationService.SendNotificationAsync(
                    agent.UserId,
                    "New Delivery Assigned",
                    $"Order #{po.PONumber} has been assigned to you. Vehicle: {vehicle.LicensePlate}.",
                    "Delivery",
                    $"/DeliveryAgent/Dashboard"
                );
            }

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

    public class InboundItemReceiptViewModel
    {
        public int ProductId { get; set; }
        public int ReceivedQty { get; set; }
        public int DamagedQty { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

        [Route("OperationalSettings")]
        public async Task<IActionResult> OperationalSettings()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            var viewModel = new SCM_System.Models.ViewModels.WarehouseManagerSettingsViewModel
            {
                EmployeeId = manager.Id,
                FullName = manager.User?.FullName ?? "",
                Email = manager.User?.Email ?? "",
                Phone = manager.User?.PhoneNumber ?? "",
                ExistingProfilePhoto = manager.User?.ProfileImage,
                DefaultWarehouseLocation = manager.DefaultWarehouseLocation,
                LowStockThreshold = manager.LowStockThreshold,
                PicklistFormat = manager.PicklistFormat,
                AutoAcceptPickTasks = manager.AutoAcceptPickTasks,
                NotifyLowStock = manager.NotifyLowStock,
                DefaultPackingPriority = manager.DefaultPackingPriority,
                DailyCutoffTime = manager.DailyCutoffTime,
                PrintLabelFormat = manager.PrintLabelFormat,
                EnableVoicePicking = manager.EnableVoicePicking,
                AssignedZones = string.IsNullOrEmpty(manager.AssignedZones) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(manager.AssignedZones) ?? new List<string>()
            };

            return View(viewModel);
        }

        [HttpPost("OperationalSettings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OperationalSettings(SCM_System.Models.ViewModels.WarehouseManagerSettingsViewModel model)
        {
            var manager = await _context.SupplierEmployees.FindAsync(model.EmployeeId);
            if (manager == null) return NotFound();

            manager.DefaultWarehouseLocation = model.DefaultWarehouseLocation;
            manager.LowStockThreshold = model.LowStockThreshold;
            manager.PicklistFormat = model.PicklistFormat;
            manager.AutoAcceptPickTasks = model.AutoAcceptPickTasks;
            manager.NotifyLowStock = model.NotifyLowStock;
            manager.DefaultPackingPriority = model.DefaultPackingPriority;
            manager.DailyCutoffTime = model.DailyCutoffTime;
            manager.PrintLabelFormat = model.PrintLabelFormat;
            manager.EnableVoicePicking = model.EnableVoicePicking;
            manager.AssignedZones = JsonSerializer.Serialize(model.AssignedZones ?? new List<string>());

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Operational preferences updated successfully.";
            return RedirectToAction(nameof(OperationalSettings));
        }

        [Route("Settings")]
        public async Task<IActionResult> Settings()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || manager.User == null) return Unauthorized();

            var viewModel = new SCM_System.Models.ViewModels.WarehouseManagerSettingsViewModel
            {
                EmployeeId = manager.Id,
                FullName = manager.User.FullName,
                Email = manager.User.Email,
                Phone = manager.User.PhoneNumber ?? "",
                ExistingProfilePhoto = manager.User.ProfileImage,
                
                // Operational
                DefaultWarehouseLocation = manager.DefaultWarehouseLocation,
                LowStockThreshold = manager.LowStockThreshold,
                PicklistFormat = manager.PicklistFormat,
                AutoAcceptPickTasks = manager.AutoAcceptPickTasks,
                NotifyLowStock = manager.NotifyLowStock,
                DefaultPackingPriority = manager.DefaultPackingPriority,
                DailyCutoffTime = manager.DailyCutoffTime,
                PrintLabelFormat = manager.PrintLabelFormat,
                EnableVoicePicking = manager.EnableVoicePicking,
                AssignedZones = string.IsNullOrEmpty(manager.AssignedZones) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(manager.AssignedZones) ?? new List<string>(),

                // Notifications
                EnableTaskAlerts = manager.EnableTaskAlerts,
                EnableReminders = manager.EnableReminders
            };

            // Security Details
            ViewBag.TfaEnabled = manager.User.TwoFactorEnabled;
            
            // Active Sessions
            ViewBag.ActiveSessions = await _context.UserSessions
                .Where(s => s.UserId == manager.UserId && s.IsActive)
                .OrderByDescending(s => s.LastActivityTime)
                .ToListAsync();

            // Login History
            ViewBag.LoginHistory = await _context.AuditLogs
                .Where(l => l.PerformedByUserId == manager.UserId && l.ActionType == "Login")
                .OrderByDescending(l => l.PerformedAtUtc)
                .Take(10)
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost("Settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SCM_System.Models.ViewModels.WarehouseManagerSettingsViewModel model)
        {
            var manager = await _context.SupplierEmployees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (manager == null || manager.User == null) return NotFound();

            // 1. Update Identity
            manager.User.FullName = model.FullName;
            manager.User.PhoneNumber = model.Phone;
            manager.FullName = model.FullName;
            manager.Phone = model.Phone;
            
            // 2. Operational Logic
            manager.DefaultWarehouseLocation = model.DefaultWarehouseLocation;
            manager.LowStockThreshold = model.LowStockThreshold;
            manager.PicklistFormat = model.PicklistFormat;
            manager.AutoAcceptPickTasks = model.AutoAcceptPickTasks;
            manager.NotifyLowStock = model.NotifyLowStock;
            manager.DefaultPackingPriority = model.DefaultPackingPriority;
            manager.DailyCutoffTime = model.DailyCutoffTime;
            manager.PrintLabelFormat = model.PrintLabelFormat;
            manager.EnableVoicePicking = model.EnableVoicePicking;
            manager.AssignedZones = JsonSerializer.Serialize(model.AssignedZones ?? new List<string>());

            // 3. Notification Preferences
            manager.EnableTaskAlerts = model.EnableTaskAlerts;
            manager.EnableReminders = model.EnableReminders;

            // 4. Profile Picture
            if (model.ProfilePicture != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"profile_{manager.UserId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(model.ProfilePicture.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                manager.User.ProfileImage = $"/uploads/profiles/{uniqueFileName}";
                manager.ProfilePhotoPath = manager.User.ProfileImage;

                // Sync Session for real-time header update
                HttpContext.Session.SetString("ProfileImg", manager.User.ProfileImage ?? "/img/avatars/default.png");
            }

            // 5. Password Security
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required.");
                    return await Settings();
                }

                if (manager.User.PasswordHash != HashPassword(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
                    return await Settings();
                }

                manager.User.PasswordHash = HashPassword(model.NewPassword);
            }

            if (!ModelState.IsValid)
            {
                return await Settings();
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost("Toggle2FA")]
        public async Task<IActionResult> Toggle2FA(bool enable)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || manager.User == null) return Json(new { success = false, message = "User not found." });

            if (!enable)
            {
                manager.User.TwoFactorEnabled = false;
                manager.User.TwoFactorSecret = null;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            // Generate Secret using GoogleAuthenticator
            string secret = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10).ToUpper();
            manager.User.TwoFactorSecret = secret;
            await _context.SaveChangesAsync();

            // Using Google.Authenticator library
            var tfa = new Google.Authenticator.TwoFactorAuthenticator();
            var setupInfo = tfa.GenerateSetupCode("SCM System", manager.User.Email, secret, false, 3);
            
            return Json(new { success = true, qrCodeUri = setupInfo.QrCodeSetupImageUrl, manualEntryKey = setupInfo.ManualEntryKey });
        }

        [HttpPost("Verify2FA")]
        public async Task<IActionResult> Verify2FA(string code)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || manager.User == null || string.IsNullOrEmpty(manager.User.TwoFactorSecret)) 
                return Json(new { success = false, message = "Invalid setup." });

            var tfa = new Google.Authenticator.TwoFactorAuthenticator();
            bool isValid = tfa.ValidateTwoFactorPIN(manager.User.TwoFactorSecret, code);

            if (isValid)
            {
                manager.User.TwoFactorEnabled = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Invalid verification code." });
        }

        [HttpPost("RevokeSession")]
        public async Task<IActionResult> RevokeSession(string sessionId)
        {
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == sessionId);
            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Session not found." });
        }

        [HttpPost("DeactivateAccount")]
        public async Task<IActionResult> DeactivateAccount(string password)
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null || manager.User == null) return Json(new { success = false, message = "User not found." });

            if (manager.User.PasswordHash != HashPassword(password))
            {
                return Json(new { success = false, message = "Incorrect password." });
            }

            manager.User.AccountStatus = "Inactive";
            manager.IsActive = false;
            await _context.SaveChangesAsync();

            // Audit
            await _auditLogService.LogActionAsync("User", manager.UserId.ToString(), "Deactivate", notes: "Account self-deactivated by Warehouse Manager");

            // Sign out
            HttpContext.Session.Clear();
            return Json(new { success = true });
        }

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

        [HttpPost]
        [Route("ToggleVoicePicking")]
        public async Task<IActionResult> ToggleVoicePicking()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Json(new { success = false, message = "Unauthorized" });

            manager.EnableVoicePicking = !manager.EnableVoicePicking;
            _context.Update(manager);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("Warehouse", manager.WarehouseId?.ToString() ?? "unknown", 
                "SettingsUpdate", $"Voice picking turned {(manager.EnableVoicePicking ? "ON" : "OFF")}", User.Identity.Name);

            return Json(new { success = true, message = $"Voice picking {(manager.EnableVoicePicking ? "ON" : "OFF")}" });
        }

        [HttpGet]
        [Route("PrintWorklist")]
        public IActionResult PrintWorklist()
        {
            // Mock PDF generation - in real scenario use a library like Rotativa or QuestPDF
            return Content("Worklist PDF would be generated here with all pending 'Picking' and 'Processing' orders.");
        }

        [HttpGet]
        [Route("ScanBarcode")]
        public async Task<IActionResult> ScanBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode)) return RedirectToAction(nameof(Dashboard));

            // Logic to find item or PO by barcode
            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.PONumber == barcode);
            if (po != null) return RedirectToAction("Details", new { id = po.Id });

            TempData["ErrorMessage"] = $"No entity found for barcode: {barcode}";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        [Route("GetActivityLog")]
        public async Task<IActionResult> GetActivityLog()
        {
            var manager = await GetCurrentManagerAsync();
            if (manager == null) return Unauthorized();

            var logs = await _auditLogService.GetLogsForEntityAsync("Warehouse", (manager.WarehouseId ?? 0).ToString());
            var formattedLogs = logs.Take(10).Select(l => new {
                time = l.PerformedAtUtc.ToLocalTime().ToString("HH:mm"),
                text = l.Notes,
                icon = l.ActionType switch { "StatusUpdate" => "fa-sync", "Inventory" => "fa-boxes", _ => "fa-info-circle" }
            });

            return Json(formattedLogs);
        }

        private async Task<SupplierEmployee?> GetCurrentManagerAsync()
        {
            var employeeIdStr = User.FindFirstValue("EmployeeId");
            if (int.TryParse(employeeIdStr, out int employeeId))
            {
                return await _context.SupplierEmployees
                    .Include(e => e.Warehouse)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);
            }
            
            // Fallback to UserId if claim is missing
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                return await _context.SupplierEmployees
                    .Include(e => e.User)
                    .Include(e => e.HubAccesses)
                        .ThenInclude(a => a.Warehouse)
                    .Include(e => e.WarehouseAssignments)
                    .FirstOrDefaultAsync(e => e.UserId == userId && (e.EmployeeRole == "WarehouseManager" || e.EmployeeRole == "warehouse_manager"));
            }
            return null;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        [HttpPost]
        [Route("UpdateProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(int id, string name, string address, string city, string region, string? phone, string? email, string? workingDays, string? operatingHoursFrom, string? operatingHoursTo)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.PrimaryManager)
                .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(w => w.Id == id);
            
            if (warehouse == null) return NotFound();

            warehouse.Name = name;
            warehouse.Address = address;
            warehouse.City = city;
            warehouse.Region = region;
            warehouse.WorkingDays = workingDays;
            
            if (TimeSpan.TryParse(operatingHoursFrom, out var fromTime)) warehouse.OperatingHoursFrom = fromTime;
            if (TimeSpan.TryParse(operatingHoursTo, out var toTime)) warehouse.OperatingHoursTo = toTime;

            if (warehouse.PrimaryManager != null)
            {
                warehouse.PrimaryManager.Phone = phone ?? "";
                if (warehouse.PrimaryManager.User != null)
                {
                    warehouse.PrimaryManager.User.Email = email ?? "";
                }
            }

            warehouse.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Details");
        }

        [HttpGet]
        [Route("ExportProfile")]
        public async Task<IActionResult> ExportProfile()
        {
            var manager = await GetCurrentManagerAsync();
            int? wId = manager?.WarehouseId ?? manager?.WarehouseAssignments.FirstOrDefault(a => a.IsActive)?.WarehouseId;
            if (manager == null || !wId.HasValue) return Unauthorized();

            var warehouse = await _context.Warehouses
                .Include(w => w.Supplier)
                .Include(w => w.PrimaryManager)
                .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(w => w.Id == wId.Value);

            if (warehouse == null) return NotFound();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Field,Value");
            csv.AppendLine($"Warehouse Name,{warehouse.Name}");
            csv.AppendLine($"Warehouse ID,{warehouse.Id}");
            csv.AppendLine($"Status,{warehouse.Status}");
            csv.AppendLine($"Address,{warehouse.Address}, {warehouse.City}, {warehouse.Region}");
            csv.AppendLine($"Manager,{warehouse.PrimaryManager?.User?.FullName ?? "N/A"}");
            csv.AppendLine($"Phone,{warehouse.PrimaryManager?.Phone ?? "N/A"}");
            csv.AppendLine($"Email,{warehouse.PrimaryManager?.User?.Email ?? "N/A"}");
            csv.AppendLine($"Capacity,{warehouse.MaxCapacity}");
            csv.AppendLine($"Working Days,{warehouse.WorkingDays ?? "N/A"}");
            csv.AppendLine($"Operating Hours,{warehouse.OperatingHoursFrom?.ToString() ?? "N/A"} - {warehouse.OperatingHoursTo?.ToString() ?? "N/A"}");
            csv.AppendLine($"Exported At,{DateTime.Now}");

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", $"Warehouse_Profile_{warehouse.Id}.csv");
        }
    }
}
