using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public CartService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Cart> GetCartAsync(int retailerId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Supplier)
                .FirstOrDefaultAsync(c => c.RetailerId == retailerId);

            if (cart == null)
            {
                cart = new Cart { RetailerId = retailerId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<int> GetCartItemCountAsync(int retailerId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.RetailerId == retailerId);

            return cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
        }

        public async Task AddToCartAsync(int retailerId, int productId, int quantity)
        {
            var cart = await GetCartAsync(retailerId);

            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found");

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.AddedAt = DateTime.Now;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    ProductName = product.ProductName,
                    UnitPrice = product.BasePrice,
                    Description = product.Description,
                    Quantity = quantity,
                    AddedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateCartItemQuantityAsync(int retailerId, int cartItemId, int quantity)
        {
            var cart = await GetCartAsync(retailerId);
            var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
            
            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                    item.AddedAt = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCartAsync(int retailerId, int cartItemId)
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.RetailerId == retailerId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(int retailerId)
        {
            var cart = await GetCartAsync(retailerId);
            if (cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CheckoutAsync(int retailerId, string deliveryAddress, DateTime expectedDeliveryDate)
        {
            var cart = await GetCartAsync(retailerId);
            if (!cart.CartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            // Group items by Supplier
            var supplierGroups = cart.CartItems.GroupBy(ci => ci.Product.SupplierId);

            var retailer = await _context.Retailers.FindAsync(retailerId);

            foreach (var group in supplierGroups)
            {
                var supplierId = group.Key;
                var supplierItems = group.ToList();

                // Create Order directly
                var netAmount = supplierItems.Sum(i => (decimal)i.Quantity * i.Product.BasePrice);
                var vatAmount = Math.Round(netAmount * 0.15m, 2);
                var totalAmount = netAmount + vatAmount;

                var order = new Order
                {
                    OrderNumber = $"ORD-DIR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                    RetailerId = retailerId,
                    SupplierId = supplierId,
                    Subtotal = netAmount,
                    VAT = vatAmount,
                    TotalAmount = totalAmount, 
                    OrderStatus = "Pending",
                    PaymentStatus = "Pending",
                    DeliveryAddress = deliveryAddress,
                    ExpectedDeliveryDate = expectedDeliveryDate,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                order.OrderItems = supplierItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? i.Product.ProductName : (i.ProductName ?? "Unknown Product"),
                    Description = i.Product != null ? i.Product.Description : i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product != null ? i.Product.BasePrice : (i.UnitPrice ?? 0),
                    Order = order
                }).ToList();

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Save to get Order.Id

                // Create initial unassigned PurchaseOrder for fulfillment tracking
                var po = new PurchaseOrder
                {
                    PONumber = $"PO-DIR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                    OrderId = order.Id,
                    RetailerId = retailerId,
                    SupplierId = supplierId,
                    WarehouseId = null, // IMPORTANT: Enforce unassigned initial state
                    Subtotal = netAmount,
                    VAT = vatAmount,
                    TotalAmount = totalAmount,
                    Status = "Issued",
                    DeliveryAddress = deliveryAddress,
                    ExpectedDeliveryDate = expectedDeliveryDate,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    PurchaseOrderItems = order.OrderItems.Select(i => new PurchaseOrderItem
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                };

                _context.PurchaseOrders.Add(po);

                // Notify Admin, Supplier and Retailer
                var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == supplierId);
                if (supplier != null)
                {
                    // Admin Notification
                    var adminNote = new AdminNotification
                    {
                        Title = "New Order Request",
                        Message = $"A new order (#{order.OrderNumber}) has been placed by {retailer?.BusinessName ?? "Retailer"}.",
                        NotificationType = "Order",
                        ActionUrl = "/Admin/Orders",
                        CreatedAt = DateTime.Now
                    };
                    await _notificationService.CreateAdminNotificationAsync(adminNote);

                    // Supplier Notification
                    await _notificationService.SendNotificationAsync(
                        supplier.UserId,
                        "New Order Received",
                        $"New direct order request ({order.OrderNumber}) from {retailer?.BusinessName ?? "Retailer"}. Please Review.",
                        "Order",
                        $"/Order/Details/{order.Id}"
                    );

                    // Retailer Notification
                    if (retailer != null)
                    {
                        await _notificationService.SendNotificationAsync(
                            retailer.UserId,
                            "Order Placed successfully",
                            $"Your order #{order.OrderNumber} has been received and is pending supplier acceptance.",
                            "Order",
                            $"/Retailer/OrderTracking"
                        );
                    }
                }
            }

            // Remove items from Cart
            foreach(var item in cart.CartItems)
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync();
        }
    }
}
