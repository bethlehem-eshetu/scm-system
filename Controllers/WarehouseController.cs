using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;
using SCM_System.Services;
using System.Security.Claims;
using SCM_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Warehouse,WarehouseManager")]
    [Route("Warehouse")]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPurchaseOrderService _poService;
        private readonly ILogger<WarehouseController> _logger;
        private readonly IWebHostEnvironment _env;

        public WarehouseController(
            ApplicationDbContext context, 
            IPurchaseOrderService poService, 
            ILogger<WarehouseController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _poService = poService;
            _logger = logger;
            _env = env;
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
                    .Include(e => e.Warehouse)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId && (e.EmployeeRole == "WarehouseManager" || e.EmployeeRole == "warehouse_manager"));
            }
            return null;
        }

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var manager = await GetCurrentManagerAsync();
                if (manager == null || !manager.WarehouseId.HasValue) 
                {
                    _logger.LogWarning("Warehouse manager login failed: Record not found for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                    return Unauthorized();
                }

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
                ViewBag.WarehouseLocation = manager.DefaultWarehouseLocation ?? manager.Warehouse?.Address ?? "Not Set";
                
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
                ViewBag.LowStockAlertCount = inventories.Count(i => i.QuantityOnHand <= (manager?.LowStockThreshold ?? 20));

                // Quick Stats (Fleet & Staff)
                ViewBag.ActiveAgents = await _context.SupplierEmployees
                    .CountAsync(e => e.SupplierId == sId && (e.EmployeeRole == "DeliveryAgent" || e.EmployeeRole == "delivery_person") && e.IsActive);
                
                ViewBag.AvailableVehicles = await _context.Vehicles
                    .CountAsync(v => v.SupplierId == sId && v.Status == SCM_System.Models.Enums.VehicleStatus.Available);

                // Notifications (Recent updates)
                ViewBag.RecentAlerts = pos.OrderByDescending(p => p.UpdatedAt).Take(5).ToList();

                return View(pos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Warehouse Dashboard");
                return Content($"An error occurred while loading the dashboard: {ex.Message}. Check logs for details.");
            }
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

            ViewBag.PicklistFormat = manager.PicklistFormat ?? "Detailed";
            return View(pos);
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

            var threshold = manager.LowStockThreshold;
            var lowStock = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.WarehouseId == manager.WarehouseId && i.QuantityOnHand <= threshold)
                .ToListAsync();

            ViewBag.Threshold = threshold;
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
                LowStockItems = await _context.Inventories.CountAsync(i => i.WarehouseId == manager.WarehouseId.Value && i.QuantityOnHand <= manager.LowStockThreshold),
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

            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id && p.WarehouseId == manager.WarehouseId);

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
        [Route("Settings")]
        public async Task<IActionResult> Settings()
        {
            var employeeIdStr = User.FindFirstValue("EmployeeId");
            if (!int.TryParse(employeeIdStr, out int employeeId))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

                var emp = await _context.SupplierEmployees.FirstOrDefaultAsync(e => e.UserId == userId);
                if (emp == null) return Unauthorized();
                employeeId = emp.Id;
            }

            var employee = await _context.SupplierEmployees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);
            
            if (employee == null) return NotFound();

            var viewModel = new SCM_System.Models.ViewModels.WarehouseManagerSettingsViewModel
            {
                EmployeeId = employee.Id,
                FullName = employee.User?.FullName ?? "",
                Email = employee.User?.Email ?? "",
                Phone = employee.User?.PhoneNumber ?? "",
                ExistingProfileImage = employee.User?.ProfileImage,
                DefaultWarehouseLocation = employee.DefaultWarehouseLocation,
                LowStockThreshold = employee.LowStockThreshold,
                PicklistFormat = employee.PicklistFormat,
                AutoAcceptPickTasks = employee.AutoAcceptPickTasks,
                NotifyLowStock = employee.NotifyLowStock
            };

            return View(viewModel);
        }

        [HttpPost("Settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SCM_System.Models.ViewModels.WarehouseManagerSettingsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var employee = await _context.SupplierEmployees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (employee == null) return NotFound();

            // 1. Update Operational Settings
            employee.DefaultWarehouseLocation = model.DefaultWarehouseLocation;
            employee.LowStockThreshold = model.LowStockThreshold;
            employee.PicklistFormat = model.PicklistFormat;
            employee.AutoAcceptPickTasks = model.AutoAcceptPickTasks;
            employee.NotifyLowStock = model.NotifyLowStock;

            // 2. Update Personal Profile
            if (employee.User != null)
            {
                employee.User.FullName = model.FullName;
                employee.User.Email = model.Email;
                employee.User.PhoneNumber = model.Phone;

                // Sync email on the employee record if applicable
                employee.Email = model.Email;

                // Update Session variables (optional but good for UX)
                HttpContext.Session.SetString("UserName", model.FullName);
                HttpContext.Session.SetString("UserEmail", model.Email);

                // 3. Security: Password Change
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        ModelState.AddModelError("CurrentPassword", "Current password is required to set a new one.");
                        return View(model);
                    }

                    string currentHash = HashPassword(model.CurrentPassword);
                    if (employee.User.PasswordHash != currentHash)
                    {
                        ModelState.AddModelError("CurrentPassword", "The current password provided is incorrect.");
                        return View(model);
                    }

                    employee.User.PasswordHash = HashPassword(model.NewPassword);
                }

                // 4. Profile Picture Upload
                if (model.ProfilePicture != null)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(employee.User.ProfileImage))
                    {
                        string oldPath = Path.Combine(_env.WebRootPath, employee.User.ProfileImage.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string uniqueFileName = $"profile_{employee.User.Id}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(model.ProfilePicture.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(fileStream);
                    }

                    employee.User.ProfileImage = $"/uploads/profiles/{uniqueFileName}";
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "All settings and profile details updated successfully.";
            return RedirectToAction(nameof(Dashboard));
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
    }
}
