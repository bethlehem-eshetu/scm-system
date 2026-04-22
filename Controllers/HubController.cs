using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Models.Enums;
using SCM_System.Models.Constants;
using SCM_System.Services;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    public class HubController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SCM_System.Services.ISupplierService _supplierService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IWebHostEnvironment _env;

        public HubController(ApplicationDbContext context, SCM_System.Services.ISupplierService supplierService, IAuditLogService auditLogService, INotificationService notificationService, IWebHostEnvironment env)
        {
            _context = context;
            _supplierService = supplierService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _env = env;
        }

        private async Task<List<int>> GetAccessibleWarehouseIdsAsync(int userId, int supplierId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new List<int>();

            // If the user is the direct Supplier owner/admin, they see all warehouses for that supplier
            if (user.Role == "Supplier")
            {
                return await _context.Warehouses
                    .Where(w => w.SupplierId == supplierId && !w.IsDeleted)
                    .Select(w => w.Id)
                    .ToListAsync();
            }

            // If the user is an employee, they only see warehouses they have explicit access to
            return await _context.EmployeeWarehouseAccesses
                .Where(ewa => ewa.SupplierEmployee.UserId == userId && ewa.IsActive && !ewa.Warehouse.IsDeleted)
                .Select(ewa => ewa.WarehouseId)
                .ToListAsync();
        }

        // ================= WAREHOUSE MANAGEMENT =================

        // GET: /Supplier/Warehouses
        public async Task<IActionResult> Warehouses()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var accessibleWarehouseIds = await GetAccessibleWarehouseIdsAsync(userId.Value, supplier.Id);

            var warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && !w.IsDeleted)
                .Where(w => accessibleWarehouseIds.Contains(w.Id) || accessibleWarehouseIds.Count == 0)
                .Include(w => w.PrimaryManager)
                    .ThenInclude(se => se.User)
                .Include(w => w.Assignments)
                    .ThenInclude(a => a.SupplierEmployee)
                        .ThenInclude(se => se.User)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(warehouses);
        }

        // GET: /Supplier/CreateWarehouse
        public async Task<IActionResult> CreateWarehouse()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null || !supplier.User.IsFaydaVerified)
            {
                TempData["ErrorMessage"] = "Identity verification (Fayda) required for this action.";
                return RedirectToAction("Dashboard", "Supplier");
            }

            return View();
        }

        // POST: /Supplier/CreateWarehouse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWarehouse([Bind("Name,WarehouseCode,HubType,Region,City,SubCityZone,Address,Landmark,Latitude,Longitude,StorageArchitecture,MaxCapacity,AvgProcessingTimeHours,Timezone,WorkingDays,WeekendDays,OperatingHoursFrom,OperatingHoursTo,LoadingBays,ForkliftsAvailable,CCTVEnabled,FireSafetyInstalled,ReceivingAreaSizeM2,PackingStationsCount,HasInternet,HasBackupPower,HazardStorageAllowed,SupportsDelivery,OverflowWarningThreshold")] Warehouse warehouse)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (string.IsNullOrWhiteSpace(warehouse.Name)) ModelState.AddModelError("Name", "Name is required");
            if (string.IsNullOrWhiteSpace(warehouse.Region)) ModelState.AddModelError("Region", "Region is required");
            if (string.IsNullOrWhiteSpace(warehouse.City)) ModelState.AddModelError("City", "City is required");
            if (string.IsNullOrWhiteSpace(warehouse.Address)) ModelState.AddModelError("Address", "Address is required");

            ModelState.Remove("Supplier");
            ModelState.Remove("Country");
            ModelState.Remove("Inventories");
            ModelState.Remove("Employees");
            ModelState.Remove("Assignments");

            if (ModelState.IsValid)
            {
                warehouse.SupplierId = supplier.Id;
                warehouse.Country = "Ethiopia";
                warehouse.Status = SCM_System.Models.Enums.WarehouseStatus.Active;
                warehouse.SupportsDelivery = true;
                warehouse.CreatedAt = DateTime.Now;

                // Auto-generate Warehouse Code if not provided
                if (string.IsNullOrWhiteSpace(warehouse.WarehouseCode))
                {
                    warehouse.WarehouseCode = await GenerateWarehouseCode(supplier.Id, warehouse.HubType);
                }

                _context.Warehouses.Add(warehouse);
                await _context.SaveChangesAsync();

                // AUDIT LOG
                await _auditLogService.LogActionAsync(
                    "Warehouse", 
                    warehouse.Id.ToString(), 
                    "Create", 
                    notes: $"New warehouse '{warehouse.Name}' created at {warehouse.City}",
                    performedByUserId: userId
                );
                
                TempData["SuccessMessage"] = "Warehouse added successfully.";
                return RedirectToAction(nameof(Warehouses));
            }
            return View(warehouse);
        }

        private async Task<string> GenerateWarehouseCode(int supplierId, SCM_System.Models.Enums.HubType hubType)
        {
            string prefix = hubType switch
            {
                SCM_System.Models.Enums.HubType.DistributionCenter => "DC",
                SCM_System.Models.Enums.HubType.FulfillmentCenter => "FC",
                SCM_System.Models.Enums.HubType.ColdStorage => "CS",
                SCM_System.Models.Enums.HubType.CrossDock => "XD",
                _ => "WH"
            };

            var lastWarehouse = await _context.Warehouses
                .Where(w => w.SupplierId == supplierId && w.HubType == hubType)
                .OrderByDescending(w => w.WarehouseCode)
                .FirstOrDefaultAsync();
            
            if (lastWarehouse == null || string.IsNullOrWhiteSpace(lastWarehouse.WarehouseCode))
                return $"{prefix}-001";
            
            try 
            {
                var parts = lastWarehouse.WarehouseCode.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
                {
                    return $"{prefix}-{(lastNumber + 1):D3}";
                }
            }
            catch {}
            
            return $"{prefix}-001";
        }

        // GET: /Supplier/EditWarehouse/5
        public async Task<IActionResult> EditWarehouse(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id && w.SupplierId == supplier.Id);
            if (warehouse == null) return NotFound();

            return View(warehouse);
        }

        // POST: /Supplier/EditWarehouse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWarehouse(int id, [Bind("Id,SupplierId,Name,WarehouseCode,HubType,Country,Region,City,SubCityZone,Address,Landmark,Latitude,Longitude,StorageArchitecture,MaxCapacity,AvgProcessingTimeHours,Timezone,WorkingDays,WeekendDays,Status,IsDefault,SupportsDelivery,CreatedAt,OperatingHoursFrom,OperatingHoursTo,LoadingBays,ForkliftsAvailable,CCTVEnabled,FireSafetyInstalled,ReceivingAreaSizeM2,PackingStationsCount,HasInternet,HasBackupPower,HazardStorageAllowed,CoverageRegions,MaxDeliveryDistanceKM,OverflowWarningThreshold")] Warehouse warehouse)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (id != warehouse.Id || warehouse.SupplierId != supplier.Id) return NotFound();

            ModelState.Remove("Supplier");
            ModelState.Remove("Inventories");
            ModelState.Remove("Employees");
            ModelState.Remove("Assignments");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(warehouse);
                    await _context.SaveChangesAsync();

                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Warehouse", 
                        warehouse.Id.ToString(), 
                        "Update", 
                        notes: $"Warehouse '{warehouse.Name}' updated properties",
                        performedByUserId: userId
                    );

                    TempData["SuccessMessage"] = "Warehouse properties updated.";
                    return RedirectToAction(nameof(Warehouses));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WarehouseExists(warehouse.Id, supplier.Id)) return NotFound();
                    else throw;
                }
            }
            return View(warehouse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id && w.SupplierId == supplier.Id);
            
            if (warehouse != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // SOFT DELETE - preserve history for logistics audit
                    warehouse.IsActive = false;
                    warehouse.IsDeleted = true;
                    warehouse.DeletedAt = DateTime.Now;
                    warehouse.Status = SCM_System.Models.Enums.WarehouseStatus.Inactive;
                    warehouse.UpdatedAt = DateTime.Now;

                    // 1. End all active assignments for this warehouse
                    var activeAssigns = await _context.WarehouseAssignments
                        .Where(wa => wa.WarehouseId == id && wa.IsActive)
                        .ToListAsync();
                    foreach (var wa in activeAssigns)
                    {
                        wa.IsActive = false;
                        wa.EndDate = DateTime.Now;
                    }

                    // 2. Clear primary manager link if any
                    warehouse.PrimaryManagerId = null;

                    // 3. Unlink any vehicles assigned to this warehouse
                    var attachedVehicles = await _context.Vehicles.Where(v => v.WarehouseId == id).ToListAsync();
                    foreach(var v in attachedVehicles) { v.WarehouseId = null; }

                    _context.Update(warehouse);
                    await _context.SaveChangesAsync();

                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Warehouse", 
                        warehouse.Id.ToString(), 
                        "Delete", 
                        notes: $"Soft delete performed for warehouse '{warehouse.Name}'",
                        performedByUserId: userId
                    );

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Warehouse deactivated (Soft-Deleted) successfully.";
                }
                catch(Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Could not deactivate warehouse: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Warehouses));
        }

        
        private bool WarehouseExists(int id, int supplierId)
        {
            return _context.Warehouses.Any(e => e.Id == id && e.SupplierId == supplierId);
        }
    }
}
