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
                .Include(t => t.Bids)
                .Where(t => t.RetailerId == retailerId)
                .OrderByDescending(t => t.CreatedAt)
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
            tender.UpdatedAt = DateTime.Now;
            tender.Status = "Published"; // Default to Published as per new requirements
            
            _context.Tenders.Add(tender);
            await _context.SaveChangesAsync();

            if (items != null)
            {
                foreach (var item in items)
                {
                    item.TenderId = tender.Id;
                    _context.TenderItems.Add(item);
                }
                await _context.SaveChangesAsync();
            }

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

        public async Task<bool> AwardTenderAsync(int tenderId, int bidId)
        {
            var tender = await _context.Tenders
                .Include(t => t.Bids)
                .Include(t => t.TenderItems)
                .FirstOrDefaultAsync(t => t.Id == tenderId);

            if (tender == null || tender.Status == "Awarded") return false;

            var winningBid = await _context.TenderBids.FindAsync(bidId);
            if (winningBid == null || winningBid.TenderId != tenderId) return false;

            // Mark winning bid
            winningBid.IsWinningBid = true;
            winningBid.Status = "Accepted";

            // Mark other bids as rejected
            foreach (var bid in tender.Bids.Where(b => b.Id != bidId))
            {
                bid.Status = "Rejected";
            }

            tender.Status = "Awarded";

            // Auto-create Order
            var order = new Order
            {
                OrderNumber = "ORD-" + DateTime.Now.Ticks.ToString().Substring(8),
                DeliveryAddress = tender.DeliveryLocation ?? "Tender default location",
                ExpectedDeliveryDate = DateTime.Now.AddDays(winningBid.DeliveryLeadTimeDays),
                SupplierId = winningBid.SupplierId,
                RetailerId = tender.RetailerId,
                TotalAmount = winningBid.ProposedTotalAmount,
                OrderStatus = "Issued",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                OrderItems = tender.TenderItems.Select(ti => new OrderItem
                {
                    ProductId = ti.ProductId,
                    ProductName = ti.ProductName ?? "Custom Product",
                    Quantity = ti.Quantity,
                    UnitPrice = tender.TenderItems.Sum(t => t.Quantity) > 0 ? winningBid.ProposedTotalAmount / tender.TenderItems.Sum(t => t.Quantity) : winningBid.ProposedTotalAmount
                }).ToList()
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Auto-create PO
            var po = new PurchaseOrder
            {
                PONumber = "PO-TEND-" + DateTime.Now.Ticks.ToString().Substring(8),
                RetailerId = tender.RetailerId,
                SupplierId = winningBid.SupplierId,
                TenderBidId = winningBid.Id,
                Subtotal = winningBid.ProposedTotalAmount,
                TotalAmount = winningBid.ProposedTotalAmount,
                Status = "Issued",
                DeliveryAddress = tender.DeliveryLocation ?? "Tender default location",
                WarehouseId = null,
                ExpectedDeliveryDate = DateTime.Now.AddDays(winningBid.DeliveryLeadTimeDays),
                OrderDate = DateTime.Now,
                OrderId = order.Id,
                PurchaseOrderItems = tender.TenderItems.Select(ti => new PurchaseOrderItem
                {
                    ProductId = ti.ProductId,
                    ProductName = ti.ProductName ?? "Custom Product",
                    Quantity = ti.Quantity,
                    UnitPrice = tender.TenderItems.Sum(t => t.Quantity) > 0 ? winningBid.ProposedTotalAmount / tender.TenderItems.Sum(t => t.Quantity) : winningBid.ProposedTotalAmount
                }).ToList()
            };
            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<IEnumerable<Tender>> GetTendersByCategoryAsync(int categoryId)
        {
            // Get the category to check its level
            var category = await _context.ProductCategories.FindAsync(categoryId);
            if (category == null) return Enumerable.Empty<Tender>();

            IQueryable<Tender> query = _context.Tenders
                .Include(t => t.Retailer)
                .Include(t => t.Category)
                .Where(t => t.Status == "Published");

            if (category.Level == 1)
            {
                // If it's a parent category, include all tenders from children categories too
                var childCategoryIds = await _context.ProductCategories
                    .Where(c => c.ParentCategoryId == categoryId)
                    .Select(c => c.Id)
                    .ToListAsync();

                query = query.Where(t => t.CategoryId == categoryId || childCategoryIds.Contains(t.CategoryId));
            }
            else
            {
                // If it's a subcategory, only include exact matches
                query = query.Where(t => t.CategoryId == categoryId);
            }

            return await query.ToListAsync();
        }
    }
}
