using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Controllers
{
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WarehouseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || role != "Warehouse")
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.SupplierEmployees
                .Include(se => se.User)
                .Include(se => se.Supplier)
                .FirstOrDefaultAsync(se => se.UserId == userId);

            if (employee == null)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Fetch statistics for the dashboard
            ViewBag.TotalWarehouseItems = await _context.Inventories
                .CountAsync(i => i.Warehouse.SupplierId == employee.SupplierId);
            
            ViewBag.LowStockCount = await _context.Inventories
                .CountAsync(i => i.Warehouse.SupplierId == employee.SupplierId && i.QuantityAvailable <= 10);

            ViewBag.PendingOrders = await _context.Orders
                .CountAsync(o => o.SupplierId == employee.SupplierId && o.OrderStatus == "Pending");

            return View(employee);
        }
    }
}
