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
            if (User.IsInRole("Retailer")) return RedirectToAction(nameof(RetailerIndex));
            if (User.IsInRole("Supplier")) return RedirectToAction(nameof(SupplierIndex));
            if (User.IsInRole("WarehouseManager")) return RedirectToAction(nameof(SupplierIndex));

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> RetailerIndex()
        {
            if (!User.IsInRole("Retailer")) return Forbid();
            var rId = await GetRetailerIdAsync();
            var pos = await _poService.GetPurchaseOrdersByRetailerAsync(rId);
            return View("RetailerIndex", pos);
        }

        public async Task<IActionResult> SupplierIndex()
        {
            if (!User.IsInRole("Supplier") && !User.IsInRole("WarehouseManager")) return Forbid();
            
            if (User.IsInRole("WarehouseManager"))
            {
                var wId = await GetWarehouseIdAsync();
                var pos = await _poService.GetPurchaseOrdersByWarehouseAsync(wId);
                return View("SupplierIndex", pos);
            }
            else
            {
                var sId = await GetSupplierIdAsync();
                var pos = await _poService.GetPurchaseOrdersBySupplierAsync(sId);
                return View("SupplierIndex", pos);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var po = await _poService.GetPurchaseOrderByIdAsync(id);
            if (po == null) return NotFound();

            if (User.IsInRole("Retailer"))
            {
                var rId = await GetRetailerIdAsync();
                if (po.RetailerId != rId) return Forbid();
            }
            else if (User.IsInRole("Supplier"))
            {
                var sId = await GetSupplierIdAsync();
                if (po.SupplierId != sId) return Forbid();
            }
            else if (User.IsInRole("WarehouseManager"))
            {
                var wId = await GetWarehouseIdAsync();
                if (po.WarehouseId != wId) return Forbid();
            }
            else
            {
                return Forbid();
            }

            return View(po);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                await _poService.UpdatePurchaseOrderStatusAsync(id, status, userId);
            }
            return RedirectToAction(nameof(Details), new { id });
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

        private async Task<int> GetWarehouseIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var emp = await _context.SupplierEmployees.FirstOrDefaultAsync(x => x.UserId == userId);
                return emp?.WarehouseId ?? 0;
            }
            return 0;
        }
    }
}
