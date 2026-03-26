using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class TenderService : ITenderService
    {
        private readonly ApplicationDbContext _context;

        public TenderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Tender>> GetAllTendersAsync()
        {
            return await _context.Tenders
                .Include(t => t.Retailer)
                .Include(t => t.Category)
                .Include(t => t.TenderItems)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tender>> GetTendersByRetailerAsync(int retailerId)
        {
            return await _context.Tenders
                .Include(t => t.Category)
                .Include(t => t.TenderItems)
                .Where(t => t.RetailerId == retailerId)
                .ToListAsync();
        }

        public async Task<Tender> GetTenderByIdAsync(int id)
        {
            return await _context.Tenders
                .Include(t => t.Retailer)
                .Include(t => t.Category)
                .Include(t => t.TenderItems)
                .Include(t => t.Bids)
                    .ThenInclude(b => b.Supplier)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tender> CreateTenderAsync(Tender tender, List<TenderItem> items)
        {
            tender.CreatedAt = DateTime.Now;
            tender.Status = "Open";
            
            _context.Tenders.Add(tender);
            await _context.SaveChangesAsync();

            foreach (var item in items)
            {
                item.TenderId = tender.Id;
                _context.TenderItems.Add(item);
            }
            await _context.SaveChangesAsync();

            return tender;
        }

        public async Task<Tender> UpdateTenderStatusAsync(int id, string status)
        {
            var tender = await _context.Tenders.FindAsync(id);
            if (tender != null)
            {
                tender.Status = status;
                await _context.SaveChangesAsync();
            }
            return tender;
        }

        public async Task DeleteTenderAsync(int id)
        {
            var tender = await _context.Tenders.FindAsync(id);
            if (tender != null)
            {
                _context.Tenders.Remove(tender);
                await _context.SaveChangesAsync();
            }
        }
    }
}
