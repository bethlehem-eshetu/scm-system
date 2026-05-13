using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;
using SCM_System.Models.Enums;

namespace SCM_System.Services
{
    public class OrderService(
        ApplicationDbContext context, 
        INotificationService notificationService, 
        IPurchaseOrderService poService, 
        ICommissionService commissionService, 
        IInventoryService inventoryService) : IOrderService
    {
        private readonly INotificationService _notificationService = notificationService;
        private readonly ApplicationDbContext _context = context;
        private readonly IPurchaseOrderService _poService = poService;
        private readonly ICommissionService _commissionService = commissionService;
        private readonly IInventoryService _inventoryService = inventoryService;

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
                Subtotal = po.TotalAmount / 1.15m, // Derived from PO if it doesn't have explicit bits yet, or just copy po details
                VAT = po.TotalAmount - (po.TotalAmount / 1.15m),
                TotalAmount = po.TotalAmount,
                OrderStatus = POStatus.Picking,
                PaymentStatus = PaymentStatus.Pending.ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
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
                Status = POStatus.Picking,
                Comments = "Order generated from Purchase Order",
                ChangedByUserId = po.Supplier.UserId,
                ChangedAt = DateTime.Now
            };
            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            // Link reservations and confirm them
            var reservations = await _context.InventoryReservations.Where(r => r.PurchaseOrderId == purchaseOrderId).ToListAsync();
            foreach(var r in reservations)
            {
                r.OrderId = order.Id;
                await _context.SaveChangesAsync();
                await _inventoryService.ConfirmReservationAsync(r.Id);
            }

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
                    if (po.Status != status)
                    {
                        await _poService.UpdatePurchaseOrderStatusAsync(po.Id, status, changedByUserId);
                    }
                }
            }

            // Sync Order Total with PO Totals if they have diverged (e.g. VAT addition)
            if (order.PurchaseOrders != null && order.PurchaseOrders.Any())
            {
                order.TotalAmount = order.PurchaseOrders.Sum(p => p.TotalAmount);
            }

            var previousStatus = order.OrderStatus;
            order.OrderStatus = status;

            // ================================
            // 🔥 CHAPA RELEASE & COMMISSION DEDUCTION
            // ================================
            if (status == "Completed" && order.PaymentStatus == "Chapa")
            {
                order.PaymentStatus = PaymentStatus.Paid.ToString();
                
                foreach (var po in order.PurchaseOrders)
                {
                    // Create Platform Commission
                    await _commissionService.CreateCommissionAsync(order.Id, po.TotalAmount, po.Id);
                    
                    // Mark PO as paid in our internal tracking
                    po.PaymentStatus = "Paid";
                }
            }

            // ================================
            // 🔥 COMMISSION RECORDS (Ensure they exist)
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
                        // ✅ ORDER PAYMENT (Full amount owed by Retailer to Marketplace)
                        // Note: This is NOT a commission the supplier pays.
                        var orderPayment = new Commission
                        {
                            PurchaseOrderId = po.Id,
                            OrderId = order.Id,
                            SupplierId = po.SupplierId,
                            RetailerId = order.RetailerId,
                            OrderAmount = po.TotalAmount,
                            CommissionRate = 0.00m, // It's the base payment, not a fee
                            CommissionAmount = po.TotalAmount,
                            PaymentType = "OrderPayment",
                            Status = (status == "Completed") ? PaymentStatus.Paid.ToString() : PaymentStatus.Pending.ToString(),
                            CreatedAt = DateTime.Now,
                            PaidAt = (status == "Completed") ? DateTime.Now : null,
                            DueDate = DateTime.Now.AddDays(7),
                            Notes = $"Main order settlement for #{po.PONumber}"
                        };

                        _context.Commissions.Add(orderPayment);
                    }
                    else if (status == "Completed")
                    {
                        existingOrderPayment.Status = PaymentStatus.Paid.ToString();
                        existingOrderPayment.PaidAt = DateTime.Now;
                    }
                }
            }

            await _context.SaveChangesAsync();

            // STOCK DEDUCTION (REMOVED - Managed by PurchaseOrderService)

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

                if (order == null || order.OrderStatus != POStatus.Issued && order.OrderStatus != "Pending") return false;

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
                    singleWarehouseMatch.CurrentWorkload++; // Increment workload

                    foreach (var item in order.OrderItems)
                    {
                        
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
                        
                        // Sort warehouses dynamically for each product: 
                        // 1. Coverage Area match
                        // 2. Lowest Workload
                        // 3. Priority to default, then highest available stock
                        var orderedWarehouses = warehouses
                            .OrderByDescending(w => !string.IsNullOrEmpty(order.DeliveryCity) && w.CoverageRegions != null && w.CoverageRegions.Contains(order.DeliveryCity))
                            .ThenBy(w => w.CurrentWorkload)
                            .ThenByDescending(w => w.IsDefault)
                            .ThenByDescending(w => 
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
                                remainingQty -= allocate; // 🔥 FIX: Decrement remaining quantity
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
                decimal totalOrderAmount = 0;
                foreach (var allocation in poAllocations)
                {
                    int wId = allocation.Key;
                    var poItems = allocation.Value;
                    decimal subtotal = subtotalAllocations[wId];
                    decimal poTotal = subtotal + (subtotal * 0.15m);
                    totalOrderAmount += poTotal;

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
                        TotalAmount = poTotal,
                        Status = POStatus.Accepted,
                        DeliveryAddress = order.DeliveryAddress ?? "N/A",
                        ExpectedDeliveryDate = order.ExpectedDeliveryDate ?? DateTime.Now.AddDays(7),
                        CreatedAt = DateTime.Now,
                        OrderDate = DateTime.Now,
                        PurchaseOrderItems = poItems
                    };

                    _context.PurchaseOrders.Add(po);
                    await _context.SaveChangesAsync(); // Save to get PO ID

                    // NEW: Reserve stock immediately so it reflects in the dashboard
                    // Fix: Pass OrderId for better traceability and cancellation support
                    var reservationSuccess = await _inventoryService.BulkReserveStockForPOAsync(po.Id, po.SupplierId, po.WarehouseId);
                    if (reservationSuccess)
                    {
                        var reservations = await _context.InventoryReservations.Where(r => r.PurchaseOrderId == po.Id).ToListAsync();
                        foreach(var r in reservations) r.OrderId = order.Id;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to reserve stock for Product(s) in PO {po.PONumber}. Order fulfillment blocked.");
                    }
                }

                // Status Logic: Accepted and awaiting deposit
                order.OrderStatus = POStatus.Accepted;
                order.Subtotal = poAllocations.Values.Sum(v => v.Sum(i => i.Quantity * i.UnitPrice));
                order.VAT = order.Subtotal * 0.15m;
                order.TotalAmount = order.Subtotal + order.VAT;
                
                var history = new OrderStatusHistory 
                { 
                    OrderId = order.Id, 
                    Status = order.OrderStatus,
                    Comments = "Order accepted. Awaiting 50% advanced payment from retailer.", 
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
                        "Order Accepted ✅ - Payment Required",
                        $"Your order #{order.OrderNumber} has been accepted. Please pay the 50% advanced deposit to proceed with fulfillment.",
                        "Success",
                        $"/Order/Details/{order.Id}"
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
                    $"/Order/Details/{order.Id}"
                );
            }

            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _context.Orders
                .Include(o => o.Supplier)
                .Include(o => o.PurchaseOrders)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new InvalidOperationException("Order not found.");

            // Security Check: Only the retailer who placed the order can cancel it
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
            if (retailer == null || order.RetailerId != retailer.Id) 
            {
                throw new InvalidOperationException("Unauthorized: You do not have permission to cancel this order.");
            }

            if (order.OrderStatus == POStatus.Cancelled) return true;

            // Rules: Cannot cancel if any PO is Picked, Packing, Packed, Ready, InTransit, Delivered, Completed
            var blockedStatuses = new[] { POStatus.Picked, POStatus.Packing, POStatus.Packed, POStatus.Ready, POStatus.InTransit, POStatus.Delivered, POStatus.Completed };
            
            var blockingPO = order.PurchaseOrders.FirstOrDefault(po => blockedStatuses.Contains(po.Status) || po.PickedAt != null);
            if (blockingPO != null)
            {
                throw new InvalidOperationException($"Order cannot be cancelled because it is already being processed (PO Status: {blockingPO.Status}).");
            }

            // Transactional update
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.OrderStatus = POStatus.Cancelled;
                order.CancelledAt = DateTime.Now;
                order.UpdatedAt = DateTime.Now;

                foreach (var po in order.PurchaseOrders)
                {
                    po.Status = POStatus.Cancelled;
                    po.UpdatedAt = DateTime.Now;
                }

                // Restore stock
                await _inventoryService.ReturnStockOnCancelAsync(order.Id);

                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = POStatus.Cancelled,
                    Comments = "Order cancelled by retailer.",
                    ChangedByUserId = userId,
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
                        "Order Cancelled ⚠️",
                        $"Your order #{order.OrderNumber} has been successfully cancelled.",
                        "Warning",
                        $"/Order/Details/{order.Id}"
                    );
                }

                // Notify Supplier
                var supplierUserId = order.Supplier?.UserId ?? 0;
                if (supplierUserId != 0)
                {
                    await _notificationService.SendNotificationAsync(
                        supplierUserId,
                        "Order Cancelled ⚠️",
                        $"Order #{order.OrderNumber} was cancelled by the retailer.",
                        "Warning",
                        $"/Order/Details/{order.Id}"
                    );
                }

                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log error for diagnostics
                Console.WriteLine($"[CancelOrder Error] Order {orderId}: {ex.Message}");
                throw new InvalidOperationException("Failed to cancel order due to a system error. Please try again later.");
            }
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
                            Status = PaymentStatus.Pending.ToString(),
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
                        var commissionPercentage = Supplier.GetTieredCommissionRate(po.TotalAmount);
                        var commissionRate = commissionPercentage / 100m;
                        var platformAmount = Math.Round(po.TotalAmount * commissionRate, 2);

                        var platformCommission = new Commission
                        {
                            PurchaseOrderId = po.Id,
                            OrderId = order.Id,
                            SupplierId = po.SupplierId,
                            RetailerId = order.RetailerId,
                            OrderAmount = po.TotalAmount,
                            CommissionRate = commissionRate,
                            CommissionAmount = platformAmount,
                            PaymentType = "PlatformCommission",
                            Status = PaymentStatus.Pending.ToString(),
                            CreatedAt = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(7),
                            Notes = $"Platform service fee ({commissionPercentage}%) for #{po.PONumber}"
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
