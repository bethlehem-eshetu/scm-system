using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetOrdersByRetailerAsync(int retailerId);
        Task<IEnumerable<Order>> GetOrdersBySupplierAsync(int supplierId);
        Task<Order> GetOrderByIdAsync(int id);
        Task<Order> CreateOrderFromPurchaseOrderAsync(int purchaseOrderId);
        Task<Order> UpdateOrderStatusAsync(int orderId, string status, string comments, int changedByUserId);
        Task<IEnumerable<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId);
        Task<bool> AcceptOrderAsync(int orderId, int? warehouseId = null);
        Task<bool> RejectOrderAsync(int orderId, string reason);
        Task<int> CreateMissingCommissionsForDeliveredOrders();
    }
}
