using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class OrderService : IOrderService
    {
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;
        private readonly IPurchaseOrderService _poService;
        private readonly ICommissionService _commissionService;

        public OrderService(ApplicationDbContext context, INotificationService notificationService, IPurchaseOrderService poService, ICommissionService commissionService)
        {
            _context = context;
            _notificationService = notificationService;
            _poService = poService;
            _commissionService = commissionService;
        }

        public async Task<IEnumerable<Order>> GetOrdersByRetailerAsync(int retailerId)
        {
            return await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.OrderItems)
                .Where(o => o.RetailerId == retailerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersBySupplierAsync(int supplierId)
        {
            return await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.OrderItems)
                .Where(o => o.SupplierId == supplierId)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Supplier)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(po => po.Warehouse)
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(po => po.PurchaseOrderItems)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.StatusHistory)
                .Include(o => o.Delivery)
                .FirstOrDefaultAsync(o => o.Id == id);
        }



        public async Task<Order> CreateOrderFromPurchaseOrderAsync(int purchaseOrderId)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

            if (po == null || po.Status != "Accepted") return null;

            var order = new Order
            {
                OrderNumber = "ORD-" + DateTime.Now.Ticks.ToString().Substring(8),
                SupplierId = po.SupplierId,
                RetailerId = po.RetailerId,
                TotalAmount = po.TotalAmount,
                OrderStatus = "Processing",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.Now,
                QRCodeValue = "" // Temporary, will update after saving
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save to get Order.Id

            // Now generate QR code with the actual Order ID
            order.QRCodeValue = GenerateOrderQRCode(order.Id);
            _context.Orders.Update(order);

            po.OrderId = order.Id;
            _context.PurchaseOrders.Update(po);

            foreach (var item in po.PurchaseOrderItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                _context.OrderItems.Add(orderItem);
            }

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "Processing",
                Comments = "Order generated from Purchase Order",
                ChangedByUserId = po.Supplier.UserId,
                ChangedAt = DateTime.Now
            };
            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, string status, string comments, int changedByUserId)
        {
            var order = await _context.Orders
                .Include(o => o.PurchaseOrders)
                    .ThenInclude(po => po.PurchaseOrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new Exception("Order not found");

            if ((status == "Delivered" || status == "Completed") && order.OrderStatus != "Delivered" && order.OrderStatus != "Completed")
            {
                var pos = await _context.PurchaseOrders
                    .Where(p => p.OrderId == order.Id && p.Status != "Cancelled" && p.Status != "Delivered" && p.Status != "Completed")
                    .ToListAsync();
                    
                foreach (var po in pos)
                {
                    await _poService.UpdatePurchaseOrderStatusAsync(po.Id, status, changedByUserId);
                }
            }

            var previousStatus = order.OrderStatus;
            order.OrderStatus = status;

            // ================================
            // 🔥 COMMISSION CREATION FIX (MAIN FIX)
            // ================================
            if (status == "Delivered" || status == "Completed" || status == "Partially Delivered")
            {
                foreach (var po in order.PurchaseOrders)
                {
                    // ✅ ORDER PAYMENT (Retailer → Supplier)
                    var existingOrderPayment = await _context.Commissions
                        .FirstOrDefaultAsync(c =>
                            c.PurchaseOrderId == po.Id &&
                            c.PaymentType == "OrderPayment");

                    if (existingOrderPayment == null)
                    {
                        // ORDER PAYMENT (Retailer → Supplier)
                        var orderPayment = new Commission
                        {
                            PurchaseOrderId = po.Id,
                            OrderId = order.Id,
                            SupplierId = po.SupplierId,
                            RetailerId = order.RetailerId,
                            OrderAmount = po.TotalAmount,
                            CommissionRate = 1.00m,
                            CommissionAmount = po.TotalAmount,
                            PaymentType = "OrderPayment",
                            Status = "Pending",
                            PaymentRequestData = "",           // ✅ REQUIRED - MUST SET
                            PaymentVerificationData = "",      // ✅ REQUIRED - MUST SET
                            CreatedAt = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(7),
                            Notes = $"Order payment for Purchase Order #{po.PONumber}"
                        };

                        _context.Commissions.Add(orderPayment);
                    }

                    // ✅ PLATFORM COMMISSION (Supplier → Platform)
                    var existingPlatform = await _context.Commissions
                        .FirstOrDefaultAsync(c =>
                            c.PurchaseOrderId == po.Id &&
                            c.PaymentType == "PlatformCommission");

                    if (existingPlatform == null)
                    {
                        var commissionRate = 0.05m;

                        // PLATFORM COMMISSION (Supplier → Platform)
                        var platformCommission = new Commission
                        {
                            PurchaseOrderId = po.Id,
                            OrderId = order.Id,
                            SupplierId = po.SupplierId,
                            OrderAmount = po.TotalAmount,
                            CommissionRate = commissionRate,
                            CommissionAmount = po.TotalAmount * commissionRate,
                            PaymentType = "PlatformCommission",
                            Status = "Pending",
                            PaymentRequestData = "",           // ✅ REQUIRED - MUST SET
                            PaymentVerificationData = "",      // ✅ REQUIRED - MUST SET
                            CreatedAt = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(7),
                            Notes = $"Platform commission for Purchase Order #{po.PONumber}"
                        };

                        _context.Commissions.Add(platformCommission);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // ================================
            // STOCK DEDUCTION (UNCHANGED)
            // ================================
            if ((status == "Delivered" || status == "Completed") &&
                previousStatus != "Delivered" &&
                previousStatus != "Completed")
            {
                var pos = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderItems)
                    .Where(p => p.OrderId == order.Id && p.Status != "Cancelled")
                    .ToListAsync();

                foreach (var po in pos)
                {
                    var inventories = await _context.Inventories
                        .Where(i => i.WarehouseId == po.WarehouseId)
                        .ToListAsync();

                    foreach (var item in po.PurchaseOrderItems)
                    {
                        var inv = inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                        if (inv != null)
                        {
                            inv.QuantityReserved -= item.Quantity;
                            inv.QuantityOnHand -= item.Quantity;
                        }
                    }
                }
            }

            // ================================
            // STATUS HISTORY
            // ================================
            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = status,
                Comments = comments,
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.Now
            };

            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null || order.OrderStatus == "Completed" || order.OrderStatus == "Delivered" || order.OrderStatus == "Cancelled")
                    return false;

                // Release reserved stock from warehouses
                var pos = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderItems)
                    .Where(p => p.OrderId == order.Id && p.Status != "Cancelled" && p.Status != "Completed").ToListAsync();

                foreach (var po in pos)
                {
                    var inventories = await _context.Inventories.Where(i => i.WarehouseId == po.WarehouseId).ToListAsync();
                    foreach (var item in po.PurchaseOrderItems)
                    {
                        if (item.ProductId == null) continue; // Custom product not in inventory

                        var inv = inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                        if (inv != null)
                        {
                            // Only decrement what we reserved during the split
                            inv.QuantityReserved -= item.Quantity;
                        }
                    }
                    po.Status = "Cancelled";
                }

                order.OrderStatus = "Cancelled";
                
                var history = new OrderStatusHistory { OrderId = order.Id, Status = "Cancelled", Comments = "Order cancelled by Retailer. Stock reservations released.", ChangedByUserId = order.RetailerId, ChangedAt = DateTime.Now };
                _context.OrderStatusHistories.Add(history);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task<IEnumerable<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId)
        {
            return await _context.OrderStatusHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> AcceptOrderAsync(int orderId, int? explicitWarehouseId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Supplier)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null || order.OrderStatus != "Pending") return false;

                // Load all active warehouses for this supplier
                var query = _context.Warehouses
                    .Include(w => w.Inventories)
                    .Where(w => w.SupplierId == order.SupplierId && w.Status == SCM_System.Models.Enums.WarehouseStatus.Active);
                
                if (explicitWarehouseId.HasValue)
                {
                    query = query.Where(w => w.Id == explicitWarehouseId.Value);
                }
                
                var warehouses = await query.ToListAsync();
                if (!warehouses.Any()) throw new InvalidOperationException("No active warehouses available to fulfill this order.");

                var poAllocations = new Dictionary<int, List<PurchaseOrderItem>>(); // WarehouseId -> PO Items
                var subtotalAllocations = new Dictionary<int, decimal>();

                // STEP 1: Try finding a SINGLE warehouse that can fulfill the entire order
                Warehouse? singleWarehouseMatch = null;
                foreach (var w in warehouses)
                {
                    bool canFulfillAll = true;
                    foreach (var item in order.OrderItems)
                    {
                        if (item.ProductId == null) continue; // Custom item skip inventory check

                        var inv = w.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                        if (inv == null || (inv.QuantityOnHand - inv.QuantityReserved) < item.Quantity)
                        {
                            canFulfillAll = false;
                            break;
                        }
                    }
                    if (canFulfillAll)
                    {
                        singleWarehouseMatch = w;
                        break;
                    }
                }

                if (singleWarehouseMatch != null)
                {
                    // Full allocation to ONE warehouse
                    poAllocations[singleWarehouseMatch.Id] = new List<PurchaseOrderItem>();
                    subtotalAllocations[singleWarehouseMatch.Id] = 0;

                    foreach (var item in order.OrderItems)
                    {
                        if (item.ProductId.HasValue)
                        {
                            var inventory = singleWarehouseMatch.Inventories.First(i => i.ProductId == item.ProductId.Value);
                            inventory.QuantityReserved += item.Quantity;
                        }
                        
                        poAllocations[singleWarehouseMatch.Id].Add(new PurchaseOrderItem
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Description = item.Description,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });
                        subtotalAllocations[singleWarehouseMatch.Id] += (item.Quantity * item.UnitPrice);
                    }
                }
                else
                {
                    // STEP 2: Fallback to Multi-Warehouse Splitting (Existing logic)
                    foreach (var item in order.OrderItems)
                    {
                        int remainingQty = item.Quantity;
                        
                        // Sort warehouses dynamically for each product: priority to default, then highest available stock
                        var orderedWarehouses = warehouses.OrderByDescending(w => w.IsDefault).ThenByDescending(w => 
                        {
                            var inv = w.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                            return inv != null ? (inv.QuantityOnHand - inv.QuantityReserved) : 0;
                        }).ToList();

                        foreach (var w in orderedWarehouses)
                        {
                            if (remainingQty <= 0) break;

                            var inventory = w.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                            if (inventory == null) continue;

                            int available = inventory.QuantityOnHand - inventory.QuantityReserved;
                            if (available > 0)
                            {
                                int allocate = Math.Min(available, remainingQty);
                                
                                // Deduct and reserve
                                inventory.QuantityReserved += allocate;
                                remainingQty -= allocate;

                                // Record allocation
                                if (!poAllocations.ContainsKey(w.Id))
                                {
                                    poAllocations[w.Id] = new List<PurchaseOrderItem>();
                                    subtotalAllocations[w.Id] = 0;
                                }

                                poAllocations[w.Id].Add(new PurchaseOrderItem
                                {
                                    ProductId = item.ProductId,
                                    ProductName = item.ProductName,
                                    Description = item.Description,
                                    Quantity = allocate,
                                    UnitPrice = item.UnitPrice
                                });

                                subtotalAllocations[w.Id] += (allocate * item.UnitPrice);
                            }
                        }

                        if (remainingQty > 0)
                        {
                            if (item.ProductId == null)
                            {
                                // Custom item that couldn't be allocated to any warehouse (shouldn't happen with at least one warehouse)
                                // Assign to the first available warehouse anyway
                                var fallbackWh = warehouses.First();
                                if (!poAllocations.ContainsKey(fallbackWh.Id))
                                {
                                    poAllocations[fallbackWh.Id] = new List<PurchaseOrderItem>();
                                    subtotalAllocations[fallbackWh.Id] = 0;
                                }
                                poAllocations[fallbackWh.Id].Add(new PurchaseOrderItem
                                {
                                    ProductId = null,
                                    ProductName = item.ProductName,
                                    Description = item.Description,
                                    Quantity = remainingQty,
                                    UnitPrice = item.UnitPrice
                                });
                                subtotalAllocations[fallbackWh.Id] += (remainingQty * item.UnitPrice);
                            }
                            else
                            {
                                throw new InvalidOperationException($"Insufficient global stock for product: {item.ProductName}");
                            }
                        }
                    }
                }

                // Create independent POs for each allocated warehouse
                foreach (var allocation in poAllocations)
                {
                    int wId = allocation.Key;
                    var poItems = allocation.Value;
                    decimal subtotal = subtotalAllocations[wId];

                    var po = new PurchaseOrder
                    {
                        PONumber = "PO-" + (order.OrderNumber.StartsWith("ORD-") ? order.OrderNumber.Substring(4) : order.OrderNumber) + "-" + wId,
                        OrderId = order.Id,
                        RetailerId = order.RetailerId,
                        SupplierId = order.SupplierId,
                        WarehouseId = wId,
                        Subtotal = subtotal,
                        VAT = subtotal * 0.15m,
                        Discount = 0m,
                        TotalAmount = subtotal + (subtotal * 0.15m),
                        Status = "PO Issued",
                        DeliveryAddress = order.DeliveryAddress ?? "N/A",
                        ExpectedDeliveryDate = order.ExpectedDeliveryDate ?? DateTime.Now.AddDays(7),
                        CreatedAt = DateTime.Now,
                        OrderDate = DateTime.Now,
                        PurchaseOrderItems = poItems
                    };

                    _context.PurchaseOrders.Add(po);
                }

                // Status Logic: Split? -> Partially Processing, Else -> Processing
                bool isSplit = poAllocations.Count > 1;
                order.OrderStatus = isSplit ? "Partially Processing" : "Processing";
                
                var history = new OrderStatusHistory 
                { 
                    OrderId = order.Id, 
                    Status = order.OrderStatus,
                    Comments = isSplit ? "Order split across multiple warehouses due to stock allocation." : "Order assigned to single warehouse for fulfillment.", 
                    ChangedByUserId = order.Supplier.UserId, 
                    ChangedAt = DateTime.Now 
                };
                _context.OrderStatusHistories.Add(history);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notify Retailer
                var retailerUserId = await _context.Retailers
                    .Where(r => r.Id == order.RetailerId)
                    .Select(r => r.UserId)
                    .FirstOrDefaultAsync();

                if (retailerUserId != 0)
                {
                    await _notificationService.SendNotificationAsync(
                        retailerUserId,
                        "Order Accepted ✅",
                        $"Your order #{order.OrderNumber} has been accepted by {order.Supplier?.CompanyName}.",
                        "Success",
                        $"/Retailer/OrderTrackingDetails/{order.Id}"
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                // Rollback implicitly by disposing uncommitted transaction
                if (ex is InvalidOperationException) throw; 
                throw new InvalidOperationException("Failed to allocate order: " + ex.Message, ex);
            }
        }

        public async Task<bool> RejectOrderAsync(int orderId, string reason)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.OrderStatus != "Pending") return false;

            order.OrderStatus = "Rejected";
            order.RejectionReason = reason;
            order.RejectedAt = DateTime.Now;

            var history = new OrderStatusHistory 
            { 
                OrderId = order.Id, 
                Status = "Rejected",
                Comments = $"Order rejected by supplier. Reason: {reason}", 
                ChangedByUserId = order.SupplierId, // Assuming SupplierId corresponds to a user for now, or use a specific user id
                ChangedAt = DateTime.Now 
            };
            _context.OrderStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            // Notify Retailer
            var retailerUserId = await _context.Retailers
                .Where(r => r.Id == order.RetailerId)
                .Select(r => r.UserId)
                .FirstOrDefaultAsync();

            if (retailerUserId != 0)
            {
                await _notificationService.SendNotificationAsync(
                    retailerUserId,
                    "Order Rejected ❌",
                    $"Your order #{order.OrderNumber} was rejected by {order.Supplier?.CompanyName}. Reason: {reason}",
                    "Error",
                    $"/Retailer/OrderTrackingDetails/{order.Id}"
                );
            }

            return true;
        }


        public async Task<int> CreateMissingCommissionsForDeliveredOrders()
        {
            var deliveredOrders = await _context.Orders
                .Include(o => o.PurchaseOrders)
                .Where(o => o.OrderStatus == "Delivered" || o.OrderStatus == "Completed")
                .ToListAsync();

            int createdCount = 0;

            foreach (var order in deliveredOrders)
            {
                foreach (var po in order.PurchaseOrders.Where(p => p.Status == "Delivered" || p.Status == "Completed"))
                {
                    var existingOrderPayment = await _context.Commissions
                        .FirstOrDefaultAsync(c => c.PurchaseOrderId == po.Id && c.PaymentType == "OrderPayment");

                    if (existingOrderPayment == null)
                    {
                        var orderPayment = new Commission
                        {
                            PurchaseOrderId = po.Id,
                            OrderId = order.Id,
                            SupplierId = po.SupplierId,
                            RetailerId = order.RetailerId,
                            OrderAmount = po.TotalAmount,
                            CommissionRate = 1.00m,
                            CommissionAmount = po.TotalAmount,
                            PaymentType = "OrderPayment",
                            Status = "Pending",
                            ChapaTransactionId = "",
                            PaymentRequestData = "",
                            PaymentVerificationData = "",
                            CreatedAt = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(7),
                            Notes = $"Order payment for Purchase Order #{po.PONumber}"
                        };
                        _context.Commissions.Add(orderPayment);
                        createdCount++;
                    }

                    var existingPlatform = await _context.Commissions
                        .FirstOrDefaultAsync(c => c.PurchaseOrderId == po.Id && c.PaymentType == "PlatformCommission");

                    if (existingPlatform == null)
                    {
                        var commissionRate = 0.05m;
                        var platformAmount = po.TotalAmount * commissionRate;

                        var platformCommission = new Commission
                        {
                            PurchaseOrderId = po.Id,
                            OrderId = order.Id,
                            SupplierId = po.SupplierId,
                            OrderAmount = po.TotalAmount,
                            CommissionRate = commissionRate,
                            CommissionAmount = platformAmount,
                            PaymentType = "PlatformCommission",
                            Status = "Pending",
                            ChapaTransactionId = "",
                            PaymentRequestData = "",
                            PaymentVerificationData = "",
                            CreatedAt = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(7),
                            Notes = $"Platform commission for Purchase Order #{po.PONumber}"
                        };
                        _context.Commissions.Add(platformCommission);
                        createdCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return createdCount;
        }


        // Add this method to OrderService.cs
        private string GenerateOrderQRCode(int orderId)
        {
            // Generate a unique QR code that's easy to scan and verify
            // Format: ORD-{orderId}-{timestamp}-{random}
            var timestamp = DateTime.Now.Ticks.ToString().Substring(8);
            var random = new Random().Next(1000, 9999);
            return $"ORD-{orderId}-{timestamp}-{random}";
        }

        // Then, update your CreateOrderFromPurchaseOrderAsync method to include QR code:
        

    }
}
