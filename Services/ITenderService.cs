using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface ITenderService
    {
        Task<IEnumerable<Tender>> GetAllTendersAsync();
        Task<IEnumerable<Tender>> GetTendersByRetailerAsync(int retailerId);
        Task<Tender> GetTenderByIdAsync(int id);
        Task<Tender> CreateTenderAsync(Tender tender, List<TenderItem> items);
        Task<Tender> UpdateTenderStatusAsync(int id, string status);
        Task DeleteTenderAsync(int id);
    }
}
