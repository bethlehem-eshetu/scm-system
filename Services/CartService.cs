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

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
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

                // Create PO
                var po = new PurchaseOrder
                {
                    PONumber = $"PO-DIR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                    RetailerId = retailerId,
                    SupplierId = supplierId,
                    TotalAmount = supplierItems.Sum(i => i.Quantity * i.Product.BasePrice),
                    Status = "Pending",
                    DeliveryAddress = deliveryAddress,
                    ExpectedDeliveryDate = expectedDeliveryDate,
                    CreatedAt = DateTime.Now,
                    OrderDate = DateTime.Now,
                    PurchaseOrderItems = supplierItems.Select(i => new PurchaseOrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.Product.BasePrice
                    }).ToList()
                };

                _context.PurchaseOrders.Add(po);

                // Notify supplier
                var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == supplierId);
                if (supplier != null)
                {
                    await _notificationService.SendNotificationAsync(
                        supplier.UserId,
                        $"You received a new direct purchase order ({po.PONumber}) from {retailer?.BusinessName}.",
                        "Info"
                    );
                }
            }

            // Clear the cart
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();
        }
    }
}
