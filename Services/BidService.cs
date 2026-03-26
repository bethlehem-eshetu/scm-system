using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class BidService : IBidService
    {
        private readonly ApplicationDbContext _context;

        public BidService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TenderBid>> GetBidsForTenderAsync(int tenderId)
        {
            return await _context.TenderBids
                .Include(b => b.Supplier)
                .Where(b => b.TenderId == tenderId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TenderBid>> GetBidsBySupplierAsync(int supplierId)
        {
            return await _context.TenderBids
                .Include(b => b.Tender)
                    .ThenInclude(t => t.Category)
                .Where(b => b.SupplierId == supplierId)
                .ToListAsync();
        }

        public async Task<TenderBid> GetBidByIdAsync(int id)
        {
            return await _context.TenderBids
                .Include(b => b.Tender)
                .Include(b => b.Supplier)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<TenderBid> SubmitBidAsync(TenderBid bid)
        {
            bid.SubmittedAt = DateTime.Now;
            bid.Status = "Pending";
            
            _context.TenderBids.Add(bid);
            await _context.SaveChangesAsync();
            return bid;
        }

        public async Task<TenderBid> UpdateBidStatusAsync(int id, string status)
        {
            var bid = await _context.TenderBids.FindAsync(id);
            if (bid != null)
            {
                bid.Status = status;
                await _context.SaveChangesAsync();
            }
            return bid;
        }

        public async Task<TenderBid> AcceptBidAsync(int id)
        {
            var bid = await GetBidByIdAsync(id);
            if (bid == null) return null;

            bid.Status = "Accepted";
            
            // Reject other bids
            var otherBids = await _context.TenderBids
                .Where(b => b.TenderId == bid.TenderId && b.Id != id)
                .ToListAsync();
                
            foreach (var other in otherBids)
            {
                other.Status = "Rejected";
            }

            // Update tender status
            if (bid.Tender != null)
            {
                bid.Tender.Status = "Awarded";
            }

            await _context.SaveChangesAsync();
            return bid;
        }
    }
}
