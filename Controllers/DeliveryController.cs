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

        // GET: /Delivery/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || role != "Delivery")
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the employee record and include the supplier info
            var employee = await _context.SupplierEmployees
                .Include(se => se.User)
                .Include(se => se.Supplier)
                .FirstOrDefaultAsync(se => se.UserId == userId);

            if (employee == null)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View(employee);
        }
    }
}
