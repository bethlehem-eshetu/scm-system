using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IPurchaseOrderService
    {
        Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByRetailerAsync(int retailerId);
        Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(int supplierId);
        Task<PurchaseOrder> GetPurchaseOrderByIdAsync(int id);
        Task<PurchaseOrder> GeneratePurchaseOrderFromBidAsync(int tenderBidId, string deliveryAddress);
        Task<PurchaseOrder> CreateDirectPurchaseOrderAsync(PurchaseOrder po, List<PurchaseOrderItem> items);
        Task<PurchaseOrder> UpdatePurchaseOrderStatusAsync(int id, string status);
    }
}
