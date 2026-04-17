using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface ICommissionService
    {
        Task<Commission> CreateCommissionAsync(int orderId, decimal orderAmount, int purchaseOrderId);
        Task<Commission> GetCommissionByIdAsync(int id);
        Task<List<Commission>> GetSupplierCommissionsAsync(int supplierId);
        Task<List<Commission>> GetPendingCommissionsAsync();
        Task<Commission> ProcessPaymentAsync(int commissionId, string paymentUrl);
        Task<Commission> VerifyPaymentAsync(int commissionId);
        Task<decimal> GetTotalCommissionsEarnedAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<decimal> GetPendingCommissionsTotalAsync();
    }
}