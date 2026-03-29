using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class RetailerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly INotificationService _notificationService;

        public RetailerController(ApplicationDbContext context, ICartService cartService, INotificationService notificationService)
        {
            _context = context;
            _cartService = cartService;
            _notificationService = notificationService;
        }

        // Helper method to check if user is retailer
        private bool IsRetailer()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            var user = _context.Users.Find(userId);
            return user != null && user.Role == "Retailer" && user.IsApproved;
        }

        // GET: /Retailer/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers
                .Include(r => r.User)
                .Include(r => r.PurchaseOrders)
                .Include(r => r.Tenders)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (retailer == null) return NotFound();

            // Advanced Stats for Dashboard
            var orders = await _context.Orders
                .Where(o => o.RetailerId == retailer.Id)
                .ToListAsync();

            ViewBag.TotalOrders = orders.Count;
            ViewBag.ActiveOrders = orders.Count(o => o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" && o.OrderStatus != "Rejected");
            ViewBag.TotalPurchaseOrders = retailer.PurchaseOrders?.Count ?? 0;
            ViewBag.ActiveTenders = retailer.Tenders?.Count(t => t.Status == "Open") ?? 0;
            
            // Deliveries In Progress
            ViewBag.DeliveriesInProgress = await _context.PurchaseOrders
                .Where(po => po.RetailerId == retailer.Id && po.Status == "In Transit")
                .CountAsync();

            // Order Status Summary Bar
            ViewBag.StatusPending = orders.Count(o => o.OrderStatus == "Pending");
            ViewBag.StatusProcessing = orders.Count(o => o.OrderStatus == "Processing" || o.OrderStatus == "Partially Processing");
            ViewBag.StatusInTransit = await _context.PurchaseOrders
                .Where(po => po.RetailerId == retailer.Id && po.Status == "In Transit")
                .Select(po => po.OrderId)
                .Distinct()
                .CountAsync();
            ViewBag.StatusDelivered = orders.Count(o => o.OrderStatus == "Completed" || o.OrderStatus == "Delivered");

            // Recent Activity
            ViewBag.RecentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(5).ToList();
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(retailer);
        }

        // GET: /Retailer/OrderTracking
        public async Task<IActionResult> OrderTracking()
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            
            var orders = await _context.Orders
                .Include(o => o.Supplier)
                .Where(o => o.RetailerId == retailer.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: /Retailer/OrderTrackingDetails/5
        public async Task<IActionResult> OrderTrackingDetails(int id)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);

            var order = await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(po => po.Warehouse)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(po => po.DeliveryAgent)
                        .ThenInclude(da => da.User)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == retailer.Id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /Retailer/Notifications
        public async Task<IActionResult> Notifications()
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Mark all as read when viewing
            var unread = notifications.Where(n => !n.IsRead).ToList();
            if (unread.Any())
            {
                foreach (var n in unread) n.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(notifications);
        }

        // GET: /Retailer/AccountSettings
        public async Task<IActionResult> AccountSettings()
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (retailer == null) return NotFound();

            return View(retailer);
        }

        // GET: /Retailer/BrowseCatalog
        public async Task<IActionResult> BrowseCatalog(string searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string supplierCity)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventories)
                .Where(p => !p.IsDeleted && p.IsAvailable);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => p.ProductName.Contains(searchTerm) || p.Description.Contains(searchTerm));
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(p => p.CategoryId == categoryId.Value);
            if (minPrice.HasValue)
                query = query.Where(p => p.BasePrice >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= maxPrice.Value);
            if (!string.IsNullOrEmpty(supplierCity))
                query = query.Where(p => p.Supplier.City == supplierCity);

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            ViewData["Categories"] = await _context.ProductCategories.ToListAsync();
            ViewData["Cities"] = await _context.Suppliers.Select(s => s.City).Distinct().ToListAsync();
            
            return View(products);
        }

        // GET: /Retailer/ProductDetails/5
        public async Task<IActionResult> ProductDetails(int id)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            return View(product);
        }

        // APIs for Global Cart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromForm] int productId, [FromForm] int quantity = 1)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Not authenticated" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false, message = "Retailer not found" });

            await _cartService.AddToCartAsync(retailer.Id, productId, quantity);
            var count = await _cartService.GetCartItemCountAsync(retailer.Id);

            return Json(new { success = true, cartItemCount = count });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartJson()
        {
            if (!IsRetailer()) return Json(new { success = false });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false });

            var cart = await _cartService.GetCartAsync(retailer.Id);
            
            var result = new {
                success = true,
                items = cart.CartItems?.Select(i => new {
                    id = i.Id,
                    productId = i.ProductId,
                    productName = i.Product?.ProductName,
                    supplierName = i.Product?.Supplier?.CompanyName,
                    price = i.Product?.BasePrice,
                    quantity = i.Quantity,
                    maxQuantity = i.Product?.Inventories?.Sum(inv => inv.QuantityOnHand - inv.QuantityReserved) ?? 0,
                    total = (i.Quantity * (i.Product?.BasePrice ?? 0))
                }),
                subtotal = cart.CartItems?.Sum(i => i.Quantity * (i.Product?.BasePrice ?? 0)) ?? 0
            };

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartItem([FromForm] int cartItemId, [FromForm] int quantity)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Not authenticated" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            
            await _cartService.UpdateCartItemQuantityAsync(retailer.Id, cartItemId, quantity);
            var cart = await _cartService.GetCartAsync(retailer.Id);
            
            return Json(new { success = true, subtotal = cart.CartItems?.Sum(i => i.Quantity * (i.Product?.BasePrice ?? 0)) ?? 0 });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart([FromForm] int cartItemId)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Not authenticated" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);

            await _cartService.RemoveFromCartAsync(retailer.Id, cartItemId);
            var count = await _cartService.GetCartItemCountAsync(retailer.Id);
            var cart = await _cartService.GetCartAsync(retailer.Id);
            
            return Json(new { success = true, cartItemCount = count, subtotal = cart.CartItems?.Sum(i => i.Quantity * (i.Product?.BasePrice ?? 0)) ?? 0 });
        }

        [HttpPost]
        public async Task<IActionResult> Checkout([FromForm] string deliveryAddress, [FromForm] DateTime expectedDeliveryDate)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);

            try
            {
                await _cartService.CheckoutAsync(retailer.Id, deliveryAddress, expectedDeliveryDate);
                TempData["SuccessMessage"] = "Checkout successful! Orders have been generated and sent to suppliers.";
                // We will navigate them to the "Orders" tab so they can see pending orders.
                return RedirectToAction("Index", "Order");
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (ex.InnerException != null) message += " | Inner: " + ex.InnerException.Message;
                TempData["ErrorMessage"] = "Checkout failed: " + message;
                return RedirectToAction("Dashboard");
            }
        }
    }
}