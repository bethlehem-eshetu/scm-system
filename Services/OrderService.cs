using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetOrdersByRetailerAsync(int retailerId)
        {
            return await _context.Orders
                .Include(o => o.Supplier)
                .Where(o => o.RetailerId == retailerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersBySupplierAsync(int supplierId)
        {
            return await _context.Orders
                .Include(o => o.Retailer)
                .Where(o => o.SupplierId == supplierId)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Supplier)
                .Include(o => o.PurchaseOrder)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.StatusHistory)
                .Include(o => o.Delivery)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order> CreateOrderFromPurchaseOrderAsync(int purchaseOrderId)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

            if (po == null || po.Status != "Accepted") return null;

            var order = new Order
            {
                OrderNumber = "ORD-" + DateTime.Now.Ticks.ToString().Substring(8),
                PurchaseOrderId = po.Id,
                SupplierId = po.SupplierId,
                RetailerId = po.RetailerId,
                TotalAmount = po.TotalAmount,
                OrderStatus = "Processing",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create Order items matching PO items
            foreach(var item in po.PurchaseOrderItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                _context.OrderItems.Add(orderItem);
            }

            // Create initial status history
            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "Processing",
                Comments = "Order generated from Purchase Order",
                ChangedByUserId = po.Supplier.UserId,
                ChangedAt = DateTime.Now
            };
            
            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, string status, string comments, int changedByUserId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.OrderStatus = status;
                
                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = status,
                    Comments = comments,
                    ChangedByUserId = changedByUserId,
                    ChangedAt = DateTime.Now
                };
                
                _context.OrderStatusHistories.Add(history);
                await _context.SaveChangesAsync();
            }
            return order;
        }

        public async Task<IEnumerable<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId)
        {
            return await _context.OrderStatusHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }
    }
}
