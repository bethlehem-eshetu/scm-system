using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;
using SCM_System.Models.Entities;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderService _poService;
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;

        public PurchaseOrderController(IPurchaseOrderService poService, IOrderService orderService, ApplicationDbContext context, IInventoryService inventoryService)
        {
            _poService = poService;
            _orderService = orderService;
            _context = context;
            _inventoryService = inventoryService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Retailer")) return RedirectToAction(nameof(RetailerIndex));
            if (User.IsInRole("Supplier")) return RedirectToAction(nameof(SupplierIndex));
            if (User.IsInRole("WarehouseManager")) return RedirectToAction(nameof(SupplierIndex));

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> RetailerIndex(string searchTerm, int page = 1)
        {
            if (!User.IsInRole("Retailer")) return Forbid();
            var rId = await GetRetailerIdAsync();
            
            var query = _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Where(po => po.RetailerId == rId)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(po => po.PONumber.Contains(searchTerm));
            }

            int pageSize = 10;
            var pos = await query
                .OrderByDescending(po => po.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);

            return View("RetailerIndex", pos);
        }

        public async Task<IActionResult> SupplierIndex(string searchTerm, int page = 1)
        {
            if (!User.IsInRole("Supplier") && !User.IsInRole("WarehouseManager")) return Forbid();
            
            IQueryable<PurchaseOrder> query = _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Include(po => po.Warehouse)
                .AsNoTracking();

            if (User.IsInRole("WarehouseManager"))
            {
                var wId = await GetWarehouseIdAsync();
                query = query.Where(po => po.WarehouseId == wId);
            }
            else
            {
                var sId = await GetSupplierIdAsync();
                query = query.Where(po => po.SupplierId == sId);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(po => po.PONumber.Contains(searchTerm) || po.Retailer.BusinessName.Contains(searchTerm));
            }

            int pageSize = 10;
            var pos = await query
                .OrderByDescending(po => po.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);

            return View("SupplierIndex", pos);
        }

        [Route("PurchaseOrder/Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            PurchaseOrder po = null;
            if (int.TryParse(id, out int poId))
            {
                po = await _poService.GetPurchaseOrderByIdAsync(poId);
            }

            if (po == null)
            {
                po = await _poService.GetPurchaseOrderByNumberAsync(id);
            }

            if (po == null)
            {
                TempData["ErrorMessage"] = "Purchase Order not found.";
                return RedirectToAction("Index", "Order");
            }

            // Redirect to unified Order Details
            return RedirectToAction("Details", "Order", new { id = po.OrderId, poId = po.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                try
                {
                    await _poService.UpdatePurchaseOrderStatusAsync(id, status, userId);
                    TempData["SuccessMessage"] = $"Purchase Order {id} status updated to {status}.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPO(int id, string cancellationReason)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                try
                {
                    var po = await _context.PurchaseOrders.FindAsync(id);
                    if (po != null && (po.Status == "Issued" || po.Status == "Pending"))
                    {
                        po.CancellationReason = cancellationReason;
                        po.UpdatedAt = DateTime.Now;
                        _context.Update(po);
                        await _context.SaveChangesAsync();
                        await _poService.UpdatePurchaseOrderStatusAsync(id, "Rejected", userId);
                        TempData["ErrorMessage"] = "Purchase Order Rejected.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPO(int id, int? warehouseId)
        {
            var po = await _context.PurchaseOrders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (po == null)
            {
                TempData["ErrorMessage"] = "PO not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!warehouseId.HasValue || warehouseId == 0)
            {
                TempData["ErrorMessage"] = "Warehouse selection is required.";
                return RedirectToAction("Details", "Order", new { id = po.OrderId, poId = po.Id });
            }

            return RedirectToAction(
                "AcceptPO",
                "Order",
                new
                {
                    orderId = po.OrderId,
                    poId = po.Id,
                    warehouseId = warehouseId.Value
                });
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
