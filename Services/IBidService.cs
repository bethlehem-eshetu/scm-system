using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IBidService
    {
        Task<IEnumerable<TenderBid>> GetBidsForTenderAsync(int tenderId);
        Task<IEnumerable<TenderBid>> GetBidsBySupplierAsync(int supplierId);
        Task<TenderBid> GetBidByIdAsync(int id);
        Task<TenderBid> SubmitBidAsync(TenderBid bid);
        Task<TenderBid> UpdateBidStatusAsync(int id, string status);
        Task<PurchaseOrder> AcceptBidAsync(int id, string deliveryAddress);
    }
}
