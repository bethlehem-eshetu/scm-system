using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    public class RetailerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly INotificationService _notificationService;
        private readonly IOrderService _orderService;

        public RetailerController(ApplicationDbContext context, ICartService cartService, INotificationService notificationService, IOrderService orderService)
        {
            _context = context;
            _cartService = cartService;
            _notificationService = notificationService;
            _orderService = orderService;
        }

        // Helper method to check if user is retailer
        private bool IsRetailer()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            var user = _context.Users.Find(userId);
            return user != null && user.Role == "Retailer" && user.IsApproved && user.IsFaydaVerified;
        }

        private async Task<int> GetRetailerIdInternalAsync()
        {
            var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? HttpContext.Session.GetInt32("UserId")?.ToString();
            if (int.TryParse(userIdStr, out int userId))
            {
                var r = await _context.Retailers.FirstOrDefaultAsync(x => x.UserId == userId);
                return r?.Id ?? 0;
            }
            return 0;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");
            
            var rId = await GetRetailerIdInternalAsync();
            var order = await _orderService.GetOrderByIdAsync(id);
            
            if (order == null || order.RetailerId != rId) return NotFound();
            
            bool success = await _orderService.CancelOrderAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Order cancelled successfully. Stock has been returned.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cancel order. It may have already been dispatched.";
            }
            
            return RedirectToAction("OrderTrackingDetails", new { id });
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

            // Unread messages and penalties
            ViewBag.UnreadMessagesCount = await _context.Messages
                .Where(m => m.Conversation.RetailerId == retailer.Id &&
                            m.SenderId != userId &&
                            !m.IsRead)
                .CountAsync();

            ViewBag.ActivePenalties = await _context.Penalties
               .CountAsync(p => p.UserId == userId && (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));

            // Advanced Stats for Dashboard
            var orders = await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.PurchaseOrders)
                .Where(o => o.RetailerId == retailer.Id)
                .ToListAsync();

            ViewBag.TotalOrders = orders.Count;
            ViewBag.ActiveOrders = orders.Count(o => o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" && o.OrderStatus != "Rejected");
            ViewBag.TotalPurchaseOrders = retailer.PurchaseOrders?.Count ?? 0;
            ViewBag.ActiveTenders = retailer.Tenders?.Count(t => t.Status == "Open") ?? 0;

            // Performance KPIs
            ViewBag.TotalSpent = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            ViewBag.AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0;
            
            var totalOrders = orders.Count;
            var completedOrdersCount = orders.Count(o => o.OrderStatus == "Completed");
            ViewBag.OnTimeDeliveryRate = totalOrders > 0 ? Math.Round((completedOrdersCount * 100.0 / totalOrders), 1) : 0;

            var ratingsList = await _context.Ratings
                .Where(r => r.RetailerId == retailer.Id)
                .ToListAsync();
            ViewBag.AverageRatingGiven = ratingsList.Any() ? ratingsList.Average(r => r.RatingValue) : 0;

            // Deliveries In Progress
            ViewBag.DeliveriesInProgress = await _context.PurchaseOrders
                .Where(po => po.RetailerId == retailer.Id && po.Status == "In Transit")
                .CountAsync();

            // Status Summary
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

            var recentConversations = await _context.Conversations
                .Include(c => c.Supplier)
                .Include(c => c.Messages)
                .Where(c => c.RetailerId == retailer.Id)
                .OrderByDescending(c => c.LastMessageAt)
                .Take(4)
                .Select(c => new
                {
                    OtherUserId = c.Supplier.UserId,
                    OtherUserName = c.Supplier.CompanyName,
                    OtherUserRole = "Supplier",
                    LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault() != null 
                        ? c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault().MessageText 
                        : "No messages yet",
                    LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId)
                })
                .ToListAsync();
            ViewBag.RecentConversations = recentConversations;

            ViewBag.AllCategories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
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
                .Include(o => o.PurchaseOrders)
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
                    .ThenInclude(po => po.Commission)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == retailer.Id);

            if (order == null) return NotFound();

            var statusHistory = await _context.OrderStatusHistories
                .Where(h => h.OrderId == order.Id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            ViewBag.StatusHistory = statusHistory;

            return View(order);
        }

        // GET: /Retailer/MyPurchaseOrders
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

            if (product.Supplier != null)
            {
                var ratings = product.Supplier.ReceivedRatings;
                ViewBag.AverageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.RatingValue), 1) : 0;
                ViewBag.RatingCount = ratings.Count();
                
                ViewBag.DealsCompleted = await _context.Orders
                    .CountAsync(o => o.SupplierId == product.SupplierId && (o.OrderStatus == "Completed" || o.OrderStatus == "Delivered"));
            }

            var totalOnHand = product.Inventories?.Sum(i => i.QuantityOnHand) ?? 0;
            var totalReserved = product.Inventories?.Sum(i => i.QuantityReserved) ?? 0;
            ViewBag.AvailableStock = totalOnHand - totalReserved;

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Session expired. Please login again." });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false, message = "Retailer profile not found" });

            try
            {
                await _cartService.AddToCartAsync(retailer.Id, productId, quantity);
                var count = await _cartService.GetCartItemCountAsync(retailer.Id);

                return Json(new { 
                    success = true, 
                    message = "Product added to cart!", 
                    cartItemCount = count 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
        public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Not authenticated" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);

            await _cartService.UpdateCartItemQuantityAsync(retailer.Id, cartItemId, quantity);
            var cart = await _cartService.GetCartAsync(retailer.Id);

            return Json(new { success = true, subtotal = cart.CartItems?.Sum(i => i.Quantity * (i.Product?.BasePrice ?? 0)) ?? 0 });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
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
        public async Task<IActionResult> Checkout(string deliveryAddress, DateTime expectedDeliveryDate)
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

        [HttpPost]
        public async Task<IActionResult> UpdateBusinessInfo(Retailer model)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false, message = "Retailer not found" });

            retailer.BusinessName = model.BusinessName;
            retailer.BusinessType = model.BusinessType;
            retailer.BusinessLicenseNumber = model.BusinessLicenseNumber;
            retailer.TaxIdentificationNumber = model.TaxIdentificationNumber;
            retailer.StoreSize = model.StoreSize;
            retailer.City = model.City;
            retailer.BusinessAddress = model.BusinessAddress;
            retailer.Description = model.Description;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Business information updated successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateContactDetails(string contactPersonName, string contactPersonEmail, string contactPersonPhone)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.Include(r => r.User).FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false, message = "Retailer not found" });

            retailer.ContactPersonName = contactPersonName;
            retailer.ContactPersonEmail = contactPersonEmail;
            retailer.ContactPersonPhone = contactPersonPhone;

            if (retailer.User != null)
            {
                retailer.User.FullName = contactPersonName ?? retailer.User.FullName;
                retailer.User.Email = contactPersonEmail ?? retailer.User.Email;
                retailer.User.PhoneNumber = contactPersonPhone ?? retailer.User.PhoneNumber;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Contact details updated successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProcurementDefaults(int defaultTenderClosingDays, int preferredDeliveryTimeline, decimal? budgetMin, decimal? budgetMax, string preferredCategories)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false, message = "Retailer not found" });

            retailer.DefaultTenderClosingDays = defaultTenderClosingDays;
            retailer.PreferredDeliveryTimeline = preferredDeliveryTimeline;
            retailer.BudgetMin = budgetMin;
            retailer.BudgetMax = budgetMax;
            retailer.PreferredCategories = preferredCategories;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Procurement defaults updated!" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderPreferences(string defaultShippingMethod, bool proofOfDeliveryRequired, bool deliveryNotifications)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            
            retailer.DefaultShippingMethod = defaultShippingMethod;
            retailer.ProofOfDeliveryRequired = proofOfDeliveryRequired;
            retailer.DeliveryNotifications = deliveryNotifications;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Order preferences updated!" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNotificationPreferences(bool newTenderMatchAlert, bool bidAcceptedAlert, bool orderShippedAlert, bool orderDeliveredAlert, bool lowStockAlert, bool priceDropAlert)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);

            retailer.NewTenderMatchAlert = newTenderMatchAlert;
            retailer.BidAcceptedAlert = bidAcceptedAlert;
            retailer.OrderShippedAlert = orderShippedAlert;
            retailer.OrderDeliveredAlert = orderDeliveredAlert;
            retailer.LowStockAlert = lowStockAlert;
            retailer.PriceDropAlert = priceDropAlert;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Notification preferences updated!" });
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (!IsRetailer()) return RedirectToAction("AccountSettings");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null || profilePicture == null) return RedirectToAction("AccountSettings");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            retailer.BusinessLogo = "/uploads/profiles/" + fileName;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile picture updated!";
            return RedirectToAction("AccountSettings");
        }

        [HttpPost]
        public async Task<IActionResult> AddAddress(RetailerAddress address)
        {
            if (!IsRetailer()) return Json(new { success = false });

            var rId = await GetRetailerIdInternalAsync();
            address.RetailerId = rId;

            if (address.IsDefault)
            {
                var existingDefault = await _context.RetailerAddresses.FirstOrDefaultAsync(a => a.RetailerId == rId && a.AddressType == address.AddressType && a.IsDefault);
                if (existingDefault != null) existingDefault.IsDefault = false;
            }

            _context.RetailerAddresses.Add(address);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            if (!IsRetailer()) return Json(new { success = false });

            var address = await _context.RetailerAddresses.FindAsync(id);
            if (address != null)
            {
                _context.RetailerAddresses.Remove(address);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddPaymentMethod(RetailerPaymentMethod method)
        {
            if (!IsRetailer()) return Json(new { success = false });

            var rId = await GetRetailerIdInternalAsync();
            method.RetailerId = rId;

            if (method.IsDefault)
            {
                var existingDefault = await _context.RetailerPaymentMethods.FirstOrDefaultAsync(p => p.RetailerId == rId && p.IsDefault);
                if (existingDefault != null) existingDefault.IsDefault = false;
            }

            _context.RetailerPaymentMethods.Add(method);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RevokeSession(int sessionId)
        {
            if (!IsRetailer()) return Json(new { success = false });

            var session = await _context.UserSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplierPreferences(string favoriteSuppliers, string blockedSuppliers, int supplierRatingThreshold)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null) return Json(new { success = false, message = "Retailer not found" });

            retailer.FavoriteSuppliers = favoriteSuppliers;
            retailer.BlockedSuppliers = blockedSuppliers;
            retailer.SupplierRatingThreshold = supplierRatingThreshold;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Supplier preferences updated!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateAccount(string password)
        {
            if (!IsRetailer()) return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FindAsync(userId);

            if (user.PasswordHash == password)
            {
                user.AccountStatus = "Suspended";
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Account deactivated." });
            }

            return Json(new { success = false, message = "Invalid password." });
        }

        [HttpGet]
        public async Task<IActionResult> ExportData()
        {
            if (!IsRetailer()) return Unauthorized();

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Property,Value");
            csv.AppendLine($"Business Name,{retailer.BusinessName}");
            csv.AppendLine($"Business Type,{retailer.BusinessType}");
            csv.AppendLine($"City,{retailer.City}");
            csv.AppendLine($"Created At,{retailer.CreatedAt}");
            
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", $"Retailer_Data_{retailer.Id}.csv");
        }

        // GET: /Retailer/PayOrder/5
        public async Task<IActionResult> PayOrder(int id)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);

            var order = await _context.Orders
                .Include(o => o.Supplier)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == retailer.Id);

            if (order == null) return NotFound();

            if (order.PaymentStatus != "Pending")
            {
                TempData["ErrorMessage"] = "This order is already paid or in escrow.";
                return RedirectToAction("OrderTrackingDetails", new { id = order.Id });
            }

            // Initialize Chapa Payment
            var txRef = $"ORD-PAY-{order.Id}-{DateTime.Now.Ticks}";
            var user = await _context.Users.FindAsync(userId);

            var chapaService = HttpContext.RequestServices.GetRequiredService<IChapaService>();
            var request = new ChapaPaymentRequest
            {
                Amount = order.TotalAmount,
                Currency = "ETB",
                Email = user?.Email ?? "",
                FirstName = user?.FullName?.Split(' ').FirstOrDefault() ?? "",
                LastName = user?.FullName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                TxRef = txRef,
                CallbackUrl = $"{Request.Scheme}://{Request.Host}/Retailer/PaymentCallback",
                ReturnUrl = $"{Request.Scheme}://{Request.Host}/Retailer/PaymentCallback?tx_ref={txRef}",
                CustomizationTitle = "EthioChain Order Payment",
                CustomizationDescription = $"Payment for Order #{order.OrderNumber}"
            };

            var result = await chapaService.InitializePaymentAsync(request);

            if (result.Success)
            {
                return Redirect(result.PaymentUrl);
            }

            TempData["ErrorMessage"] = "Failed to initialize payment: " + result.Message;
            return RedirectToAction("OrderTrackingDetails", new { id = order.Id });
        }

        // GET: /Retailer/PaymentCallback
        public async Task<IActionResult> PaymentCallback(string tx_ref, string status)
        {
            if (!IsRetailer()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(tx_ref)) return RedirectToAction("OrderTracking");

            var chapaService = HttpContext.RequestServices.GetRequiredService<IChapaService>();
            var verifyResult = await chapaService.VerifyPaymentAsync(tx_ref);

            if (verifyResult.Success && verifyResult.Status == "success")
            {
                var parts = tx_ref.Split('-');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int orderId))
                {
                    var order = await _context.Orders.FindAsync(orderId);
                    if (order != null && order.PaymentStatus == "Pending")
                    {
                        order.PaymentStatus = "Escrow";
                        
                        var history = new OrderStatusHistory
                        {
                            OrderId = order.Id,
                            Status = order.OrderStatus,
                            Comments = "Payment received and held in Escrow via Chapa.",
                            ChangedByUserId = HttpContext.Session.GetInt32("UserId") ?? 0,
                            ChangedAt = DateTime.Now
                        };
                        _context.OrderStatusHistories.Add(history);
                        
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Payment successful! Your funds are now held in Escrow.";
                        return RedirectToAction("OrderTrackingDetails", new { id = orderId });
                    }
                }
            }

            TempData["ErrorMessage"] = "Payment verification failed or timed out.";
            return RedirectToAction("OrderTracking");
        }
    }
}
