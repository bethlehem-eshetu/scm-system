using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Controllers
{
    public class SupplierEmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierEmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetCurrentSupplierId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
            return supplier?.Id;
        }

        // GET: SupplierEmployee
        public async Task<IActionResult> Index()
        {
            var supplierId = await GetCurrentSupplierId();
            if (supplierId == null) return RedirectToAction("Login", "Account");

            var employees = await _context.SupplierEmployees
                .Where(e => e.SupplierId == supplierId)
                .ToListAsync();

            return View(employees);
        }

        // GET: SupplierEmployee/Create
        public async Task<IActionResult> Create()
        {
            var supplierId = await GetCurrentSupplierId();
            if (supplierId == null) return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: SupplierEmployee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,Phone,Role,Status")] SupplierEmployee employee)
        {
            var supplierId = await GetCurrentSupplierId();
            if (supplierId == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                employee.SupplierId = supplierId.Value;
                employee.CreatedAt = DateTime.Now;
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: SupplierEmployee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var supplierId = await GetCurrentSupplierId();
            if (supplierId == null) return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var employee = await _context.SupplierEmployees
                .FirstOrDefaultAsync(e => e.Id == id && e.SupplierId == supplierId);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: SupplierEmployee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Email,Phone,Role,Status")] SupplierEmployee employee)
        {
            var supplierId = await GetCurrentSupplierId();
            if (supplierId == null) return RedirectToAction("Login", "Account");

            if (id != employee.Id) return NotFound();

            var existingEmployee = await _context.SupplierEmployees
                .FirstOrDefaultAsync(e => e.Id == id && e.SupplierId == supplierId);
            
            if (existingEmployee == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existingEmployee.FullName = employee.FullName;
                    existingEmployee.Email = employee.Email;
                    existingEmployee.Phone = employee.Phone;
                    existingEmployee.Role = employee.Role;
                    existingEmployee.Status = employee.Status;

                    _context.Update(existingEmployee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierEmployeeExists(employee.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // POST: SupplierEmployee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplierId = await GetCurrentSupplierId();
            if (supplierId == null) return RedirectToAction("Login", "Account");

            var employee = await _context.SupplierEmployees
                .FirstOrDefaultAsync(e => e.Id == id && e.SupplierId == supplierId);
                
            if (employee != null)
            {
                _context.SupplierEmployees.Remove(employee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SupplierEmployeeExists(int id)
        {
            return _context.SupplierEmployees.Any(e => e.Id == id);
        }
    }
}
