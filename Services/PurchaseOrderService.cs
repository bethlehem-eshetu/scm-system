using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;

namespace SCM_System.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public PurchaseOrderService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByWarehouseAsync(int warehouseId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Where(po => po.WarehouseId == warehouseId)
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

            // Automated Warehouse Assignment Logic
            var allocations = new Dictionary<int, List<PurchaseOrderItem>>(); // WarehouseId -> Items
            
            foreach (var tenderItem in bid.Tender.TenderItems)
            {
                int remainingToAllocate = tenderItem.Quantity;
                
                // Find warehouses with stock for this ProductId belonging to the Winning Supplier
                var inventories = await _context.Inventories
                    .Where(i => i.ProductId == tenderItem.ProductId && i.Warehouse.SupplierId == bid.SupplierId)
                    .OrderByDescending(i => i.QuantityOnHand - i.QuantityReserved)
                    .ToListAsync();
                
                foreach (var inv in inventories)
                {
                    if (remainingToAllocate <= 0) break;
                    
                    int available = inv.QuantityOnHand - inv.QuantityReserved;
                    if (available <= 0) continue;
                    
                    int allocate = Math.Min(remainingToAllocate, available);
                    
                    if (inv.WarehouseId.HasValue)
                    {
                        if (!allocations.ContainsKey(inv.WarehouseId.Value))
                            allocations[inv.WarehouseId.Value] = new List<PurchaseOrderItem>();
                            
                        allocations[inv.WarehouseId.Value].Add(new PurchaseOrderItem {
                            ProductId = tenderItem.ProductId,
                            ProductName = tenderItem.ProductName,
                            Description = tenderItem.Description,
                            Quantity = allocate,
                            UnitPrice = bid.UnitPrice
                        });
                        
                        // Reserve Stock
                        inv.QuantityReserved += allocate;
                        remainingToAllocate -= allocate;
                    }
                }
                
                // If not enough stock was found, we still allocate to the first warehouse (will show as shortage for manager)
                if (remainingToAllocate > 0)
                {
                    var firstWh = await _context.Warehouses.FirstOrDefaultAsync(w => w.SupplierId == bid.SupplierId);
                    if (firstWh != null)
                    {
                        if (!allocations.ContainsKey(firstWh.Id))
                            allocations[firstWh.Id] = new List<PurchaseOrderItem>();
                            
                        allocations[firstWh.Id].Add(new PurchaseOrderItem {
                            ProductId = tenderItem.ProductId,
                            ProductName = tenderItem.ProductName,
                            Description = tenderItem.Description,
                            Quantity = remainingToAllocate,
                            UnitPrice = bid.UnitPrice
                        });
                    }
                }
            }

            PurchaseOrder firstPo = null;

            foreach (var kvp in allocations)
            {
                var warehouseId = kvp.Key;
                var items = kvp.Value;

                var po = new PurchaseOrder
                {
                    PONumber = "PO-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    RetailerId = bid.Tender.RetailerId,
                    SupplierId = bid.SupplierId,
                    WarehouseId = warehouseId,
                    TenderBidId = bid.Id,
                    TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
                    Status = POStatus.Issued,
                    DeliveryAddress = deliveryAddress,
                    ExpectedDeliveryDate = DateTime.Now.AddDays(bid.DeliveryLeadTimeDays),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    OrderDate = DateTime.Now,
                    PurchaseOrderItems = items
                };

                _context.PurchaseOrders.Add(po);
                if (firstPo == null) firstPo = po;
            }

            await _context.SaveChangesAsync();
            return firstPo;
        }

        public async Task<PurchaseOrder> CreateDirectPurchaseOrderAsync(PurchaseOrder po, List<PurchaseOrderItem> items)
        {
            po.PONumber = "PO-" + DateTime.Now.Ticks.ToString().Substring(8);
            po.CreatedAt = DateTime.Now;
            po.UpdatedAt = DateTime.Now;
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

        public async Task<PurchaseOrder> UpdatePurchaseOrderStatusAsync(int id, string status, int userId)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po != null)
            {
                var oldStatus = po.Status;
                po.Status = status;
                po.UpdatedAt = DateTime.Now;

                if (status == POStatus.Picked) po.PickedAt = DateTime.Now;
                if (status == POStatus.Packed) po.PackedAt = DateTime.Now;
                if (status == POStatus.Delivered) po.DeliveredAt = DateTime.Now;

                // Sync with parent Order if it exists
                if (po.Order != null)
                {
                    // Add to History
                    var history = new OrderStatusHistory
                    {
                        OrderId = po.OrderId,
                        Status = status,
                        Comments = $"Warehouse Status Update: {po.PONumber} moved from {oldStatus} to {status}.",
                        ChangedByUserId = userId,
                        ChangedAt = DateTime.Now
                    };

                    _context.OrderStatusHistories.Add(history);

                    // If all POs are Delivered, mark Order as Delivered (if applicable)
                    // If all POs are Delivered, mark Order as Delivered (if applicable)
                    if (status == POStatus.Delivered)
                    {
                        var allOtherPos = await _context.PurchaseOrders
                            .Where(p => p.OrderId == po.OrderId && p.Id != po.Id)
                            .AllAsync(p => p.Status == POStatus.Delivered);

                        if (allOtherPos)
                        {
                            po.Order.OrderStatus = POStatus.Delivered;
                        }
                        else
                        {
                            po.Order.OrderStatus = "Partially Delivered";
                        }
                    }
                    else if (status == POStatus.Picked || status == POStatus.Packed || status == POStatus.Ready || status == POStatus.InTransit)
                    {
                        if (po.Order.OrderStatus == POStatus.Accepted) 
                        {
                             po.Order.OrderStatus = POStatus.Processing; 
                        }
                    }
                }

                // Notify Retailer on specific milestones
                if (po.Retailer?.UserId != null)
                {
                    string title = "";
                    string msg = "";
                    string type = "Info";

                    if (status == POStatus.Packed)
                    {
                        title = "Order Packed 📦";
                        msg = $"Items from your order #{po.Order?.OrderNumber} have been packed at {po.Warehouse?.Name}.";
                    }
                    else if (status == POStatus.InTransit)
                    {
                        title = "Order Shipped 🚚";
                        msg = $"A shipment for your order #{po.Order?.OrderNumber} is now in transit.";
                        type = "Success";
                    }
                    else if (status == POStatus.Delivered)
                    {
                        title = "Order Delivered ✅";
                        msg = $"Your shipment from order #{po.Order?.OrderNumber} has been delivered.";
                        type = "Success";
                    }

                    if (!string.IsNullOrEmpty(title))
                    {
                        await _notificationService.SendNotificationAsync(
                            po.Retailer.UserId,
                            title,
                            msg,
                            type,
                            $"/Retailer/OrderTrackingDetails/{po.OrderId}"
                        );
                    }
                }

                // Handle Inventory Deduction on Delivery
                if (status == POStatus.Delivered && oldStatus != POStatus.Delivered)
                {
                    // Refined logic: Loop through all items in the PO
                    foreach (var item in po.PurchaseOrderItems)
                    {
                        var itemInv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == po.WarehouseId);
                        
                        if (itemInv == null)
                        {
                            throw new InvalidOperationException($"Inventory record missing for Product ID {item.ProductId} in Warehouse {po.WarehouseId}. Cannot complete delivery.");
                        }

                        if (itemInv.QuantityReserved < item.Quantity)
                        {
                            throw new InvalidOperationException($"Insufficient reserved stock for Product ID {item.ProductId}. Reserved: {itemInv.QuantityReserved}, Required: {item.Quantity}");
                        }

                        itemInv.QuantityReserved -= item.Quantity;
                        itemInv.QuantityOnHand -= item.Quantity;
                        
                        if (itemInv.QuantityOnHand < 0) itemInv.QuantityOnHand = 0; // Safety
                        
                        _context.Update(itemInv);
                    }
                }

                await _context.SaveChangesAsync();
            }
            return po;
        }
    }
}
