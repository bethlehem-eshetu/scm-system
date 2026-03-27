using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeliveryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || role != "Delivery")
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
            ViewBag.AssignedDeliveries = await _context.Deliveries
                .CountAsync(d => d.DeliveryEmployeeId == employee.Id && d.DeliveryStatus != "Delivered");

            ViewBag.CompletedDeliveries = await _context.Deliveries
                .CountAsync(d => d.DeliveryEmployeeId == employee.Id && d.DeliveryStatus == "Delivered");

            return View(employee);
        }
    }
}
