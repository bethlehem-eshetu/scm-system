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
        public async Task<IActionResult> CreateVehicle([Bind("LicensePlate,AssetCode,VehicleType,Brand,Model,ManufactureYear,Color,MaxLoadCapacity,InternalVolumeM3,TemperatureControlled,FuelType,FuelTankCapacity,GPSInstalled,Mileage,CurrentMileage,FuelEfficiency,LastServiceDate,NextServiceDueDate,TireChangeDue,InsuranceExpiryDate,RegistrationExpiryDate,WarehouseId,PurchaseDate,PurchaseCost,InsuranceProvider,FuelCardNumber,DriverEligibilityType")] Vehicle vehicle, IFormFile? registrationDoc, IFormFile? insuranceDoc)
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

            ModelState.Remove("Supplier");
            ModelState.Remove("DeliveryAgents");
            ModelState.Remove("VehicleAssignments");
            ModelState.Remove("Assignments");

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
        public async Task<IActionResult> EditVehicle(int id, [Bind("Id,SupplierId,LicensePlate,AssetCode,VehicleType,Brand,Model,ManufactureYear,Color,MaxLoadCapacity,InternalVolumeM3,TemperatureControlled,Status,CreatedAt,FuelType,FuelTankCapacity,GPSInstalled,LastServiceDate,NextServiceDueDate,Mileage,CurrentMileage,FuelEfficiency,InsuranceExpiryDate,RegistrationExpiryDate,WarehouseId,PurchaseDate,PurchaseCost,InsuranceProvider,FuelCardNumber,DriverEligibilityType")] Vehicle vehicle, IFormFile? registrationDoc, IFormFile? insuranceDoc)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (id != vehicle.Id || vehicle.SupplierId != supplier.Id) return NotFound();

            ModelState.Remove("Supplier");
            ModelState.Remove("DeliveryAgents");
            ModelState.Remove("VehicleAssignments");
            ModelState.Remove("Assignments");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingVehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                    if (existingVehicle != null)
                    {
                        if (registrationDoc != null)
                            vehicle.RegistrationCertificateUrl = await SaveFileAsync(registrationDoc, "vehicles");
                        else
                            vehicle.RegistrationCertificateUrl = existingVehicle.RegistrationCertificateUrl;

                        if (insuranceDoc != null)
                            vehicle.InsuranceCertificateUrl = await SaveFileAsync(insuranceDoc, "vehicles");
                        else
                            vehicle.InsuranceCertificateUrl = existingVehicle.InsuranceCertificateUrl;
                            
                        vehicle.VehiclePhotosUrls = existingVehicle.VehiclePhotosUrls;
                    }

                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();

                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Vehicle", 
                        vehicle.Id.ToString(), 
                        "Update", 
                        notes: $"Vehicle {vehicle.LicensePlate} profile updated",
                        performedByUserId: userId
                    );

                    TempData["SuccessMessage"] = "Vehicle details updated successfully.";
                    return RedirectToAction(nameof(Vehicles));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.Id, supplier.Id)) return NotFound();
                    else throw;
                }
            }

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
