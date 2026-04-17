using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace SCM_System.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SCM_System.Services.ISupplierService _supplierService;

        public SupplierController(ApplicationDbContext context, SCM_System.Services.ISupplierService supplierService)
        {
            _context = context;
            _supplierService = supplierService;
        }

        
        // GET: /Supplier/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var analytics = await _supplierService.GetDashboardAnalyticsAsync(supplier.Id);

            // ========== ADD MESSAGING VIEWBAGS ==========

            // Get unread message count
            var unreadCount = await _context.Messages
                .Where(m => (m.Conversation.RetailerId == userId ||
                            m.Conversation.SupplierId == userId) &&
                            m.SenderId != userId &&
                            !m.IsRead)
                .CountAsync();

            ViewBag.UnreadMessagesCount = unreadCount;

            // Get active penalties count
            
            ViewBag.ActivePenalties = await _context.Penalties
                .CountAsync(p => p.UserId == userId && (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));

            // Get recent conversations for dashboard widget
            ViewBag.RecentConversations = await _context.Conversations
                .Include(c => c.Retailer)
                    .ThenInclude(r => r.User)
                .Include(c => c.Supplier)
                    .ThenInclude(s => s.User)
                .Where(c => c.SupplierId == userId || c.RetailerId == userId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Take(5)
                .Select(c => new
                {
                    Id = c.Id,
                    OtherUserName = c.SupplierId == userId ?
                        (c.Retailer != null ? c.Retailer.User.FullName : "Retailer") :
                        (c.Supplier != null ? c.Supplier.User.FullName : "Supplier"),
                    OtherUserRole = c.SupplierId == userId ? "Retailer" : "Supplier",
                    LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.MessageText.Length > 50 ? m.MessageText.Substring(0, 50) + "..." : m.MessageText)
                        .FirstOrDefault() ?? "No messages yet",
                    LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead)
                })
                .ToListAsync();

            // ========== END OF ADDED VIEWBAGS ==========

            return View(analytics);
        }

        // GET: /Supplier/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: /Supplier/Notifications
        public async Task<IActionResult> Notifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // POST: /Supplier/MarkNotificationRead
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // GET: /Supplier/Employees
        public async Task<IActionResult> Employees()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var employees = await _context.SupplierEmployees
                .Include(se => se.User)
                .Include(se => se.Warehouse)
                .Include(se => se.Vehicle)
                .Where(se => se.SupplierId == supplier.Id)
                .OrderByDescending(se => se.CreatedAt)
                .ToListAsync();

            return View(employees);
        }

        // GET: /Supplier/AddEmployee
        public async Task<IActionResult> AddEmployee()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            ViewBag.Warehouses = await _context.Warehouses.Where(w => w.SupplierId == supplier.Id).ToListAsync();
            ViewBag.Vehicles = await _context.Vehicles.Where(v => v.SupplierId == supplier.Id).ToListAsync();

            return View(new EmployeeViewModel());
        }

        // POST: /Supplier/AddEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(EmployeeViewModel model)
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
                if (!model.WarehouseId.HasValue) ModelState.AddModelError("WarehouseId", "Warehouse Manager must select a warehouse.");
                model.VehicleId = null;
            }
            else if (model.Role == "DeliveryAgent")
            {
                if (!model.VehicleId.HasValue) ModelState.AddModelError("VehicleId", "Delivery Agent must select a vehicle.");
                model.WarehouseId = null;
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
                        CreatedAt = DateTime.Now,
                        EmailVerified = false,
                        PhoneVerified = false
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var employeeRole = model.Role == "WarehouseManager" ? "WarehouseManager" : "DeliveryAgent";

                    var employee = new SupplierEmployee
                    {
                        UserId = user.Id,
                        SupplierId = supplier.Id,
                        EmployeeRole = employeeRole,
                        WarehouseId = model.WarehouseId,
                        VehicleId = model.VehicleId,
                        DrivingLicenseNumber = model.Role == "DeliveryAgent" ? model.DrivingLicenseNumber : null,
                        LicenseExpiryDate = model.Role == "DeliveryAgent" ? model.LicenseExpiryDate : null,
                        IsLicenseVerified = false,
                        Phone = model.PhoneNumber,
                        Email = model.Email,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    _context.SupplierEmployees.Add(employee);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Employee added successfully.";
                    return RedirectToAction(nameof(Employees));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Failed to add employee. Please try again.";
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
                .FirstOrDefaultAsync(se => se.Id == id && se.SupplierId == supplier.Id);

            if (employee == null) return NotFound();

            var model = new EmployeeViewModel
            {
                Id = employee.Id,
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                PhoneNumber = employee.User.PhoneNumber,
                Role = employee.User.Role,
                WarehouseId = employee.WarehouseId,
                VehicleId = employee.VehicleId,
                DrivingLicenseNumber = employee.DrivingLicenseNumber,
                LicenseExpiryDate = employee.LicenseExpiryDate
            };

            ViewBag.Warehouses = await _context.Warehouses.Where(w => w.SupplierId == supplier.Id).ToListAsync();
            ViewBag.Vehicles = await _context.Vehicles.Where(v => v.SupplierId == supplier.Id).ToListAsync();

            return View(model);
        }

        // POST: /Supplier/EditEmployee/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, EmployeeViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (id != model.Id) return NotFound();

            if (model.Role == "WarehouseManager")
            {
                if (!model.WarehouseId.HasValue) ModelState.AddModelError("WarehouseId", "Warehouse Manager must select a warehouse.");
                model.VehicleId = null;
            }
            else if (model.Role == "DeliveryAgent")
            {
                if (!model.VehicleId.HasValue) ModelState.AddModelError("VehicleId", "Delivery Agent must select a vehicle.");
                model.WarehouseId = null;
            }

            if (ModelState.IsValid)
            {
                var employee = await _context.SupplierEmployees
                    .Include(se => se.User)
                    .FirstOrDefaultAsync(se => se.Id == id && se.SupplierId == supplier.Id);

                if (employee == null) return NotFound();

                // Check email uniqueness if changed
                if (employee.User.Email != model.Email && await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                employee.User.FullName = model.FullName;
                employee.User.Email = model.Email;
                employee.User.PhoneNumber = model.PhoneNumber;
                employee.User.Role = model.Role;
                
                employee.Email = model.Email;
                employee.Phone = model.PhoneNumber;
                employee.EmployeeRole = model.Role == "WarehouseManager" ? "WarehouseManager" : "DeliveryAgent";
                
                if (model.Role == "DeliveryAgent")
                {
                    employee.DrivingLicenseNumber = model.DrivingLicenseNumber;
                    employee.LicenseExpiryDate = model.LicenseExpiryDate;
                    employee.WarehouseId = null;
                    employee.VehicleId = model.VehicleId;
                }
                else
                {
                    employee.DrivingLicenseNumber = null;
                    employee.LicenseExpiryDate = null;
                    employee.VehicleId = null;
                    employee.WarehouseId = model.WarehouseId;
                }

                if (!string.IsNullOrEmpty(model.Password))
                {
                    employee.User.PasswordHash = HashPassword(model.Password);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee updated successfully.";
                return RedirectToAction(nameof(Employees));
            }
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
                    var user = employee.User;
                    _context.SupplierEmployees.Remove(employee);
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Employee deleted successfully.";
                }
                catch(Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Could not delete employee. They may have related records.";
                }
            }

            return RedirectToAction(nameof(Employees));
        }

        // ================= WAREHOUSE MANAGEMENT =================

        // GET: /Supplier/Warehouses
        public async Task<IActionResult> Warehouses()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var warehouses = await _context.Warehouses
                .Where(w => w.SupplierId == supplier.Id)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(warehouses);
        }

        // GET: /Supplier/CreateWarehouse
        public async Task<IActionResult> CreateWarehouse()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            return View();
        }

        // POST: /Supplier/CreateWarehouse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWarehouse([Bind("Name,WarehouseCode,Region,City,Address,StorageType,MaxCapacity,HandlingTimeHours")] Warehouse warehouse)
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

            if (ModelState.IsValid)
            {
                warehouse.SupplierId = supplier.Id;
                warehouse.Country = "Ethiopia";
                warehouse.Status = SCM_System.Models.Enums.WarehouseStatus.Active;
                warehouse.SupportsDelivery = true;
                warehouse.CreatedAt = DateTime.Now;
                _context.Warehouses.Add(warehouse);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Warehouse added successfully.";
                return RedirectToAction(nameof(Warehouses));
            }
            return View(warehouse);
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
        public async Task<IActionResult> EditWarehouse(int id, [Bind("Id,SupplierId,Name,WarehouseCode,Country,Region,City,Address,StorageType,MaxCapacity,Status,IsDefault,SupportsDelivery,HandlingTimeHours,CreatedAt")] Warehouse warehouse)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (id != warehouse.Id || warehouse.SupplierId != supplier.Id) return NotFound();

            ModelState.Remove("Supplier");
            ModelState.Remove("Inventories");
            ModelState.Remove("Employees");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(warehouse);
                    await _context.SaveChangesAsync();
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

        // POST: /Supplier/DeleteWarehouse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var warehouse = await _context.Warehouses
                                .Include(w => w.Employees)
                                .FirstOrDefaultAsync(w => w.Id == id && w.SupplierId == supplier.Id);
            if (warehouse != null)
            {
                if (warehouse.Employees.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete: Warehouse is assigned to a Warehouse Manager.";
                    return RedirectToAction(nameof(Warehouses));
                }

                // Check active stock in Inventory
                bool hasStock = await _context.Inventories.AnyAsync(i => i.WarehouseId == id && i.QuantityOnHand > 0);
                if (hasStock)
                {
                    TempData["ErrorMessage"] = "Cannot delete: Warehouse has active stock.";
                    return RedirectToAction(nameof(Warehouses));
                }
                
                // Check if used in Orders (if orders track warehouse logic later, it would be here)

                _context.Warehouses.Remove(warehouse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Warehouse removed completely.";
            }

            return RedirectToAction(nameof(Warehouses));
        }
        
        private bool WarehouseExists(int id, int supplierId)
        {
            return _context.Warehouses.Any(e => e.Id == id && e.SupplierId == supplierId);
        }

        // ================= VEHICLE MANAGEMENT =================

        // GET: /Supplier/Vehicles
        public async Task<IActionResult> Vehicles()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var vehicles = await _context.Vehicles
                .Where(v => v.SupplierId == supplier.Id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return View(vehicles);
        }

        // GET: /Supplier/CreateVehicle
        public async Task<IActionResult> CreateVehicle()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            return View();
        }

        // POST: /Supplier/CreateVehicle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVehicle([Bind("LicensePlate,VehicleType,MaxLoadCapacity,VolumeCapacity,HasTemperatureControl,RegistrationNumber,InsuranceStatus,InsuranceExpiryDate,RoadworthinessStatus")] Vehicle vehicle)
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

            if (ModelState.IsValid)
            {
                vehicle.SupplierId = supplier.Id;
                vehicle.CreatedAt = DateTime.Now;
                vehicle.Status = SCM_System.Models.Enums.VehicleStatus.Available;
                
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Vehicle registered successfully.";
                return RedirectToAction(nameof(Vehicles));
            }
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

            return View(vehicle);
        }

        // POST: /Supplier/EditVehicle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVehicle(int id, [Bind("Id,SupplierId,LicensePlate,VehicleType,MaxLoadCapacity,VolumeCapacity,HasTemperatureControl,RegistrationNumber,InsuranceStatus,InsuranceExpiryDate,RoadworthinessStatus,LastMaintenanceDate,Status,CreatedAt")] Vehicle vehicle)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            if (id != vehicle.Id || vehicle.SupplierId != supplier.Id) return NotFound();

            ModelState.Remove("Supplier");
            ModelState.Remove("DeliveryAgents");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Vehicle details updated successfully.";
                    return RedirectToAction(nameof(Vehicles));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.Id, supplier.Id)) return NotFound();
                    else throw;
                }
            }
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
                if (vehicle.DeliveryAgents.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete: Vehicle is assigned to a Delivery Agent.";
                    return RedirectToAction(nameof(Vehicles));
                }
                
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle wiped from fleet record.";
            }

            return RedirectToAction(nameof(Vehicles));
        }

        // GET: /Supplier/Reports
        public async Task<IActionResult> Reports()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var reports = await _supplierService.GetSupplierReportsAsync(supplier.Id);
            return View(reports);
        }

        // GET: /Supplier/OrderTracking
        public async Task<IActionResult> OrderTracking()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var orders = await _supplierService.GetSupplierOrdersForTrackingAsync(supplier.Id);
            return View(orders);
        }

        // GET: /Supplier/Payments
        public async Task<IActionResult> Payments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (supplier == null) return RedirectToAction("AccessDenied", "Home");

            var commissions = await _supplierService.GetSupplierCommissionsAsync(supplier.Id);
            return View(commissions);
        }

        // POST: /Supplier/PayCommission
        [HttpPost]
        public async Task<IActionResult> PayCommission(int id)
        {
            var commission = await _supplierService.GetCommissionByIdAsync(id);
            if (commission == null) return NotFound();

            // Mock Chapa payment redirection
            // In a real scenario, we would call the Chapa API to create a checkout session
            return Redirect($"https://test.chapa.co/pay?order_id={commission.PurchaseOrderId}&amount={commission.CommissionAmount}");
        }

        private bool VehicleExists(int id, int supplierId)
        {
            return _context.Vehicles.Any(e => e.Id == id && e.SupplierId == supplierId);
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
    }
}