using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;
using SCM_System.Models.Enums;

namespace SCM_System.Services
{
    public class PurchaseOrderService(
        ApplicationDbContext context, 
        INotificationService notificationService, 
        IInventoryService inventoryService,
        ICommissionService commissionService,
        ILogger<PurchaseOrderService> logger) : IPurchaseOrderService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IInventoryService _inventoryService = inventoryService;
        private readonly ICommissionService _commissionService = commissionService;
        private readonly ILogger _logger = logger;

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByRetailerAsync(int retailerId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Where(po => po.RetailerId == retailerId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(int supplierId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Include(po => po.Warehouse)
                .Where(po => po.SupplierId == supplierId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByWarehouseAsync(int warehouseId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Where(po => po.WarehouseId == warehouseId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PurchaseOrder> GetPurchaseOrderByIdAsync(int id)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(po => po.TenderBid)
                    .ThenInclude(tb => tb.Tender)
                .Include(po => po.Order)
                    .ThenInclude(o => o.StatusHistory)
                        .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(po => po.Id == id);
        }

        public async Task<PurchaseOrder> GetPurchaseOrderByNumberAsync(string poNumber)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Retailer)
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(po => po.TenderBid)
                    .ThenInclude(tb => tb.Tender)
                .Include(po => po.Order)
                    .ThenInclude(o => o.StatusHistory)
                        .ThenInclude(h => h.ChangedByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(po => po.PONumber == poNumber);
        }

        public async Task<PurchaseOrder> GeneratePurchaseOrderFromBidAsync(int tenderBidId, string deliveryAddress)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Tender)
                    .ThenInclude(t => t.TenderItems)
                        .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(b => b.Id == tenderBidId);

            if (bid == null || bid.Status != POStatus.Accepted) return null;

            // 1. Create a "Shadow Order" to represent the tender award in the commercial layer
            var order = new Order
            {
                OrderNumber = "ORD-TEND-" + DateTime.Now.Ticks.ToString().Substring(12),
                RetailerId = bid.Tender.RetailerId,
                SupplierId = bid.SupplierId,
                TotalAmount = bid.ProposedTotalAmount,
                OrderStatus = POStatus.Issued,
                PaymentStatus = "Pending",
                DeliveryAddress = deliveryAddress,
                CreatedAt = DateTime.Now
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var items = bid.Tender.TenderItems.Select(ti => new PurchaseOrderItem {
                ProductId = ti.ProductId,
                ProductName = ti.ProductName,
                Description = ti.Description,
                Quantity = ti.Quantity,
                UnitPrice = bid.UnitPrice
            }).ToList();

            decimal subtotal = items.Sum(i => i.Quantity * i.UnitPrice);
            decimal vat = subtotal * 0.15m; // Standard VAT

            var po = new PurchaseOrder
            {
                PONumber = "PO-TEND-" + DateTime.Now.Ticks.ToString().Substring(12),
                RetailerId = bid.Tender.RetailerId,
                SupplierId = bid.SupplierId,
                WarehouseId = null, // 🔥 KEEP UNASSIGNED
                TenderBidId = bid.Id,
                OrderId = order.Id,
                Subtotal = subtotal,
                VAT = vat,
                TotalAmount = subtotal + vat,
                Status = POStatus.Issued,
                DeliveryAddress = deliveryAddress,
                ExpectedDeliveryDate = DateTime.Now.AddDays(bid.DeliveryLeadTimeDays),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                OrderDate = DateTime.Now,
                PurchaseOrderItems = items
            };

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            return po;
        }

        public async Task<PurchaseOrder> CreateDirectPurchaseOrderAsync(PurchaseOrder po, List<PurchaseOrderItem> items)
        {
            po.PONumber = "PO-" + DateTime.Now.Ticks.ToString().Substring(12);
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var po = await _context.PurchaseOrders
                    .Include(p => p.Order)
                    .Include(p => p.PurchaseOrderItems)
                    .Include(p => p.Retailer)
                    .Include(p => p.Warehouse)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (po == null) return null;

                var oldStatus = po.Status;
                
                // 🛑 IMPOSSIBLE STATUS GUARDRAILS
                if (status == POStatus.Accepted && (!po.WarehouseId.HasValue || po.WarehouseId == 0))
                    throw new InvalidOperationException("Selection of a warehouse is required to accept this Purchase Order.");

                if (status == POStatus.Picking && oldStatus != POStatus.Accepted)
                    throw new InvalidOperationException("PO must be 'Accepted' before picking can begin.");

                if (status == POStatus.Picked && oldStatus != POStatus.Picking && oldStatus != POStatus.Accepted)
                    throw new InvalidOperationException("PO must be in 'Picking' or 'Accepted' status before being marked as 'Picked'.");

                if (status == POStatus.Packing && oldStatus != POStatus.Picked)
                    throw new InvalidOperationException("PO must be 'Picked' before packing can begin.");

                if (status == POStatus.Packed && oldStatus != POStatus.Packing && oldStatus != POStatus.Picked)
                    throw new InvalidOperationException("PO must be in 'Packing' or 'Picked' status before being marked as 'Packed'.");

                if (status == POStatus.Ready && oldStatus != POStatus.Packed)
                    throw new InvalidOperationException("PO must be 'Packed' before being 'Ready Dispatch'.");

                if (status == POStatus.InTransit)
                {
                    if (oldStatus != POStatus.Ready && oldStatus != POStatus.Packed)
                        throw new InvalidOperationException("PO must be 'Ready Dispatch' or 'Packed' before it can move to 'In Transit'.");
                    if (!po.DeliveryAgentId.HasValue || !po.VehicleId.HasValue)
                        throw new InvalidOperationException("A Delivery Agent and Vehicle must be assigned before shipping.");
                }

                if (status == POStatus.Delivered && oldStatus != POStatus.InTransit)
                    throw new InvalidOperationException("PO must be 'In Transit' before it can be marked as 'Delivered'.");

                if (status == POStatus.Failed && oldStatus != POStatus.InTransit)
                    throw new InvalidOperationException("PO must be 'In Transit' before it can be marked as 'Failed Delivery'.");

                // 1. If transitioning OUT OF 'Issued' or into 'Accepted', trigger atomic reservation
                var reservedStatuses = new[] { POStatus.Accepted, POStatus.Picking, POStatus.Picked, POStatus.Packing, POStatus.Packed, POStatus.Ready, POStatus.InTransit, POStatus.Failed };
                if (oldStatus == POStatus.Issued && reservedStatuses.Contains(status))
                {
                    var success = await _inventoryService.BulkReserveStockForPOAsync(id, po.SupplierId, po.WarehouseId);
                    if (!success) throw new InvalidOperationException("Failed to reserve stock. Items may be out of stock or warehouse at capacity.");
                }

                // 🔥 Auto-Complete Guard
                if (status == POStatus.Delivered && po.Order != null && po.Order.PaymentStatus == PaymentStatus.Paid.ToString())
                {
                    status = POStatus.Completed;
                }

                po.Status = status;
                po.UpdatedAt = DateTime.Now;

                if (status == POStatus.Picking) po.PickedAt = null; // Reset if re-picking? Usually just set dates
                if (status == POStatus.Picked) po.PickedAt = DateTime.Now;
                if (status == POStatus.Packed) po.PackedAt = DateTime.Now;
                if (status == POStatus.Delivered) po.DeliveredAt = DateTime.Now;

                // 🆕 Auto-Accept logic
                if (status == POStatus.Accepted)
                {
                    var manager = await _context.SupplierEmployees
                        .FirstOrDefaultAsync(e => e.WarehouseId == po.WarehouseId && (e.EmployeeRole == "WarehouseManager" || e.EmployeeRole == "warehouse_manager"));
                    
                    if (manager != null && manager.AutoAcceptPickTasks)
                    {
                        po.Status = POStatus.Picking; // Changed from Processing to Picking
                        po.UpdatedAt = DateTime.Now;
                    }
                }

                // Sync with parent Order
                if (po.Order != null)
                {
                    var history = new OrderStatusHistory
                    {
                        OrderId = po.OrderId,
                        Status = status,
                        Comments = $"ERP Workflow: {po.PONumber} moved from {oldStatus} to {status}.",
                        ChangedByUserId = userId,
                        ChangedAt = DateTime.Now
                    };
                    _context.OrderStatusHistories.Add(history);

                    if (status == POStatus.Delivered || status == POStatus.Completed)
                    {
                        var allOtherPos = await _context.PurchaseOrders
                            .Where(p => p.OrderId == po.OrderId && p.Id != po.Id)
                            .AsNoTracking()
                            .AllAsync(p => p.Status == POStatus.Delivered || p.Status == POStatus.Completed);

                        if (allOtherPos) {
                            po.Order.OrderStatus = status;
                            // Trigger Payment Initiation ONLY when ALL POs are delivered
                            await _commissionService.InitiateOrderPaymentAsync(po.OrderId);
                        }
                        else po.Order.OrderStatus = "Partially Delivered";
                    }
                    else if (status == POStatus.Picking || status == POStatus.Picked || status == POStatus.Packing || status == POStatus.Packed || status == POStatus.Ready || status == POStatus.InTransit)
                    {
                        if (po.Order.OrderStatus == POStatus.Accepted || po.Order.OrderStatus == "Paid") 
                            po.Order.OrderStatus = "Processing"; // Keep Order status broader
                    }
                }

                // 2. Handle Stock Deduction Fallback
                var shippedStatuses = new[] { POStatus.Picked, POStatus.Packing, POStatus.Packed, POStatus.Ready, POStatus.InTransit, POStatus.Delivered, POStatus.Completed };
                if (shippedStatuses.Contains(status))
                {
                    await _inventoryService.DeductStockOnPickAsync(id);
                }

                await _context.SaveChangesAsync();

                // 2. Reload full entity to avoid detached/stale navigation properties during Notification processing
                po = await _context.PurchaseOrders
                    .Include(p => p.Retailer)
                    .Include(p => p.Warehouse)
                    .Include(p => p.Order)
                    .Include(p => p.PurchaseOrderItems)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(p => p.Id == id);

                // 3. Post-transaction notifications
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
                            $"/Order/Details/{po.OrderId}"
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
                            // Safety guard: if reservation was already cleaned up but Hand is still there, adjust Hand only
                            // But usually we expect reserved stock to be there if we follow the reservation flow.
                            _logger.LogWarning("Insufficient reserved stock for Product ID {ProductId} during delivery. Adjusting Hand only.", item.ProductId);
                        }
                        else 
                        {
                            itemInv.QuantityReserved -= item.Quantity;
                        }

                        itemInv.QuantityOnHand -= item.Quantity;
                        
                        if (itemInv.QuantityOnHand < 0) itemInv.QuantityOnHand = 0; // Safety
                        
                        _context.Update(itemInv);

                        // 🆕 Low Stock Notification Logic
                        var manager = await _context.SupplierEmployees
                            .FirstOrDefaultAsync(e => e.WarehouseId == po.WarehouseId && (e.EmployeeRole == "WarehouseManager" || e.EmployeeRole == "warehouse_manager"));

                        if (manager != null && manager.NotifyLowStock && itemInv.QuantityOnHand <= manager.LowStockThreshold)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                UserId = manager.UserId,
                                Title = "Low Stock Alert ⚠️",
                                Message = $"Product {item.Product?.ProductName ?? "Unknown"} is below threshold ({itemInv.QuantityOnHand} remaining in {po.Warehouse?.Name}).",
                                Type = "Warning",
                                ActionUrl = "/Warehouse/Alerts",
                                CreatedAt = DateTime.Now,
                                IsRead = false
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();


                await transaction.CommitAsync();

                return po;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to update Purchase Order {Id} to status {Status}", id, status);
                throw; 
            }
        }
    }
}
