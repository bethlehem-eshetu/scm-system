using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public PurchaseOrderController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // GET: PurchaseOrder
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            // Both Roles use this view.
            // Retailers see POs they sent. Suppliers see POs they received.
            var query = _context.PurchaseOrders
                .Include(p => p.Retailer)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (role == "Retailer")
            {
                query = query.Where(p => p.RetailerId == userId);
            }
            else if (role == "Supplier")
            {
                query = query.Where(p => p.SupplierId == userId);
            }
            else
            {
                return Unauthorized();
            }

            var pos = await query.OrderByDescending(p => p.OrderDate).ToListAsync();
            return View(pos);
        }

        // GET: PurchaseOrder/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            var po = await _context.PurchaseOrders
                .Include(p => p.Retailer)
                    .ThenInclude(r => r.User)
                .Include(p => p.Supplier)
                    .ThenInclude(s => s.User)
                .Include(p => p.TenderBid)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return NotFound();

            // Security Rule: Must be a party to this Purchase Order
            if (role == "Retailer" && po.RetailerId != userId) return Unauthorized();
            if (role == "Supplier" && po.SupplierId != userId) return Unauthorized();

            return View(po);
        }

        // GET: PurchaseOrder/Create (from Marketplace)
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Create(int supplierId, int productId)
        {
            var product = await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == productId && p.SupplierId == supplierId);

            if (product == null) return NotFound("Product or Supplier not found.");
            if (product.Inventory == null || product.Inventory.QuantityAvailable <= 0) return BadRequest("This product is out of stock.");

            // Present a generation form that has snapshot values prepared
            ViewBag.Product = product;
            return View(new PurchaseOrder
            {
                SupplierId = supplierId,
                ProductId = productId,
                ProductName = product.ProductName,
                UnitPrice = product.BasePrice,
                Quantity = 1 // Default to 1
            });
        }

        // POST: PurchaseOrder/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Create([Bind("SupplierId,ProductId,ProductName,UnitPrice,Quantity")] PurchaseOrder po)
        {
            var product = await _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == po.ProductId);
            
            if (product == null) return NotFound();
            
            var availableQty = product.Inventory?.QuantityAvailable ?? 0;
            if (po.Quantity > availableQty)
            {
                ModelState.AddModelError("Quantity", $"Only {availableQty} units are available in stock.");
            }

            if (ModelState.IsValid)
            {
                po.RetailerId = GetCurrentUserId();
                po.PONumber = $"PO-D-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
                po.TotalAmount = po.UnitPrice * po.Quantity;
                po.Status = "Pending";
                po.OrderDate = DateTime.Now;

                _context.Add(po);
                await _context.SaveChangesAsync();
                
                await _notificationService.SendNotificationAsync(
                    po.SupplierId,
                    $"You received a new direct Purchase Order ({po.PONumber}) for {po.Quantity}x {product.ProductName}.",
                    "PurchaseOrderCreated");

                TempData["SuccessMessage"] = $"Purchase Order {po.PONumber} sent successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Product = product;
            return View(po);
        }

        // POST: PurchaseOrder/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> Accept(int id)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po == null) return NotFound();

            // Security Rule: Only the designated supplier
            if (po.SupplierId != GetCurrentUserId()) return Unauthorized();

            if (po.Status == "Pending")
            {
                po.Status = "Accepted";
                
                // Deduct stock quantity from System product inventory if it's a Direct Purchase
                if (po.TenderBidId == null) 
                {
                    var product = await _context.Products.Include(p => p.Inventory).FirstOrDefaultAsync(p => p.Id == po.ProductId);
                    if (product?.Inventory != null)
                    {
                        product.Inventory.QuantityAvailable -= po.Quantity;
                        if (product.Inventory.QuantityAvailable < 0) product.Inventory.QuantityAvailable = 0;
                        _context.Update(product);
                    }
                }

                // Auto-create fulfillment Order
                var order = new Order
                {
                    PurchaseOrderId = po.Id,
                    SupplierId = po.SupplierId,
                    OrderStatus = "Processing",
                    CreatedAt = DateTime.Now
                };
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                
                await _notificationService.SendNotificationAsync(
                    po.RetailerId,
                    $"Purchase Order {po.PONumber} was accepted by the supplier. A fulfillment order has been generated.",
                    "PurchaseOrderAccepted");
                
                TempData["SuccessMessage"] = "Purchase Order accepted! Fulfillment order generated.";
            }

            return RedirectToAction(nameof(Details), new { id = po.Id });
        }

        // POST: PurchaseOrder/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> Reject(int id)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po == null) return NotFound();

            // Security Rule
            if (po.SupplierId != GetCurrentUserId()) return Unauthorized();

            if (po.Status == "Pending")
            {
                po.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Purchase Order rejected.";
            }

            return RedirectToAction(nameof(Details), new { id = po.Id });
        }
        
        // POST: PurchaseOrder/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po == null) return NotFound();

            // Security Rule
            if (po.RetailerId != GetCurrentUserId()) return Unauthorized();

            if (po.Status == "Pending")
            {
                po.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Purchase Order cancelled.";
            }

            return RedirectToAction(nameof(Details), new { id = po.Id });
        }
    }
}
