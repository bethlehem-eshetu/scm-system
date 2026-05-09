using SCM_System.Models.ViewModels;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface ISupplierService
    {
        Task<SupplierDashboardViewModel> GetDashboardAnalyticsAsync(int supplierId);
        Task<SupplierReportsViewModel> GetSupplierReportsAsync(int supplierId);
        Task<IEnumerable<Order>> GetSupplierOrdersForTrackingAsync(int supplierId);
        Task<IEnumerable<Commission>> GetSupplierCommissionsAsync(int supplierId);
        Task<Commission> GetCommissionByIdAsync(int commissionId);
        Task<bool> UpdateCommissionPaymentStatusAsync(int commissionId, string chapaId, string status, string verificationData);
        Task<List<VehicleSuggestion>> GetSmartDispatchSuggestionsAsync(int warehouseId);
        Task<List<DriverSuggestion>> GetSmartDriverSuggestionsAsync(int warehouseId);
        Task<bool> UpdateSupplierTierAsync(int supplierId);
    }
}
