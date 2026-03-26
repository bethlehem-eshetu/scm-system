using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByRetailerAsync(int retailerId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Where(po => po.RetailerId == retailerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(int supplierId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Where(po => po.SupplierId == supplierId)
                .ToListAsync();
        }

        public async Task<PurchaseOrder> GetPurchaseOrderByIdAsync(int id)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(po => po.TenderBid)
                    .ThenInclude(tb => tb.Tender)
                .FirstOrDefaultAsync(po => po.Id == id);
        }

        public async Task<PurchaseOrder> GeneratePurchaseOrderFromBidAsync(int tenderBidId, string deliveryAddress)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Tender)
                    .ThenInclude(t => t.TenderItems)
                .FirstOrDefaultAsync(b => b.Id == tenderBidId);

            if (bid == null || bid.Status != "Accepted") return null;

            var po = new PurchaseOrder
            {
                PONumber = "PO-" + DateTime.Now.Ticks.ToString().Substring(8),
                RetailerId = bid.Tender.RetailerId,
                SupplierId = bid.SupplierId,
                TenderBidId = bid.Id,
                TotalAmount = bid.ProposedTotalAmount,
                Status = "Pending",
                DeliveryAddress = deliveryAddress,
                ExpectedDeliveryDate = DateTime.Now.AddDays(bid.DeliveryLeadTimeDays),
                CreatedAt = DateTime.Now,
                OrderDate = DateTime.Now
            };

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            // Just reference products by name if actual Products don't exist, 
            // but for system integrity, products should exist. Assuming Tender items link loosely.
            // For simplicity in Module 3 rebuilding, we will not auto-generate items from bids unless needed.
            return po;
        }

        public async Task<PurchaseOrder> CreateDirectPurchaseOrderAsync(PurchaseOrder po, List<PurchaseOrderItem> items)
        {
            po.PONumber = "PO-" + DateTime.Now.Ticks.ToString().Substring(8);
            po.CreatedAt = DateTime.Now;
            po.OrderDate = DateTime.Now;
            po.Status = "Pending";
            
            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            foreach(var item in items)
            {
                item.PurchaseOrderId = po.Id;
                _context.PurchaseOrderItems.Add(item);
            }
            await _context.SaveChangesAsync();

            return po;
        }

        public async Task<PurchaseOrder> UpdatePurchaseOrderStatusAsync(int id, string status)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po != null)
            {
                po.Status = status;
                await _context.SaveChangesAsync();
            }
            return po;
        }
    }
}
