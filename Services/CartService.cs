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

        private async Task ClearExpiredReservationsAsync()
        {
            var expiredTime = DateTime.Now.AddMinutes(-15);
            var expiredItems = await _context.CartItems
                .Where(ci => ci.AddedAt < expiredTime)
                .ToListAsync();

            foreach (var item in expiredItems)
            {
                if (item.ProductId.HasValue)
                {
                    var inventories = await _context.Inventories
                        .Where(i => i.ProductId == item.ProductId.Value && i.QuantityReserved > 0)
                        .ToListAsync();

                    int toRelease = item.Quantity;
                    foreach (var inv in inventories)
                    {
                        if (toRelease <= 0) break;
                        int amount = Math.Min(toRelease, inv.QuantityReserved);
                        inv.QuantityReserved -= amount;
                        toRelease -= amount;
                    }
                }
                _context.CartItems.Remove(item);
            }
            
            if (expiredItems.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Cart> GetCartAsync(int retailerId)
        {
            await ClearExpiredReservationsAsync();

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
            await ClearExpiredReservationsAsync();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.RetailerId == retailerId);

            return cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
        }

        public async Task AddToCartAsync(int retailerId, int productId, int quantity)
        {
            await ClearExpiredReservationsAsync();

            var cart = await GetCartAsync(retailerId);

            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found");

            // Allocate reservation
            var inventories = await _context.Inventories.Where(i => i.ProductId == productId).ToListAsync();
            int available = inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved);
            if (available < quantity) throw new Exception($"Insufficient stock available. Only {available} units left.");

            int toReserve = quantity;
            foreach(var inv in inventories.OrderByDescending(i => i.QuantityOnHand - i.QuantityReserved))
            {
                if (toReserve <= 0) break;
                int canReserve = inv.QuantityOnHand - inv.QuantityReserved;
                if (canReserve > 0)
                {
                    int amount = Math.Min(toReserve, canReserve);
                    inv.QuantityReserved += amount;
                    toReserve -= amount;
                }
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.AddedAt = DateTime.Now; // Refresh the timer
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
            await ClearExpiredReservationsAsync();
            
            var cart = await GetCartAsync(retailerId);
            var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
            
            if (item != null && item.ProductId.HasValue)
            {
                int diff = quantity - item.Quantity;
                if (diff > 0)
                {
                    var inventories = await _context.Inventories.Where(i => i.ProductId == item.ProductId.Value).ToListAsync();
                    int available = inventories.Sum(i => i.QuantityOnHand - i.QuantityReserved);
                    if (available < diff) throw new Exception("Insufficient stock available.");

                    int toReserve = diff;
                    foreach(var inv in inventories.OrderByDescending(i => i.QuantityOnHand - i.QuantityReserved))
                    {
                        if (toReserve <= 0) break;
                        int canReserve = inv.QuantityOnHand - inv.QuantityReserved;
                        if (canReserve > 0)
                        {
                            int amount = Math.Min(toReserve, canReserve);
                            inv.QuantityReserved += amount;
                            toReserve -= amount;
                        }
                    }
                }
                else if (diff < 0)
                {
                    int toRelease = -diff;
                    var inventories = await _context.Inventories.Where(i => i.ProductId == item.ProductId.Value).ToListAsync();
                    foreach(var inv in inventories.OrderByDescending(i => i.QuantityReserved))
                    {
                        if (toRelease <= 0) break;
                        if (inv.QuantityReserved > 0)
                        {
                            int amount = Math.Min(toRelease, inv.QuantityReserved);
                            inv.QuantityReserved -= amount;
                            toRelease -= amount;
                        }
                    }
                }
                
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                    item.AddedAt = DateTime.Now; // Refresh
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCartAsync(int retailerId, int cartItemId)
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.RetailerId == retailerId);
            if (item != null)
            {
                if (item.ProductId.HasValue)
                {
                    int toRelease = item.Quantity;
                    var inventories = await _context.Inventories.Where(i => i.ProductId == item.ProductId.Value).ToListAsync();
                    foreach(var inv in inventories.OrderByDescending(i => i.QuantityReserved))
                    {
                        if (toRelease <= 0) break;
                        if (inv.QuantityReserved > 0)
                        {
                            int amount = Math.Min(toRelease, inv.QuantityReserved);
                            inv.QuantityReserved -= amount;
                            toRelease -= amount;
                        }
                    }
                }
                
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(int retailerId)
        {
            var cart = await GetCartAsync(retailerId);
            if (cart.CartItems.Any())
            {
                foreach(var item in cart.CartItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        int toRelease = item.Quantity;
                        var inventories = await _context.Inventories.Where(i => i.ProductId == item.ProductId.Value).ToListAsync();
                        foreach(var inv in inventories.OrderByDescending(i => i.QuantityReserved))
                        {
                            if (toRelease <= 0) break;
                            if (inv.QuantityReserved > 0)
                            {
                                int amount = Math.Min(toRelease, inv.QuantityReserved);
                                inv.QuantityReserved -= amount;
                                toRelease -= amount;
                            }
                        }
                    }
                }
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
                var order = new Order
                {
                    OrderNumber = $"ORD-DIR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                    RetailerId = retailerId,
                    SupplierId = supplierId,
                    TotalAmount = supplierItems.Sum(i => (decimal)i.Quantity * i.Product.BasePrice),
                    OrderStatus = "Pending",
                    PaymentStatus = "Pending",
                    DeliveryAddress = deliveryAddress,
                    ExpectedDeliveryDate = expectedDeliveryDate,
                    CreatedAt = DateTime.Now
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

                // Notify supplier
                var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == supplierId);
                if (supplier != null)
                {
                    await _notificationService.SendNotificationAsync(
                        supplier.UserId,
                        "New Order Request",
                        $"New direct order request ({order.OrderNumber}) from {retailer?.BusinessName}. Please Review.",
                        "Info"
                    );
                }
            }

            // Remove items from Cart but DO NOT release QuantityReserved!
            // The reservation transitions from the CartItem to the newly created Pending Order.
            foreach(var item in cart.CartItems)
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync();
        }
    }
}
