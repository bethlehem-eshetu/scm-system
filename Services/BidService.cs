using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;

namespace SCM_System.Services
{
    public class BidService : IBidService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPurchaseOrderService _poService;

        public BidService(ApplicationDbContext context, IPurchaseOrderService poService)
        {
            _context = context;
            _poService = poService;
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
            
            // Recalculate scores for all bids in this tender (relative scoring)
            var allBids = await _context.TenderBids
                .Where(b => b.TenderId == bid.TenderId)
                .ToListAsync();
            
            foreach (var b in allBids)
            {
                b.Score = await CalculateBidScoreAsync(b);
            }
            
            await _context.SaveChangesAsync();
            return bid;
        }

        private async Task<decimal> CalculateBidScoreAsync(TenderBid bid)
        {
            var tender = await _context.Tenders.FindAsync(bid.TenderId);
            if (tender == null) return 0;

            // 1. Price Score (Standard: Lowest / Current)
            var lowestPrice = await _context.TenderBids
                .Where(b => b.TenderId == bid.TenderId)
                .Select(b => (decimal?)b.ProposedTotalAmount)
                .MinAsync() ?? bid.ProposedTotalAmount;
            
            decimal priceScore = (lowestPrice / bid.ProposedTotalAmount) * 100;

            // 2. Technical Score (Refined for new fields)
            // Max Points: 100
            decimal technicalPoints = 0;
            if (!string.IsNullOrEmpty(bid.TechnicalProposal)) technicalPoints += 30;
            if (!string.IsNullOrEmpty(bid.WarrantyPeriod)) technicalPoints += 20;
            if (!string.IsNullOrEmpty(bid.PreviousExperience)) technicalPoints += 20;
            if (bid.InspectionCompliance == "Accept") technicalPoints += 20;
            if (!string.IsNullOrEmpty(bid.PackagingPlan)) technicalPoints += 10;
            
            decimal technicalScore = technicalPoints;

            // 3. Delivery Score (Relative Lead Time)
            var lowestLeadTime = await _context.TenderBids
                .Where(b => b.TenderId == bid.TenderId)
                .Select(b => (int?)b.DeliveryLeadTimeDays)
                .MinAsync() ?? bid.DeliveryLeadTimeDays;
            
            decimal deliveryScore = (decimal)lowestLeadTime / (decimal)bid.DeliveryLeadTimeDays * 100;

            // Weighted Average using Tender weights
            decimal finalScore = (priceScore * (tender.PriceWeight / 100.0m)) + 
                                 (technicalScore * (tender.TechnicalWeight / 100.0m)) + 
                                 (deliveryScore * (tender.DeliveryWeight / 100.0m));

            return Math.Round(finalScore, 2);
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

        public async Task<BidFeedbackViewModel> GetBidFeedbackAsync(int bidId)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Tender)
                .FirstOrDefaultAsync(b => b.Id == bidId);

            if (bid == null) return null;

            var allBids = await _context.TenderBids
                .Where(b => b.TenderId == bid.TenderId)
                .OrderByDescending(b => b.Score)
                .ToListAsync();

            var rank = allBids.FindIndex(b => b.Id == bidId) + 1;
            var winningBid = allBids.First();
            var winningScore = winningBid.Score;

            var suggestions = new List<string>();

            // Price Suggestions
            if (bid.ProposedTotalAmount > winningBid.ProposedTotalAmount)
            {
                var priceDiff = ((bid.ProposedTotalAmount - winningBid.ProposedTotalAmount) / winningBid.ProposedTotalAmount) * 100;
                if (priceDiff > 5)
                {
                    suggestions.Add($"Lower your price by approximately {priceDiff:F0}% to be more competitive with the top proposal.");
                }
            }

            // Technical Suggestions
            if (string.IsNullOrEmpty(bid.TechnicalProposal))
            {
                suggestions.Add("Provide a detailed technical proposal to improve your technical score.");
            }
            if (string.IsNullOrEmpty(bid.WarrantyPeriod) || bid.WarrantyPeriod == "No Warranty")
            {
                suggestions.Add("Offering a comprehensive warranty period can significantly boost your technical compliance score.");
            }

            // Delivery Suggestions
            if (bid.DeliveryLeadTimeDays > winningBid.DeliveryLeadTimeDays + 3)
            {
                suggestions.Add($"Your lead time is {bid.DeliveryLeadTimeDays - winningBid.DeliveryLeadTimeDays} days longer than the winning bid. Reducing lead time improves logistics scoring.");
            }

            // Calculate current breakdown (approximate based on logic in CalculateBidScoreAsync)
            var lowestPrice = allBids.Min(b => b.ProposedTotalAmount);
            var priceScore = (lowestPrice / bid.ProposedTotalAmount) * 100;

            decimal technicalPoints = 0;
            if (!string.IsNullOrEmpty(bid.TechnicalProposal)) technicalPoints += 30;
            if (!string.IsNullOrEmpty(bid.WarrantyPeriod)) technicalPoints += 20;
            if (!string.IsNullOrEmpty(bid.PreviousExperience)) technicalPoints += 20;
            if (bid.InspectionCompliance == "Accept") technicalPoints += 20;
            if (!string.IsNullOrEmpty(bid.PackagingPlan)) technicalPoints += 10;

            var lowestLeadTime = allBids.Min(b => b.DeliveryLeadTimeDays);
            var deliveryScore = (decimal)lowestLeadTime / (decimal)bid.DeliveryLeadTimeDays * 100;

            return new BidFeedbackViewModel
            {
                Score = bid.Score,
                Rank = rank,
                TotalBids = allBids.Count,
                WinningScore = winningScore,
                Improvements = suggestions,
                PriceScore = Math.Round(priceScore, 1),
                TechnicalScore = Math.Round(technicalPoints, 1),
                DeliveryScore = Math.Round(deliveryScore, 1),
                TenderTitle = bid.Tender?.Title,
                ReferenceNumber = bid.Tender?.ReferenceNumber
            };
        }

        public async Task<PurchaseOrder> AcceptBidAsync(int id, string deliveryAddress)
        {
            var bid = await GetBidByIdAsync(id);
            if (bid == null) return null;

            // Update Bid Status
            bid.Status = "Accepted";
            bid.IsWinningBid = true;
            
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

            // Generate Purchase Order(s)
            return await _poService.GeneratePurchaseOrderFromBidAsync(id, deliveryAddress);
        }
    }
}
