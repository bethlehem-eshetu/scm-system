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
    public class WorkforceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SCM_System.Services.ISupplierService _supplierService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IWebHostEnvironment _env;

        public WorkforceController(ApplicationDbContext context, SCM_System.Services.ISupplierService supplierService, IAuditLogService auditLogService, INotificationService notificationService, IWebHostEnvironment env)
        {
            _context = context;
            _supplierService = supplierService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _env = env;
        }

        private async Task<bool> IsDriverCompliantAsync(int employeeId)
        {
            var employee = await _context.SupplierEmployees
                .Include(e => e.DriverProfile)
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null || employee.IsDeleted || employee.Status != SCM_System.Models.Enums.EmployeeStatus.Active) return false;

            // Check License Expiry
            if (employee.DriverProfile != null && employee.DriverProfile.LicenseExpiryDate.HasValue && employee.DriverProfile.LicenseExpiryDate.Value < DateTime.Now) return false;

            // Check for missing mandatory documents (e.g. License proof)
            var hasLicenseDoc = employee.Documents.Any(d => d.DocumentType == "License" && d.IsActive);
            if (!hasLicenseDoc) return false;

            return true;
        }

        private async Task<bool> IsVehicleCompliantAsync(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null || vehicle.IsDeleted || !vehicle.IsActive) return false;

            // Block if service is overdue
            if (vehicle.NextServiceDueDate.HasValue && vehicle.NextServiceDueDate.Value < DateTime.Now) return false;
            
            // Block if insurance is expired
            if (vehicle.InsuranceExpiryDate.HasValue && vehicle.InsuranceExpiryDate.Value < DateTime.Now) return false;

            return true;
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

        // GET: /Supplier/Employees        [HttpGet]
        public async Task<IActionResult> GetDriverDetails(int employeeId)
        {
            var driver = await _context.SupplierEmployees
                .Include(e => e.VehicleAssignments.Where(va => va.IsActive))
                    .ThenInclude(va => va.Vehicle)
                .Include(e => e.DriverProfile)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (driver == null) return NotFound();

            var activeVehicle = driver.VehicleAssignments.FirstOrDefault()?.Vehicle;

            return Json(new
            {
                vehicleId = activeVehicle?.Id,
                licensePlate = activeVehicle?.LicensePlate,
                vehicleType = activeVehicle?.VehicleType.ToString(),
                maxLoadCapacity = activeVehicle?.MaxLoadCapacity,
                licenseNumber = driver.DriverProfile?.DrivingLicenseNumber,
                licenseExpiry = driver.DriverProfile?.LicenseExpiryDate?.ToString("yyyy-MM-dd")
            });
        }

        public async Task<IActionResult> Employees()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var accessibleWarehouseIds = await GetAccessibleWarehouseIdsAsync(userId.Value, supplier.Id);

            var employees = await _context.SupplierEmployees
                .Include(se => se.User)
                .Include(se => se.DriverProfile)
                .Include(se => se.WarehouseAssignments)
                    .ThenInclude(wa => wa.Warehouse)
                .Include(se => se.VehicleAssignments)
                    .ThenInclude(va => va.Vehicle)
                .Where(se => se.SupplierId == supplier.Id && !se.IsDeleted)
                .Where(se => se.EmployeeRole == "DeliveryAgent" || se.WarehouseAssignments.Any(wa => accessibleWarehouseIds.Contains(wa.WarehouseId)) || accessibleWarehouseIds.Count == 0) // Count==0 handles edge case or owner with no hubs yet
                .OrderByDescending(se => se.CreatedAt)
                .ToListAsync();

            return View(employees);
        }

        // GET: /Supplier/AddEmployee
        public async Task<IActionResult> AddEmployee()
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

            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == supplier.Id && v.IsActive && v.Status == SCM_System.Models.Enums.VehicleStatus.Available)
                .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = $"{v.LicensePlate} - {v.VehicleType}" })
                .ToListAsync();

            return View(new EmployeeViewModel());
        }

        // POST: /Supplier/AddEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(EmployeeViewModel model, IFormFile? photoDoc, IFormFile? contractDoc, IFormFile? idDoc)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required for new employees");
            }

            if (model.Role == "WarehouseManager")
            {
                if (!model.WarehouseId.HasValue) 
                {
                    ModelState.AddModelError("WarehouseId", "Warehouse Manager must select a warehouse.");
                }
                else
                {
                    var existingWarehouseManager = await _context.WarehouseAssignments.AnyAsync(w => w.WarehouseId == model.WarehouseId && w.IsActive);
                    if (existingWarehouseManager)
                    {
                        ModelState.AddModelError("WarehouseId", "This warehouse already has an active manager.");
                    }
                }
                model.VehicleId = null;
            }
            else if (model.Role == "DeliveryAgent")
            {
                if (!model.VehicleId.HasValue) 
                {
                    ModelState.AddModelError("VehicleId", "Delivery Agent must select a vehicle.");
                }
                else
                {
                    var existingVehicleDriver = await _context.VehicleAssignments.AnyAsync(v => v.VehicleId == model.VehicleId && v.IsActive);
                    if (existingVehicleDriver)
                    {
                        ModelState.AddModelError("VehicleId", "This vehicle already has an active driver.");
                    }

                    // Maintenance & Compliance Guard
                    if (!await IsVehicleCompliantAsync(model.VehicleId.Value))
                    {
                        ModelState.AddModelError("VehicleId", "This vehicle cannot be assigned: Service is overdue or Insurance has expired.");
                    }
                }
                model.WarehouseId = null;
                
                // Strict Document Validation for Delivery Agent
                if (idDoc == null) ModelState.AddModelError("", "A valid Driving License or National ID document is required for Delivery Agents.");
                if (contractDoc == null) ModelState.AddModelError("", "Employment Contract is required for Delivery Agents.");
            }
            
            // Check file extensions and sizes limit (e.g. 5MB)
            var maxFileSize = 5 * 1024 * 1024;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            foreach(var file in new[] {photoDoc, contractDoc, idDoc, Request.Form.Files.GetFile("medicalDoc")})
            {
                if (file != null && file.Length > 0)
                {
                    if (file.Length > maxFileSize) ModelState.AddModelError("", $"File {file.FileName} exceeds the max size of 5MB.");
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext)) ModelState.AddModelError("", $"File {file.FileName} extension {ext} not allowed.");
                }
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = new User
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        PasswordHash = HashPassword(model.Password),
                        PhoneNumber = model.PhoneNumber,
                        Role = model.Role,
                        AccountStatus = "Active",
                        IsApproved = true,
                        ApprovalStatus = "Approved",
                        IsFaydaVerified = true,
                        FaydaStatus = "Verified",
                        CreatedAt = DateTime.Now,
                        EmailVerified = true,
                        PhoneVerified = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var employeeRole = model.Role == "WarehouseManager" ? "WarehouseManager" : "DeliveryAgent";
                    
                    // Generate ERP-style Employee ID
                    var employeeCount = await _context.SupplierEmployees.CountAsync(e => e.SupplierId == supplier.Id);
                    var employeeDisplayId = $"EMP-{supplier.Id.ToString("D2")}-{(employeeCount + 1).ToString("D3")}";

                    var employee = new SupplierEmployee
                    {
                        UserId = user.Id,
                        SupplierId = supplier.Id,
                        EmployeeRole = employeeRole,
                        EmployeeDisplayId = employeeDisplayId,
                        Department = model.Department,
                        EmploymentType = model.EmploymentType,
                        Shift = model.Shift,
                        JoinDate = model.JoinDate ?? DateTime.Now,
                        Status = model.Status,
                        ForcePasswordChange = model.ForcePasswordChange,
                        Gender = model.Gender,
                        DateOfBirth = model.DateOfBirth,
                        NationalID = model.NationalID,
                        Phone = model.PhoneNumber,
                        Email = model.Email,
                        CreatedAt = DateTime.Now,
                        ProfilePhotoPath = await SaveFileAsync(photoDoc, "employees"),
                        ContractDocumentUrl = await SaveFileAsync(contractDoc, "employees"),
                        IdDocumentUrl = await SaveFileAsync(idDoc, "employees"),
                        MonthlySalary = model.MonthlySalary,
                        EmergencyContactName = model.EmergencyContactName,
                        EmergencyContactPhone = model.EmergencyContactPhone,
                        IsActive = true
                    };

                    _context.SupplierEmployees.Add(employee);
                    await _context.SaveChangesAsync();

                    // Save to EmployeeDocuments Table for compliance history
                    if (photoDoc != null) _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = employee.Id, DocumentType = "ProfilePhoto", DocumentName = photoDoc.FileName ?? "Profile Photo", DocumentUrl = employee.ProfilePhotoPath });
                    if (contractDoc != null) _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = employee.Id, DocumentType = "Contract", DocumentName = contractDoc.FileName ?? "Employment Contract", DocumentUrl = employee.ContractDocumentUrl });
                    if (idDoc != null) _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = employee.Id, DocumentType = "FaydaID", DocumentName = idDoc.FileName ?? "Fayda National ID", DocumentUrl = employee.IdDocumentUrl });
                    
                    IFormFile? medDoc = Request.Form.Files.GetFile("medicalDoc");
                    var medUrl = await SaveFileAsync(medDoc, "employees");
                    if (!string.IsNullOrEmpty(medUrl)) _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = employee.Id, DocumentType = "Medical", DocumentName = medDoc?.FileName ?? "Medical Certificate", DocumentUrl = medUrl });

                    if (model.Role == "WarehouseManager")
                    {
                        employee.WarehouseProfile = new WarehouseProfile
                        {
                            CanApproveTransfers = model.CanApproveTransfers,
                            CanManageInventory = model.CanManageInventory,
                            CanViewReports = model.CanViewReports
                        };
                        
                        if (model.WarehouseId.HasValue)
                        {
                            employee.WarehouseId = model.WarehouseId;
                            employee.WarehouseAssignments = new List<WarehouseAssignment> 
                            { 
                                new WarehouseAssignment { WarehouseId = model.WarehouseId.Value, StartDate = DateTime.Now, IsPrimary = true } 
                            };
                        }
                    }
                    else if (model.Role == "DeliveryAgent")
                    {
                        employee.DriverProfile = new DriverProfile { 
                            DrivingLicenseNumber = model.DrivingLicenseNumber,
                            LicenseType = model.LicenseType?.ToString(),
                            LicenseIssueDate = model.LicenseIssueDate,
                            LicenseExpiryDate = model.LicenseExpiryDate,
                            MedicalFitnessExpiry = model.MedicalFitnessExpiryDate,
                            DeliveryRegion = model.DeliveryRegion,
                            CityCoverage = model.CityCoverage,
                            CoverageArea = model.CoverageArea
                        };

                        if (model.VehicleId.HasValue)
                        {
                            employee.VehicleId = model.VehicleId;
                            employee.VehicleAssignments = new List<VehicleAssignment> 
                            { 
                                new VehicleAssignment { VehicleId = model.VehicleId.Value, StartDate = DateTime.Now, IsPrimary = true } 
                            };
                        }
                    }

                    // Employee entity is already tracked by EF, so just save changes
                    await _context.SaveChangesAsync();

                    // Two-Way Save: update the primary manager/driver on the asset table
                    if (model.Role == "WarehouseManager" && model.WarehouseId.HasValue)
                    {
                        // 1. Deactivate other active managers for this warehouse
                        var otherAssignments = await _context.WarehouseAssignments
                            .Where(wa => wa.WarehouseId == model.WarehouseId.Value && wa.IsActive && wa.SupplierEmployeeId != employee.Id)
                            .ToListAsync();
                        foreach(var oa in otherAssignments) { oa.IsActive = false; oa.EndDate = DateTime.Now; }

                        // 2. Update Warehouse primary manager
                        var wh = await _context.Warehouses.FindAsync(model.WarehouseId.Value);
                        if (wh != null)
                        {
                            wh.PrimaryManagerId = employee.Id;
                            _context.Update(wh);
                        }
                    }
                    else if (model.Role == "DeliveryAgent" && model.VehicleId.HasValue)
                    {
                        // 1. Deactivate other active drivers for this vehicle
                        var otherAssignments = await _context.VehicleAssignments
                            .Where(va => va.VehicleId == model.VehicleId.Value && va.IsActive && va.SupplierEmployeeId != employee.Id)
                            .ToListAsync();
                        foreach(var oa in otherAssignments) { oa.IsActive = false; oa.EndDate = DateTime.Now; }

                        // 2. Update Vehicle primary driver
                        var vh = await _context.Vehicles.FindAsync(model.VehicleId.Value);
                        if (vh != null)
                        {
                            vh.PrimaryDriverId = employee.Id;
                            _context.Update(vh);
                        }
                    }

                    await _context.SaveChangesAsync();

                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Employee", 
                        employee.Id.ToString(), 
                        "Create", 
                        notes: $"New employee {employee.User.FullName} ({employee.EmployeeDisplayId}) registered as {employee.EmployeeRole}",
                        performedByUserId: userId
                    );

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Employee added successfully.";
                    return RedirectToAction(nameof(Employees));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Failed to add employee: " + ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : "");
                }
            }
            return View(model);
        }

        // GET: /Supplier/EditEmployee/5
        public async Task<IActionResult> EditEmployee(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var employee = await _context.SupplierEmployees
                .Include(se => se.User)
                .Include(se => se.WarehouseProfile)
                .Include(se => se.DriverProfile)
                .Include(se => se.WarehouseAssignments)
                    .ThenInclude(wa => wa.Warehouse)
                .Include(se => se.VehicleAssignments)
                    .ThenInclude(va => va.Vehicle)
                .FirstOrDefaultAsync(se => se.Id == id && se.SupplierId == supplier.Id);

            if (employee == null) return NotFound();

            var model = new EmployeeViewModel
            {
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                PhoneNumber = employee.User.PhoneNumber,
                Role = employee.User.Role,
                EmployeeDisplayId = employee.EmployeeDisplayId,
                Department = employee.Department,
                Gender = employee.Gender,
                DateOfBirth = employee.DateOfBirth,
                NationalID = employee.NationalID,
                JoinDate = employee.JoinDate,
                EmploymentType = employee.EmploymentType,
                Shift = employee.Shift,
                Status = employee.Status,
                ForcePasswordChange = employee.ForcePasswordChange,
                
                // Mapping Assignment IDs
                WarehouseId = employee.WarehouseAssignments?.FirstOrDefault(w => w.IsActive)?.WarehouseId,
                VehicleId = employee.VehicleAssignments?.FirstOrDefault(v => v.IsActive)?.VehicleId,

                // Profile-specific data
                CanApproveTransfers = employee.WarehouseProfile?.CanApproveTransfers ?? false,
                CanManageInventory = employee.WarehouseProfile?.CanManageInventory ?? false,
                CanViewReports = employee.WarehouseProfile?.CanViewReports ?? false,

                DrivingLicenseNumber = employee.DriverProfile?.DrivingLicenseNumber,
                LicenseIssueDate = employee.DriverProfile?.LicenseIssueDate,
                LicenseExpiryDate = employee.DriverProfile?.LicenseExpiryDate,
                MedicalFitnessExpiryDate = employee.DriverProfile?.MedicalFitnessExpiry,
                DeliveryRegion = employee.DriverProfile?.DeliveryRegion,
                CityCoverage = employee.DriverProfile?.CityCoverage,
                CoverageArea = employee.DriverProfile?.CoverageArea,
                LicenseType = Enum.TryParse<SCM_System.Models.Enums.LicenseType>(employee.DriverProfile?.LicenseType, out var lt) ? lt : null,

                // Document Mappings
                ProfilePhotoPath = employee.ProfilePhotoPath,
                IdDocumentUrl = employee.IdDocumentUrl,
                ContractDocumentUrl = employee.ContractDocumentUrl,

                // ERP Fields
                MonthlySalary = employee.MonthlySalary,
                EmergencyContactName = employee.EmergencyContactName,
                EmergencyContactPhone = employee.EmergencyContactPhone
            };

            // Enhanced Dropdown logic: Include current assignments even if "InUse" or "Locked"
            var currentWarehouseId = model.WarehouseId;
            var currentVehicleId = model.VehicleId;

            var warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && (w.IsActive || w.Id == currentWarehouseId))
                .Select(w => new SelectListItem 
                { 
                    Value = w.Id.ToString(), 
                    Text = $"{w.Name} ({w.City}) - {(w.IsActive ? "Active" : "Closed")}",
                    Selected = w.Id == currentWarehouseId
                })
                .ToListAsync();
            warehouses.Insert(0, new SelectListItem { Value = "", Text = "-- Unassigned / Floating --" });
            ViewBag.Warehouses = warehouses;

            var vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == supplier.Id && (v.Status == SCM_System.Models.Enums.VehicleStatus.Available || v.Id == currentVehicleId))
                .Select(v => new SelectListItem 
                { 
                    Value = v.Id.ToString(), 
                    Text = $"{v.LicensePlate} ({v.VehicleType}) - {v.Status}",
                    Selected = v.Id == currentVehicleId
                })
                .ToListAsync();
            vehicles.Insert(0, new SelectListItem { Value = "", Text = "-- No Vehicle Assigned --" });
            ViewBag.Vehicles = vehicles;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, EmployeeViewModel model, IFormFile? photoDoc, IFormFile? contractDoc, IFormFile? idDoc, IFormFile? medicalDoc)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var employee = await _context.SupplierEmployees
                .Include(se => se.User)
                .Include(se => se.DriverProfile)
                .Include(se => se.WarehouseProfile)
                .Include(se => se.VehicleAssignments)
                .Include(se => se.WarehouseAssignments)
                .FirstOrDefaultAsync(se => se.Id == id && se.SupplierId == supplier.Id);

            if (employee == null) return NotFound();

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Check email uniqueness if changed
                    if (employee.User.Email != model.Email && await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Email already registered by another user.");
                        goto RePopulateAndReturn;
                    }

                    // Update Basics
                    employee.User.FullName = model.FullName;
                    employee.User.Email = model.Email;
                    employee.User.PhoneNumber = model.PhoneNumber;
                    
                    employee.Email = model.Email;
                    employee.Phone = model.PhoneNumber;
                    employee.Department = model.Department;
                    employee.Gender = model.Gender;
                    employee.DateOfBirth = model.DateOfBirth;
                    employee.NationalID = model.NationalID;
                    employee.JoinDate = model.JoinDate;
                    employee.EmploymentType = model.EmploymentType;
                    employee.Shift = model.Shift;
                    employee.Status = model.Status;
                    employee.ForcePasswordChange = model.ForcePasswordChange;
                    employee.MonthlySalary = model.MonthlySalary;
                    employee.EmergencyContactName = model.EmergencyContactName;
                    employee.EmergencyContactPhone = model.EmergencyContactPhone;

                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        employee.User.PasswordHash = HashPassword(model.Password);
                    }

                    // Handle Document Uploads
                    if (photoDoc != null) employee.ProfilePhotoPath = await SaveFileAsync(photoDoc, "employees");
                    if (idDoc != null) employee.IdDocumentUrl = await SaveFileAsync(idDoc, "employees");
                    if (contractDoc != null) employee.ContractDocumentUrl = await SaveFileAsync(contractDoc, "employees");
                    
                    if (photoDoc != null) _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = id, DocumentType = "ProfilePhoto", DocumentName = photoDoc.FileName ?? "Profile Photo", DocumentUrl = employee.ProfilePhotoPath });
                    if (idDoc != null) _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = id, DocumentType = "FaydaID", DocumentName = idDoc.FileName ?? "Fayda National ID", DocumentUrl = employee.IdDocumentUrl });
                    if (contractDoc != null) _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = id, DocumentType = "Contract", DocumentName = contractDoc.FileName ?? "Employment Contract", DocumentUrl = employee.ContractDocumentUrl });

                    if (medicalDoc != null)
                    {
                        var medUrl = await SaveFileAsync(medicalDoc, "employees");
                        if (!string.IsNullOrEmpty(medUrl))
                        {
                            _context.EmployeeDocuments.Add(new EmployeeDocument { SupplierEmployeeId = id, DocumentType = "Medical", DocumentName = medicalDoc.FileName ?? "Medical Certificate", DocumentUrl = medUrl });
                        }
                    }

                    // ========== ASSIGNMENT SYNCHRONIZATION ==========
                    if (model.Role == "WarehouseManager")
                    {
                        if (employee.WarehouseProfile == null) employee.WarehouseProfile = new WarehouseProfile();
                        employee.WarehouseProfile.CanApproveTransfers = model.CanApproveTransfers;
                        employee.WarehouseProfile.CanManageInventory = model.CanManageInventory;
                        employee.WarehouseProfile.CanViewReports = model.CanViewReports;

                        if (model.WarehouseId != employee.WarehouseId)
                        {
                            // 1. End old assignments
                            var activeAssigns = employee.WarehouseAssignments.Where(wa => wa.IsActive).ToList();
                            foreach(var wa in activeAssigns) { wa.IsActive = false; wa.EndDate = DateTime.Now; }

                            if (model.WarehouseId.HasValue)
                            {
                                // 2. Deactivate other active managers for the target warehouse
                                var otherManagers = await _context.WarehouseAssignments
                                    .Where(wa => wa.WarehouseId == model.WarehouseId.Value && wa.IsActive && wa.SupplierEmployeeId != id)
                                    .ToListAsync();
                                foreach(var om in otherManagers) { om.IsActive = false; om.EndDate = DateTime.Now; }

                                // 3. Create new primary assignment
                                _context.WarehouseAssignments.Add(new WarehouseAssignment 
                                { 
                                    WarehouseId = model.WarehouseId.Value, 
                                    SupplierEmployeeId = id, 
                                    StartDate = DateTime.Now, 
                                    IsPrimary = true, 
                                    IsActive = true 
                                });
                                
                                // 4. Update warehouse table
                                var wh = await _context.Warehouses.FindAsync(model.WarehouseId.Value);
                                if (wh != null)
                                {
                                    wh.PrimaryManagerId = id;
                                    _context.Update(wh);
                                }
                            }
                            employee.WarehouseId = model.WarehouseId;
                        }
                    }
                    else if (model.Role == "DeliveryAgent")
                    {
                        if (employee.DriverProfile == null) employee.DriverProfile = new DriverProfile();
                        employee.DriverProfile.DrivingLicenseNumber = model.DrivingLicenseNumber;
                        employee.DriverProfile.LicenseType = model.LicenseType?.ToString();
                        employee.DriverProfile.LicenseIssueDate = model.LicenseIssueDate;
                        employee.DriverProfile.LicenseExpiryDate = model.LicenseExpiryDate;
                        employee.DriverProfile.MedicalFitnessExpiry = model.MedicalFitnessExpiryDate;
                        employee.DriverProfile.DeliveryRegion = model.DeliveryRegion;
                        employee.DriverProfile.CityCoverage = model.CityCoverage;
                        
                        if (model.VehicleId != employee.VehicleId)
                        {
                            // 1. End old assignments
                            var activeAssigns = employee.VehicleAssignments.Where(va => va.IsActive).ToList();
                            foreach(var va in activeAssigns) 
                            { 
                                va.IsActive = false; 
                                va.EndDate = DateTime.Now; 
                                
                                // Clear old vehicle driver link
                                var oldVh = await _context.Vehicles.FindAsync(va.VehicleId);
                                if (oldVh != null && oldVh.PrimaryDriverId == id)
                                {
                                    oldVh.PrimaryDriverId = null;
                                    oldVh.Status = SCM_System.Models.Enums.VehicleStatus.Available;
                                    _context.Update(oldVh);
                                }
                            }

                            if (model.VehicleId.HasValue) 
                            {
                                // Maintenance & Compliance Guard
                                if (!await IsVehicleCompliantAsync(model.VehicleId.Value))
                                {
                                    ModelState.AddModelError("VehicleId", "This vehicle cannot be assigned: Service is overdue or Insurance has expired.");
                                    goto RePopulateAndReturn;
                                }

                                // 2. Deactivate other active drivers for the target vehicle
                                var otherDrivers = await _context.VehicleAssignments
                                    .Where(va => va.VehicleId == model.VehicleId.Value && va.IsActive && va.SupplierEmployeeId != id)
                                    .ToListAsync();
                                foreach(var od in otherDrivers) { od.IsActive = false; od.EndDate = DateTime.Now; }

                                // 3. Create new primary assignment
                                _context.VehicleAssignments.Add(new VehicleAssignment 
                                { 
                                    VehicleId = model.VehicleId.Value, 
                                    SupplierEmployeeId = id, 
                                    StartDate = DateTime.Now, 
                                    IsPrimary = true, 
                                    IsActive = true 
                                });
                                
                                // 4. Update vehicle table
                                var vh = await _context.Vehicles.FindAsync(model.VehicleId.Value);
                                if (vh != null)
                                {
                                    vh.PrimaryDriverId = id;
                                    vh.Status = SCM_System.Models.Enums.VehicleStatus.InUse;
                                    _context.Update(vh);
                                }
                            }
                            employee.VehicleId = model.VehicleId;
                        }
                    }

                    await _context.SaveChangesAsync();
                    
                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Employee", 
                        employee.Id.ToString(), 
                        "Update", 
                        notes: $"Profile updated for {employee.User.FullName} ({employee.EmployeeDisplayId})",
                        performedByUserId: userId
                    );

                    // Notification if Warehouse/Role changed
                    if (model.WarehouseId.HasValue)
                    {
                        await _notificationService.SendNotificationAsync(
                            employee.UserId,
                            "Hub Assignment Updated",
                            $"You have been assigned to warehouse hub: { (await _context.Warehouses.FindAsync(model.WarehouseId))?.Name }",
                            "Info",
                            "/Warehouse/Dashboard"
                        );
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Employee profile updated successfully.";
                    return RedirectToAction(nameof(Employees));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Update failed: " + ex.Message);
                }
            }

        RePopulateAndReturn:
            // Re-populate dropdowns if validation fails
            ViewBag.Warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id && (w.IsActive || w.Id == model.WarehouseId))
                .Select(w => new SelectListItem { Value = w.Id.ToString(), Text = w.Name })
                .ToListAsync();
            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == supplier.Id && (v.Status == SCM_System.Models.Enums.VehicleStatus.Available || v.Id == model.VehicleId))
                .Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.LicensePlate })
                .ToListAsync();

            return View(model);
        }

        // POST: /Supplier/DeleteEmployee/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var employee = await _context.SupplierEmployees
                .Include(se => se.User)
                .FirstOrDefaultAsync(se => se.Id == id && se.SupplierId == supplier.Id);

            if (employee != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // SOFT DELETE - preserve history for logistics audit
                    employee.IsActive = false;
                    employee.IsDeleted = true;
                    employee.DeletedAt = DateTime.Now;
                    employee.Status = SCM_System.Models.Enums.EmployeeStatus.Inactive;
                    employee.User.AccountStatus = "Inactive";
                    employee.UpdatedAt = DateTime.Now;

                    // 1. End all active warehouse assignments
                    var activeWh = await _context.WarehouseAssignments
                        .Where(wa => wa.SupplierEmployeeId == id && wa.IsActive)
                        .ToListAsync();
                    foreach (var wa in activeWh)
                    {
                        wa.IsActive = false;
                        wa.EndDate = DateTime.Now;
                        // Clear primary manager reference
                        var wh = await _context.Warehouses.FindAsync(wa.WarehouseId);
                        if (wh != null && wh.PrimaryManagerId == id) { wh.PrimaryManagerId = null; _context.Update(wh); }
                    }

                    // 2. End all active vehicle assignments
                    var activeVa = await _context.VehicleAssignments
                        .Where(va => va.SupplierEmployeeId == id && va.IsActive)
                        .ToListAsync();
                    foreach (var va in activeVa)
                    {
                        va.IsActive = false;
                        va.EndDate = DateTime.Now;
                        // Clear primary driver reference and free up vehicle
                        var vh = await _context.Vehicles.FindAsync(va.VehicleId);
                        if (vh != null && vh.PrimaryDriverId == id) 
                        { 
                            vh.PrimaryDriverId = null; 
                            vh.Status = SCM_System.Models.Enums.VehicleStatus.Available;
                            _context.Update(vh); 
                        }
                    }

                    await _context.SaveChangesAsync();

                    // AUDIT LOG
                    await _auditLogService.LogActionAsync(
                        "Employee", 
                        employee.Id.ToString(), 
                        "Delete", 
                        notes: $"Soft delete performed for employee {employee.User.FullName}",
                        performedByUserId: userId
                    );

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Employee deactivated successfully (Soft Delete).";
                }
                catch(Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Could not deactivate employee: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Employees));
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
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
