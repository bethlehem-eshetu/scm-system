using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IReturnService
    {
        Task<ReturnRequest> CreateReturnRequestAsync(int purchaseOrderId, string reason, string? description, decimal refundAmount, string? images = null);
        Task<ReturnRequest> GetReturnRequestByIdAsync(int id);
        Task<List<ReturnRequest>> GetRetailerReturnsAsync(int retailerId);
        Task<List<ReturnRequest>> GetSupplierReturnsAsync(int supplierId);
        Task<List<ReturnRequest>> GetPendingReturnsAsync();
        Task<ReturnRequest> ApproveReturnAsync(int returnId, string adminNotes);
        Task<ReturnRequest> RejectReturnAsync(int returnId, string rejectionReason);
        Task<ReturnRequest> MarkAsShippedAsync(int returnId, string trackingNumber);
        Task<ReturnRequest> MarkAsReceivedAsync(int returnId);
        Task<ReturnRequest> ProcessRefundAsync(int returnId);
        Task<int> GetPendingCountAsync();
    }
}