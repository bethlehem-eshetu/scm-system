using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Controllers
{
    public class LogisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LogisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult DispatchCenter()
        {
            return View();
        }

        public IActionResult InventoryTransfers()
        {
            return View();
        }

        public IActionResult CreateTransfer()
        {
            return View();
        }
    }
}
