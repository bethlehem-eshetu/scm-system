using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderService _poService;
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;

        public PurchaseOrderController(IPurchaseOrderService poService, IOrderService orderService, ApplicationDbContext context)
        {
            _poService = poService;
            _orderService = orderService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Retailer"))
            {
                var rId = await GetRetailerIdAsync();
                var pos = await _poService.GetPurchaseOrdersByRetailerAsync(rId);
                return View("RetailerIndex", pos);
            }
            else if (User.IsInRole("Supplier"))
            {
                var sId = await GetSupplierIdAsync();
                var pos = await _poService.GetPurchaseOrdersBySupplierAsync(sId);
                return View("SupplierIndex", pos);
            }
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Details(int id)
        {
            var po = await _poService.GetPurchaseOrderByIdAsync(id);
            if (po == null) return NotFound();
            return View(po);
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusAction(int id, string actionType)
        {
            if (actionType == "Accept")
            {
                await _poService.UpdatePurchaseOrderStatusAsync(id, "Accepted");
                await _orderService.CreateOrderFromPurchaseOrderAsync(id);
            }
            else if (actionType == "Reject")
            {
                await _poService.UpdatePurchaseOrderStatusAsync(id, "Rejected");
            }
            
            return RedirectToAction(nameof(Index));
        }

        private async Task<int> GetRetailerIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var r = await _context.Retailers.FirstOrDefaultAsync(x => x.UserId == userId);
                return r?.Id ?? 0;
            }
            return 0;
        }

        private async Task<int> GetSupplierIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var s = await _context.Suppliers.FirstOrDefaultAsync(x => x.UserId == userId);
                return s?.Id ?? 0;
            }
            return 0;
        }
    }
}
