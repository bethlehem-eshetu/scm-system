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

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Supplier/Dashboard
        public async Task<IActionResult> Dashboard()
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
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.TotalProducts = await _context.Products.CountAsync(p => p.SupplierId == supplier.Id);
            ViewBag.TotalOrders = await _context.Orders.CountAsync(o => o.SupplierId == supplier.Id);
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.SupplierId == supplier.Id && o.OrderStatus == "Processing");
            ViewBag.TotalTenders = await _context.TenderBids.CountAsync(tb => tb.SupplierId == supplier.Id);

            return View(supplier);
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

                    var employeeRole = model.Role == "Warehouse" ? "warehouse_manager" : "delivery_person";

                    var employee = new SupplierEmployee
                    {
                        UserId = user.Id,
                        SupplierId = supplier.Id,
                        EmployeeRole = employeeRole,
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
                Role = employee.User.Role
            };

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
                employee.EmployeeRole = model.Role == "Warehouse" ? "warehouse_manager" : "delivery_person";

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