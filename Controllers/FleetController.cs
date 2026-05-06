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
    public class FleetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SCM_System.Services.ISupplierService _supplierService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IWebHostEnvironment _env;

        public FleetController(ApplicationDbContext context, SCM_System.Services.ISupplierService supplierService, IAuditLogService auditLogService, INotificationService notificationService, IWebHostEnvironment env)
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


        // GET: /Supplier/Vehicles
        public async Task<IActionResult> Vehicles()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var accessibleWarehouseIds = await GetAccessibleWarehouseIdsAsync(userId.Value, supplier.Id);

            var vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == supplier.Id && !v.IsDeleted)
                .Where(v => v.WarehouseId == null || accessibleWarehouseIds.Contains(v.WarehouseId.Value) || accessibleWarehouseIds.Count == 0)
                .Include(v => v.Warehouse)
                .Include(v => v.PrimaryDriver)
                    .ThenInclude(se => se.User)
                .Include(v => v.Assignments)
                    .ThenInclude(a => a.SupplierEmployee)
                        .ThenInclude(se => se.User)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return View(vehicles);
        }

        // GET: /Supplier/CreateVehicle
        public async Task<IActionResult> CreateVehicle()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null || !supplier.User.IsFaydaVerified)
            {
                TempData["ErrorMessage"] = "Identity verification (Fayda) required for this action.";
                return RedirectToAction("Dashboard", "Supplier");
            }

            ViewBag.Warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && w.IsActive)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = $"{w.Name} ({w.City})" })
                .ToListAsync();

            return View();
        }

        // POST: /Supplier/CreateVehicle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVehicle([Bind(Prefix="")] Vehicle vehicle, IFormFile? registrationDoc, IFormFile? insuranceDoc)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (string.IsNullOrWhiteSpace(vehicle.LicensePlate)) 
                ModelState.AddModelError("LicensePlate", "License Plate is required");
            if (vehicle.MaxLoadCapacity <= 0) 
                ModelState.AddModelError("MaxLoadCapacity", "Max Load Capacity must be greater than 0");
            
            bool plateExists = await _context.Vehicles.AnyAsync(v => v.LicensePlate == vehicle.LicensePlate && v.SupplierId == supplier.Id);
            if (plateExists) 
                ModelState.AddModelError("LicensePlate", "This License Plate is already registered to your fleet");

            // Extensive Validation Cleanup
            string[] navProps = { "Supplier", "PrimaryDriver", "Warehouse", "Assignments", "DeliveryAgents", "VehicleAssignments", "Documents", "MaintenanceRecords", "AssetDispatches", "AssetIncidents", "GPSLogs", "DriverHistories" };
            foreach (var prop in navProps)
            {
                ModelState.Remove(prop);
                ModelState.Remove("model." + prop);
                ModelState.Remove("vehicle." + prop);
            }

            if (ModelState.IsValid)
            {
                vehicle.SupplierId = supplier.Id;
                vehicle.CreatedAt = DateTime.Now;
                vehicle.Status = SCM_System.Models.Enums.VehicleStatus.Available;
                
                vehicle.RegistrationCertificateUrl = await SaveFileAsync(registrationDoc, "vehicles");
                vehicle.InsuranceCertificateUrl = await SaveFileAsync(insuranceDoc, "vehicles");
                
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                // AUDIT LOG
                await _auditLogService.LogActionAsync(
                    "Vehicle", 
                    vehicle.Id.ToString(), 
                    "Create", 
                    notes: $"New vehicle {vehicle.LicensePlate} ({vehicle.VehicleType}) added to fleet",
                    performedByUserId: userId
                );
                
                TempData["SuccessMessage"] = "Vehicle registered successfully.";
                return RedirectToAction(nameof(Vehicles));
            }

            ViewBag.Warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && w.IsActive)
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = $"{w.Name} ({w.City})" })
                .ToListAsync();

            return View(vehicle);
        }

        // GET: /Supplier/EditVehicle/5
        public async Task<IActionResult> EditVehicle(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.SupplierId == supplier.Id);
            if (vehicle == null) return NotFound();

            ViewBag.Warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && (w.IsActive || w.Id == vehicle.WarehouseId))
                .Select(w => new SelectListItem 
                { 
                    Value = w.Id.ToString(), 
                    Text = $"{w.Name} ({w.City})",
                    Selected = w.Id == vehicle.WarehouseId
                })
                .ToListAsync();

            return View(vehicle);
        }

        // POST: /Supplier/EditVehicle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVehicle(int id, [Bind(Prefix="")] Vehicle model, IFormFile? registrationDoc, IFormFile? insuranceDoc)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.SupplierId == supplier.Id);
            if (vehicle == null) return NotFound();

            // Extensive Validation Cleanup for Navigation Properties
            string[] navProps = { "Supplier", "PrimaryDriver", "Warehouse", "Assignments", "DeliveryAgents", "VehicleAssignments", "Documents", "MaintenanceRecords", "AssetDispatches", "AssetIncidents", "GPSLogs", "DriverHistories" };
            foreach (var prop in navProps)
            {
                ModelState.Remove(prop);
                ModelState.Remove("model." + prop);
                ModelState.Remove("vehicle." + prop);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update Core Identity
                    vehicle.LicensePlate = model.LicensePlate;
                    vehicle.AssetCode = model.AssetCode;
                    vehicle.VehicleType = model.VehicleType;
                    vehicle.Brand = model.Brand;
                    vehicle.Model = model.Model;
                    vehicle.ManufactureYear = model.ManufactureYear;
                    vehicle.Color = model.Color;
                    
                    // Technical Specs
                    vehicle.MaxLoadCapacity = model.MaxLoadCapacity;
                    vehicle.InternalVolumeM3 = model.InternalVolumeM3;
                    vehicle.FuelType = model.FuelType;
                    vehicle.FuelTankCapacity = model.FuelTankCapacity;
                    vehicle.FuelEfficiency = model.FuelEfficiency;
                    vehicle.Mileage = model.Mileage;
                    vehicle.CurrentMileage = model.CurrentMileage ?? model.Mileage; // Sync if only one provided
                    
                    // Features
                    vehicle.GPSInstalled = model.GPSInstalled;
                    vehicle.TemperatureControlled = model.TemperatureControlled;
                    
                    // Operations
                    vehicle.Status = model.Status;
                    vehicle.WarehouseId = model.WarehouseId;
                    vehicle.DriverEligibilityType = model.DriverEligibilityType;
                    
                    // Maintenance & Compliance
                    vehicle.LastServiceDate = model.LastServiceDate;
                    vehicle.NextServiceDueDate = model.NextServiceDueDate;
                    vehicle.RegistrationExpiryDate = model.RegistrationExpiryDate;
                    vehicle.InsuranceExpiryDate = model.InsuranceExpiryDate;
                    
                    // Financials
                    vehicle.PurchaseDate = model.PurchaseDate;
                    vehicle.PurchaseCost = model.PurchaseCost;
                    vehicle.InsuranceProvider = model.InsuranceProvider;
                    vehicle.FuelCardNumber = model.FuelCardNumber;

                    // Document Uploads
                    if (registrationDoc != null)
                        vehicle.RegistrationCertificateUrl = await SaveFileAsync(registrationDoc, "vehicles");
                    if (insuranceDoc != null)
                        vehicle.InsuranceCertificateUrl = await SaveFileAsync(insuranceDoc, "vehicles");

                    // Audit Info
                    vehicle.UpdatedAt = DateTime.Now;
                    vehicle.UpdatedBy = userId.ToString();

                    await _context.SaveChangesAsync();

                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Vehicle", 
                        vehicle.Id.ToString(), 
                        "Update", 
                        notes: $"Vehicle {vehicle.LicensePlate} profile updated successfully",
                        performedByUserId: userId
                    );

                    TempData["SuccessMessage"] = "Vehicle profile updated successfully.";
                    return RedirectToAction(nameof(Vehicles));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Update failed: " + ex.Message);
                    TempData["ErrorMessage"] = "Database Update Error: " + ex.Message;
                }
            }
            else
            {
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage));
                TempData["ErrorMessage"] = "Validation Failed: " + errors;
            }

            // Re-populate dropdowns if validation fails
            ViewBag.Warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && (w.IsActive || w.Id == model.WarehouseId))
                .Select(w => new SelectListItem 
                { 
                    Value = w.Id.ToString(), 
                    Text = $"{w.Name} ({w.City})",
                    Selected = w.Id == model.WarehouseId
                })
                .ToListAsync();

            return View(model);
        }

        // POST: /Supplier/DeleteVehicle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var vehicle = await _context.Vehicles
                                .Include(v => v.DeliveryAgents)
                                .FirstOrDefaultAsync(v => v.Id == id && v.SupplierId == supplier.Id);
            if (vehicle != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // SOFT DELETE - preserve history for logistics audit
                    vehicle.IsActive = false;
                    vehicle.IsDeleted = true;
                    vehicle.DeletedAt = DateTime.Now;
                    vehicle.Status = SCM_System.Models.Enums.VehicleStatus.Inactive;
                    vehicle.UpdatedAt = DateTime.Now;

                    // 1. End all active assignments for this vehicle
                    var activeAssigns = await _context.VehicleAssignments
                        .Where(va => va.VehicleId == id && va.IsActive)
                        .ToListAsync();
                    foreach (var va in activeAssigns)
                    {
                        va.IsActive = false;
                        va.EndDate = DateTime.Now;
                    }

                    // 2. Clear primary driver link if any
                    vehicle.PrimaryDriverId = null;

                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();

                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Vehicle", 
                        vehicle.Id.ToString(), 
                        "Delete", 
                        notes: $"Soft delete performed for vehicle {vehicle.LicensePlate}",
                        performedByUserId: userId
                    );

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Vehicle retired (Soft-Deleted) successfully.";
                }
                catch(Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Could not deactivate vehicle: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Vehicles));
        }
        private bool VehicleExists(int id, int supplierId)
        {
            return _context.Vehicles.Any(e => e.Id == id && e.SupplierId == supplierId);
        }
        private async Task<string?> SaveFileAsync(IFormFile? file, string subfolder)
        {
            if (file == null || file.Length == 0) return null;
            
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) return null;

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", subfolder);
            
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subfolder}/{fileName}";
        }
    }
}
