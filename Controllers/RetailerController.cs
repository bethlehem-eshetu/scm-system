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
            return user != null && user.Role == "Retailer" && user.IsApproved && user.IsFaydaVerified;
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

            if (retailer.User != null && !string.IsNullOrEmpty(retailer.User.ApprovalStatusMessage))
            {
                ViewBag.ApprovalStatusMessage = retailer.User.ApprovalStatusMessage;
                ViewBag.ApprovalStatusType = retailer.User.ApprovalStatusType;

                // Clear after read
                retailer.User.ApprovalStatusMessage = null;
                await _context.SaveChangesAsync();
            }

            // ========== ADD MESSAGING VIEWBAGS ==========

            // Get unread message count
            ViewBag.UnreadMessagesCount = await _context.Messages
                .Where(m => m.Conversation.RetailerId == retailer.Id &&
                            m.SenderId != userId &&
                            !m.IsRead)
                .CountAsync();

            // Get active penalties count
            ViewBag.ActivePenalties = await _context.Penalties
               .CountAsync(p => p.UserId == userId && (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));

            // Get recent conversations for dashboard widget
            ViewBag.RecentConversations = await _context.Conversations
                .Include(c => c.Retailer)
                    .ThenInclude(r => r.User)
                .Include(c => c.Supplier)
                    .ThenInclude(s => s.User)
                .Where(c => c.RetailerId == retailer.Id)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Take(5)
                .Select(c => new
                {
                    Id = c.Id,
                    OtherUserId = c.SupplierId,
                    OtherUserName = c.Supplier != null ? c.Supplier.User.FullName : "Supplier",
                    OtherUserRole = "Supplier",
                    LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.MessageText.Length > 60 ? m.MessageText.Substring(0, 60) + "..." : m.MessageText)
                        .FirstOrDefault() ?? "No messages yet",
                    LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead)
                })
                .ToListAsync();

            // ========== END OF MESSAGING VIEWBAGS ==========

            // Advanced Stats for Dashboard
            var orders = await _context.Orders
                .Include(o => o.PurchaseOrders)
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

        // GET: /Retailer/OrderTracking - FIXED to include Purchase Orders
        public async Task<IActionResult> OrderTracking()
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);

            var orders = await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.PurchaseOrders)  // ✅ Add this to include POs
                    .ThenInclude(po => po.Warehouse)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(po => po.DeliveryAgent)
                        .ThenInclude(da => da.User)
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
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(po => po.Commission)  // ✅ Add this to include payment info
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == retailer.Id);

            if (order == null) return NotFound();

            // Load StatusHistory separately
            var statusHistory = await _context.OrderStatusHistories
                .Where(h => h.OrderId == order.Id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            ViewBag.StatusHistory = statusHistory;

            return View(order);
        }

        // GET: /Retailer/MyPurchaseOrders - Redirects to proper purchase orders
        public async Task<IActionResult> MyPurchaseOrders()
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            return RedirectToAction("RetailerIndex", "PurchaseOrder");
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
                .Where(p => !p.IsDeleted && p.IsAvailable && p.Category.IsActive);

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
            ViewData["Categories"] = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
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
                    .ThenInclude(s => s.ReceivedRatings)
                .Include(p => p.Inventories)
                .Include(p => p.AttributeValues)
                    .ThenInclude(av => av.AttributeDefinition)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            // Calculate Metrics
            if (product.Supplier != null)
            {
                var ratings = product.Supplier.ReceivedRatings;
                ViewBag.AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 1) : 0;
                ViewBag.RatingCount = ratings.Count();
                
                ViewBag.DealsCompleted = await _context.Orders
                    .CountAsync(o => o.SupplierId == product.SupplierId && (o.OrderStatus == "Completed" || o.OrderStatus == "Delivered"));
            }

            return View(product);
        }

        // APIs for Global Cart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromForm] int productId, [FromForm] int quantity = 1)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return NotFound();

            await _cartService.AddToCartAsync(retailer.Id, productId, quantity);
            
            TempData["SuccessMessage"] = "Product added to cart!";
            return RedirectToAction(nameof(Cart));
        }

        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            
            var cart = await _cartService.GetCartAsync(retailer.Id);
            return View(cart);
        }

        [HttpGet]
        public async Task<IActionResult> GetCartJson()
        {
            if (!IsRetailer()) return Json(new { success = false });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false });

            var cart = await _cartService.GetCartAsync(retailer.Id);

            var result = new
            {
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
                return RedirectToAction("OrderTracking");
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