using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;
using System.Transactions;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SCM_System.Services
{
    public interface IInventoryService
    {
        Task<bool> BulkReserveStockForPOAsync(int purchaseOrderId, int supplierId, int? warehouseId);
        Task<bool> ConfirmReservationAsync(int reservationId);
        Task<bool> ReleaseReservationAsync(int reservationId, string reason);
        Task<bool> DeductStockOnPickAsync(int purchaseOrderId);
        Task<bool> ReturnStockOnCancelAsync(int orderId);
        Task<List<InventoryReservation>> GetExpiredReservationsAsync();
        Task ProcessExpiredReservationsAsync();
        Task<List<InventoryReservation>> GetReservationsByOrderIdAsync(int orderId);
        Task<int> GetAvailableStockAsync(int productId);
        Task<bool> CreateStockTransferAsync(StockTransfer transfer);
        Task<bool> ApproveStockTransferAsync(int transferId);
        Task<bool> ReceiveStockTransferAsync(int transferId);
        Task AdjustInventoryAsync(InventoryAdjustment adjustment);
        Task CreateDailySnapshotAsync();
        Task RecalculateInventoryAsync(int? warehouseId = null, int? productId = null);
        Task RecalculateAllAsync();
        
        // Inbound Shipments
        Task<int> RegisterInboundShipmentAsync(InboundShipment shipment);
        Task<bool> ReceiveInboundItemAsync(int shipmentId, int productId, int receivedQty, int damagedQty, string? batchNumber = null, DateTime? expiryDate = null);
        Task<bool> FinalizeInboundShipmentAsync(int shipmentId, int performedByUserId);
        
        // Returns
        Task<bool> RestockReturnedItemsAsync(int returnRequestId, int performedByUserId);
    }

    public class InventoryService(ApplicationDbContext context, ILogger<InventoryService> logger, INotificationService notificationService) : IInventoryService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<InventoryService> _logger = logger;
        private readonly INotificationService _notificationService = notificationService;

        // ReserveStockAsync removed in favor of BulkReserveStockForPOAsync for better atomicity.

        public async Task<bool> BulkReserveStockForPOAsync(int purchaseOrderId, int supplierId, int? warehouseId)
        {
            var maxRetries = 3;
            var retryCount = 0;
            var delay = 100;

            while (retryCount < maxRetries)
            {
                // Fix nested transaction issue by checking current transaction
                var existingTransaction = _context.Database.CurrentTransaction;
                var transaction = existingTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
                
                try
                {
                    var po = await _context.PurchaseOrders
                        .Include(p => p.PurchaseOrderItems)
                        .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

                    if (po == null) throw new ArgumentException("Purchase Order not found.");

                    // 1. Idempotency Check: Strengthen by checking reserved quantity vs required quantity
                    var totalReservedQty = await _context.InventoryReservations
                        .Where(r => r.PurchaseOrderId == purchaseOrderId && r.Status != POStatus.Cancelled)
                        .SumAsync(r => (int?)r.Quantity) ?? 0;
                    
                    var totalRequiredQty = po.PurchaseOrderItems.Sum(i => i.Quantity);

                    if (totalReservedQty >= totalRequiredQty && po.Status != POStatus.Issued) 
                    {
                        _logger.LogInformation("Full reservation already exists for PO {PurchaseOrderId} ({Reserved}/{Required}). Skipping duplicate reservation.", purchaseOrderId, totalReservedQty, totalRequiredQty);
                        return true; 
                    }

                    // 2. Warehouse Capacity Check
                    if (warehouseId.HasValue)
                    {
                        var warehouse = await _context.Warehouses.FindAsync(warehouseId);
                        if (warehouse != null && warehouse.MaxCapacity > 0)
                        {
                            var currentUsage = warehouse.CapacityUsed ?? 0;
                            var utilization = (double)currentUsage / warehouse.MaxCapacity;
                            if (utilization >= 0.95)
                            {
                                throw new InvalidOperationException($"Warehouse {warehouse.Name} is at {utilization:P} capacity. Reservations blocked above 95% threshold.");
                            }
                        }
                    }

                    // 3. Full Fulfillment Selection & Validation
                    foreach (var item in po.PurchaseOrderItems)
                    {
                        if (!item.ProductId.HasValue) continue;

                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == warehouseId);
                        
                        var available = inventory?.QuantityAvailable ?? 0;
                        if (available < item.Quantity)
                        {
                            throw new InvalidOperationException($"Insufficient stock for {item.ProductName} in selected warehouse. Required: {item.Quantity}, Available: {available}. Partial shipment not enabled.");
                        }
                    }

                    // 4. Execution Phase
                    foreach (var item in po.PurchaseOrderItems)
                    {
                        if (!item.ProductId.HasValue) continue;

                        var inventory = await _context.Inventories
                            .Include(i => i.Product)
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == warehouseId);

                        if (inventory?.Product == null)
                        {
                            _logger.LogError("[BulkReserve] Inventory/Product missing for ProductId={ProductId} WarehouseId={WarehouseId} PO={POId}", item.ProductId, warehouseId, purchaseOrderId);
                            throw new InvalidOperationException($"No inventory record found for Product {item.ProductId} in Warehouse {warehouseId}. Inbound stock before accepting this order.");
                        }

                        // Defensive check for ProductId presence (required by index/FK)
                        if (item.ProductId == null || item.ProductId == 0)
                        {
                            _logger.LogError("[BulkReserve] PO Item {ItemId} has invalid ProductId: {ProductId}", item.Id, item.ProductId);
                            throw new InvalidOperationException($"PO Item {item.Id} ({item.ProductName}) has no associated ProductId. Reservation aborted.");
                        }

                        var product = inventory.Product;
                        var beforeAvailable = product.AvailableStock;
                        var beforeReserved = product.ReservedStock;

                        // Source of Truth Update: Atomically update the fast-read cache on Inventory table
                        inventory.QuantityReserved += item.Quantity;
                        
                        // Sync Cache for backward compatibility (Product table)
                        product.ReservedStock += item.Quantity;
                        product.AvailableStock -= item.Quantity;
                        product.LastStockUpdate = DateTime.Now;

                        // Create Ledger Entry
                        _context.InventoryReservations.Add(new InventoryReservation
                        {
                            ProductId = item.ProductId.Value,
                            PurchaseOrderId = purchaseOrderId,
                            SupplierId = supplierId,
                            WarehouseId = warehouseId,
                            Quantity = item.Quantity,
                            ReservedAt = DateTime.Now,
                            ExpiresAt = DateTime.Now.AddHours(24),
                            Status = ReservationStatus.Accepted, // Matches the PO status after reservation
                            Priority = 1,
                            Notes = $"Auto-reserved for PO #{purchaseOrderId}",
                            CreatedAt = DateTime.Now
                        });

                        _context.InventoryMovements.Add(new InventoryMovement
                        {
                            ProductId = item.ProductId.Value,
                            WarehouseId = warehouseId,
                            MovementType = "ReservationHold",
                            Quantity = item.Quantity,
                            BeforeAvailableStock = beforeAvailable,
                            BeforeReservedStock = beforeReserved,
                            AfterAvailableStock = product.AvailableStock,
                            AfterReservedStock = product.ReservedStock,
                            ReferenceNumber = po.PONumber,
                            ReferenceType = "PurchaseOrder",
                            ReferenceId = purchaseOrderId,
                            DocumentReference = po.PONumber,
                            Reason = "Bulk Stock Reservation",
                            CreatedAt = DateTime.Now
                        });
                    }

                    await _context.SaveChangesAsync();
                    if (transaction != null) await transaction.CommitAsync();
                    
                    _logger.LogInformation("Successfully reserved stock for PO {PurchaseOrderId}", purchaseOrderId);
                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    retryCount++;
                    if (retryCount >= maxRetries) throw;
                    await Task.Delay(delay + new Random().Next(0, 100));
                    delay *= 2;
                }
                catch (DbUpdateException dbEx)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                    _logger.LogError(dbEx, "Database update failed in BulkReserveStockForPOAsync for PO {POID}. Error: {Inner}", purchaseOrderId, innerMessage);
                    throw new InvalidOperationException($"Stock reservation failed: {innerMessage}", dbEx);
                }
                catch (Exception ex)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unexpected error in BulkReserveStockForPOAsync for PO {PurchaseOrderId}", purchaseOrderId);
                    throw; 
                }
            }
            return false;
        }

        public async Task<bool> ConfirmReservationAsync(int reservationId)
        {
            var reservation = await _context.InventoryReservations.FindAsync(reservationId);
            if (reservation == null) return false;

            reservation.Status = ReservationStatus.Confirmed;
            reservation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reservation {ReservationId} confirmed", reservationId);
            return true;
        }

        public async Task<bool> ReleaseReservationAsync(int reservationId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var reservation = await _context.InventoryReservations
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == reservationId);

                if (reservation == null) return false;
                if (reservation.Status != ReservationStatus.Pending && reservation.Status != ReservationStatus.Confirmed)
                {
                    _logger.LogWarning("Cannot release reservation {ReservationId} with status {Status}", reservationId, reservation.Status);
                    return false;
                }

                var product = reservation.Product;
                var beforeAvailable = product.AvailableStock;
                var beforeReserved = product.ReservedStock;

                reservation.Status = ReservationStatus.Cancelled;
                reservation.ReleasedAt = DateTime.Now;
                reservation.Notes = reason;
                reservation.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Synchronize product aggregate fields
                product.AvailableStock = await GetAvailableStockFromInventoriesAsync(product.Id);
                product.ReservedStock = await GetReservedStockFromInventoriesAsync(product.Id);
                product.LastStockUpdate = DateTime.Now;

                var movement = new InventoryMovement
                {
                    ProductId = reservation.ProductId,
                    MovementType = "ReservationRelease",
                    Quantity = reservation.Quantity,
                    BeforeAvailableStock = beforeAvailable,
                    BeforeReservedStock = beforeReserved,
                    AfterAvailableStock = product.AvailableStock,
                    AfterReservedStock = product.ReservedStock,
                    ReferenceNumber = reservation.PurchaseOrderId?.ToString(),
                    ReferenceType = "PurchaseOrder",
                    ReferenceId = reservation.PurchaseOrderId,
                    DocumentReference = reservation.PurchaseOrderId?.ToString() ?? "N/A",
                    Reason = reason ?? "Manual Release",
                    CreatedAt = DateTime.Now
                };
                _context.InventoryMovements.Add(movement);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Released reservation {ReservationId}. Stock returned.", reservationId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error releasing reservation {ReservationId}", reservationId);
                return false;
            }
        }

        public async Task<bool> DeductStockOnPickAsync(int purchaseOrderId)
        {
            var existingTransaction = _context.Database.CurrentTransaction;
            var transaction = existingTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
            
            try
            {
                var po = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderItems)
                    .Include(p => p.InventoryReservations)
                    .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

                if (po == null) return false;

                // Check if this PO already has been physically deducted via movements
                var movementExists = await _context.InventoryMovements
                    .AnyAsync(m => m.ReferenceId == purchaseOrderId && m.ReferenceType == "PurchaseOrder" && m.MovementType == "PickDeduction");

                if (movementExists)
                {
                    _logger.LogInformation("PO {PurchaseOrderId} already has physical deduction movements. Ensuring status alignment.", purchaseOrderId);
                    // Just ensure all reservations are closed if they aren't
                    foreach (var r in po.InventoryReservations.Where(r => r.Status != ReservationStatus.Completed))
                    {
                        r.Status = ReservationStatus.Completed;
                        r.UpdatedAt = DateTime.Now;
                    }
                    await _context.SaveChangesAsync();
                    if (transaction != null) await transaction.CommitAsync();
                    return true;
                }

                foreach (var item in po.PurchaseOrderItems)
                {
                    if (!item.ProductId.HasValue) continue;

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == po.WarehouseId);

                    int qty = item.Quantity;

                    if (inventory != null)
                    {
                        var beforeAvailable = inventory.QuantityOnHand - inventory.QuantityReserved;
                        var beforeReserved = inventory.QuantityReserved;

                        // ATOMIC DEDUCTION: Reduce both OnHand and Reserved
                        // Use reservation record to see how much was specifically reserved for this PO
                        var reservation = po.InventoryReservations.FirstOrDefault(r => r.ProductId == item.ProductId);
                        int reservedForThis = reservation?.Quantity ?? qty;

                        // Release reservation on the tracking table
                        inventory.QuantityReserved = Math.Max(0, inventory.QuantityReserved - reservedForThis);
                        // Physically remove the stock
                        inventory.QuantityOnHand = Math.Max(0, inventory.QuantityOnHand - qty);
                        inventory.LastUpdated = DateTime.Now;

                        // Update Product Cache
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.DispatchedStock += qty;
                            product.ReservedStock = await GetReservedStockFromInventoriesAsync(product.Id);
                            product.AvailableStock = await GetAvailableStockFromInventoriesAsync(product.Id);
                            product.LastStockUpdate = DateTime.Now;
                        }

                        // Close reservation record if exists
                        if (reservation != null)
                        {
                            reservation.Status = ReservationStatus.Completed;
                            reservation.PickedAt = DateTime.Now;
                            reservation.UpdatedAt = DateTime.Now;
                        }

                        // Create Movement Record (This is our persistent audit trail)
                        _context.InventoryMovements.Add(new InventoryMovement
                        {
                            ProductId = item.ProductId.Value,
                            WarehouseId = po.WarehouseId,
                            MovementType = "PickDeduction",
                            Quantity = -qty,
                            BeforeAvailableStock = beforeAvailable,
                            BeforeReservedStock = beforeReserved,
                            AfterAvailableStock = inventory.QuantityOnHand - inventory.QuantityReserved,
                            AfterReservedStock = inventory.QuantityReserved,
                            ReferenceNumber = po.PONumber,
                            ReferenceType = "PurchaseOrder",
                            ReferenceId = po.Id,
                            DocumentReference = po.PONumber,
                            Reason = "Stock Picking / Delivery Departure",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
                
                if (po.WarehouseId > 0)
                {
                    foreach (var item in po.PurchaseOrderItems)
                    {
                        if (item.ProductId.HasValue)
                        {
                            await CheckAndTriggerReorderAlertAsync(item.ProductId.Value, po.WarehouseId);
                        }
                    }
                }
                
                _logger.LogInformation("Physical stock deduction completed successfully for PO {PurchaseOrderId}.", purchaseOrderId);
                return true;
            }
            catch (Exception ex)
            {
                if (transaction != null) await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during physical stock deduction for PO {PurchaseOrderId}", purchaseOrderId);
                return false;
            }
        }

        public async Task<bool> ReturnStockOnCancelAsync(int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var reservations = await _context.InventoryReservations
                    .Include(r => r.Product)
                    .Where(r => r.OrderId == orderId && r.Status != ReservationStatus.Completed)
                    .ToListAsync();

                if (!reservations.Any()) return false;

                foreach (var reservation in reservations)
                {
                    var product = reservation.Product;

                    reservation.Status = ReservationStatus.Cancelled;
                    reservation.ReleasedAt = DateTime.Now;
                    reservation.UpdatedAt = DateTime.Now;

                    // BUG FIX: Must decrement QuantityReserved on the Inventory row.
                    // Previously only the reservation record was cancelled but the
                    // Inventory.QuantityReserved column was never restored — causing
                    // dashboards to permanently show stock as unavailable after cancellation.
                    if (reservation.WarehouseId.HasValue)
                    {
                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == reservation.ProductId && i.WarehouseId == reservation.WarehouseId);
                        if (inventory != null)
                        {
                            inventory.QuantityReserved = Math.Max(0, inventory.QuantityReserved - reservation.Quantity);
                            _logger.LogInformation("[CancelReturn] Restored {Quantity} reserved units for Product {ProductId} Warehouse {WarehouseId}", reservation.Quantity, reservation.ProductId, reservation.WarehouseId);
                        }
                    }

                    product.AvailableStock = await GetAvailableStockFromInventoriesAsync(product.Id);
                    product.ReservedStock = await GetReservedStockFromInventoriesAsync(product.Id);
                    product.LastStockUpdate = DateTime.Now;
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Stock returned for cancelled order {OrderId}. {Count} items returned.", orderId, reservations.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error returning stock for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<List<InventoryReservation>> GetExpiredReservationsAsync()
        {
            return await _context.InventoryReservations
                .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt < DateTime.Now)
                .Include(r => r.Product)
                .ToListAsync();
        }

        public async Task ProcessExpiredReservationsAsync()
        {
            var expiredReservations = await GetExpiredReservationsAsync();
            foreach (var reservation in expiredReservations)
            {
                await ReleaseReservationAsync(reservation.Id, "Reservation expired automatically");
            }
            _logger.LogInformation("Processed {Count} expired reservations", expiredReservations.Count);
        }

        public async Task<List<InventoryReservation>> GetReservationsByOrderIdAsync(int orderId)
        {
            return await _context.InventoryReservations
                .Where(r => r.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<int> GetAvailableStockAsync(int productId)
        {
            // Single source of truth: derive available stock from Inventory table
            return await _context.Inventories
                .Where(i => i.ProductId == productId)
                .SumAsync(i => i.QuantityOnHand - i.QuantityReserved);
        }

        public async Task<bool> CreateStockTransferAsync(StockTransfer transfer)
        {
            transfer.Status = "Requested";
            transfer.RequestedAt = DateTime.Now;
            _context.StockTransfers.Add(transfer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveStockTransferAsync(int transferId)
        {
            var transfer = await _context.StockTransfers.FindAsync(transferId);
            if (transfer == null) return false;

            transfer.Status = "Approved";
            transfer.ApprovedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReceiveStockTransferAsync(int transferId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _context.StockTransfers
                    .Include(t => t.Product)
                    .FirstOrDefaultAsync(t => t.Id == transferId);

                if (transfer == null) return false;

                // Deduct from source warehouse
                var sourceProduct = await _context.Products.FindAsync(transfer.ProductId);
                if (sourceProduct.AvailableStock < transfer.Quantity)
                {
                    _logger.LogWarning("Insufficient stock in source warehouse");
                    return false;
                }

                sourceProduct.AvailableStock -= transfer.Quantity;
                sourceProduct.InTransitStock += transfer.Quantity;

                transfer.Status = "InTransit";
                transfer.ShippedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // When received, complete the transfer
                sourceProduct.InTransitStock -= transfer.Quantity;

                var destinationProduct = await _context.Products.FindAsync(transfer.ProductId);
                destinationProduct.AvailableStock += transfer.Quantity;

                transfer.Status = "Received";
                transfer.ReceivedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Synchronize source and destination product aggregate fields
                var productId = transfer.ProductId;
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.AvailableStock = await GetAvailableStockFromInventoriesAsync(productId);
                    product.ReservedStock = await GetReservedStockFromInventoriesAsync(productId);
                    product.LastStockUpdate = DateTime.Now;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error receiving stock transfer {TransferId}", transferId);
                return false;
            }
        }

        public async Task AdjustInventoryAsync(InventoryAdjustment adjustment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var inventory = await _context.Inventories
                    .Include(i => i.Product)
                    .FirstOrDefaultAsync(i => i.WarehouseId == adjustment.WarehouseId && i.ProductId == adjustment.ProductId);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        WarehouseId = adjustment.WarehouseId ?? 0,
                        ProductId = adjustment.ProductId,
                        QuantityOnHand = 0,
                        QuantityReserved = 0,
                        LastUpdated = DateTime.Now
                    };
                    _context.Inventories.Add(inventory);
                }

                var product = inventory.Product;
                var beforeAvailable = product.AvailableStock;
                var beforeReserved = product.ReservedStock;

                // Apply adjustment based on type
                switch (adjustment.AdjustmentType)
                {
                    case "Damage":
                        inventory.QuantityOnHand += adjustment.QuantityChange;
                        product.DamagedStock += Math.Abs(adjustment.QuantityChange);
                        break;
                    case "Theft":
                    case "Shrinkage":
                    case "Loss":
                        inventory.QuantityOnHand += adjustment.QuantityChange;
                        break;
                    default:
                        inventory.QuantityOnHand += adjustment.QuantityChange;
                        break;
                }

                inventory.LastUpdated = DateTime.Now;
                adjustment.CreatedAt = DateTime.Now;
                _context.InventoryAdjustments.Add(adjustment);
                await _context.SaveChangesAsync();

                // Synchronize product aggregate fields
                product.AvailableStock = await GetAvailableStockFromInventoriesAsync(product.Id);
                product.ReservedStock = await GetReservedStockFromInventoriesAsync(product.Id);
                product.LastStockUpdate = DateTime.Now;

                // Create movement record
                _context.InventoryMovements.Add(new InventoryMovement
                {
                    ProductId = adjustment.ProductId,
                    WarehouseId = adjustment.WarehouseId,
                    MovementType = "Adjustment",
                    Quantity = adjustment.QuantityChange,
                    BeforeAvailableStock = beforeAvailable,
                    BeforeReservedStock = beforeReserved,
                    AfterAvailableStock = product.AvailableStock,
                    AfterReservedStock = product.ReservedStock,
                    ReferenceNumber = adjustment.DocumentReference ?? $"ADJ-{DateTime.Now:yyyyMMdd}-{adjustment.Id}",
                    ReferenceType = "InventoryAdjustment",
                    ReferenceId = adjustment.Id,
                    DocumentReference = adjustment.DocumentReference ?? $"ADJ-{DateTime.Now:yyyyMMdd}-{adjustment.Id}",
                    Reason = adjustment.Reason ?? "Manual Adjustment",
                    PerformedBy = adjustment.PerformedById ?? 0,
                    CreatedAt = DateTime.Now,
                    Remarks = $"Type: {adjustment.AdjustmentType}. Reason: {adjustment.Reason}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                if (adjustment.WarehouseId.HasValue && adjustment.QuantityChange < 0)
                {
                    await CheckAndTriggerReorderAlertAsync(adjustment.ProductId, adjustment.WarehouseId.Value);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adjusting inventory for product {ProductId}", adjustment.ProductId);
                throw;
            }
        }

        public async Task CreateDailySnapshotAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            
            var products = await _context.Products.ToListAsync();
            foreach (var product in products)
            {
                var snapshot = new InventorySnapshot
                {
                    ProductId = product.Id,
                    AvailableStock = product.AvailableStock,
                    ReservedStock = product.ReservedStock,
                    DispatchedStock = product.DispatchedStock,
                    DamagedStock = product.DamagedStock,
                    InTransitStock = product.InTransitStock,
                    SnapshotDate = today,
                    CreatedAt = DateTime.Now
                };
                _context.InventorySnapshots.Add(snapshot);
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created daily inventory snapshot for {Count} products", products.Count);
        }

        private async Task CheckAndTriggerReorderAlertAsync(int productId, int warehouseId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || !product.ReorderLevel.HasValue) return;

                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
                if (inventory == null) return;

                int quantityAvailable = inventory.QuantityOnHand - inventory.QuantityReserved;

                if (quantityAvailable <= product.ReorderLevel.Value)
                {
                    // Find warehouse manager or fallback to supplier
                    var warehouse = await _context.Warehouses
                        .Include(w => w.PrimaryManager)
                        .FirstOrDefaultAsync(w => w.Id == warehouseId);

                    int? targetUserId = warehouse?.PrimaryManager?.UserId;
                    
                    if (!targetUserId.HasValue)
                    {
                        var supplier = await _context.Suppliers.FindAsync(product.SupplierId);
                        targetUserId = supplier?.UserId;
                    }

                    if (targetUserId.HasValue)
                    {
                        string message = $"Stock alert: {product.ProductName} in warehouse '{warehouse?.Name}' has dropped to {quantityAvailable} (Reorder level: {product.ReorderLevel.Value}).";
                        
                        // Prevent spamming
                        bool hasRecentAlert = await _context.Notifications
                            .AnyAsync(n => n.UserId == targetUserId.Value 
                                        && !n.IsRead 
                                        && n.Title == "Low Stock Alert" 
                                        && n.Message.Contains(product.ProductName));

                        if (!hasRecentAlert)
                        {
                            await _notificationService.SendNotificationAsync(
                                targetUserId.Value,
                                "Low Stock Alert",
                                message,
                                "Warning",
                                "/Product/MyProducts"
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking reorder level for product {ProductId} in warehouse {WarehouseId}", productId, warehouseId);
            }
        }

        private async Task<int> GetAvailableStockFromInventoriesAsync(int productId)
        {
            return await _context.Inventories
                .Where(i => i.ProductId == productId)
                .SumAsync(i => i.QuantityOnHand - i.QuantityReserved);
        }

        private async Task<int> GetReservedStockFromInventoriesAsync(int productId)
        {
            return await _context.Inventories
                .Where(i => i.ProductId == productId)
                .SumAsync(i => i.QuantityReserved);
        }

        public async Task RecalculateInventoryAsync(int? warehouseId = null, int? productId = null)
        {
            var query = _context.Inventories.AsQueryable();
            if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
            if (productId.HasValue) query = query.Where(i => i.ProductId == productId.Value);

            var inventoryList = await query.ToListAsync();
            var activeStatuses = new List<string> { POStatus.Accepted, POStatus.Processing, POStatus.Picked, POStatus.Packed, POStatus.Ready, POStatus.InTransit };

            // 🔥 STAGE 1: Repair Physical Stock Discrepancies
            // Audit based: Find Delivered/Completed orders that missed their Movement record
            var terminalPOs = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .Where(p => p.Status == POStatus.Delivered || p.Status == "Completed")
                .ToListAsync();

            foreach (var po in terminalPOs)
            {
                var alreadyDeducted = await _context.InventoryMovements
                    .AnyAsync(m => m.ReferenceId == po.Id && m.ReferenceType == "PurchaseOrder" && m.MovementType == "PickDeduction");

                if (!alreadyDeducted)
                {
                    _logger.LogInformation("Reconciliation: Repairing Delivered PO {PONumber} - Missing physical deduction.", po.PONumber);
                    
                    foreach (var item in po.PurchaseOrderItems)
                    {
                        if (!item.ProductId.HasValue) continue;

                        // Identify the inventory record to update
                        var inv = inventoryList.FirstOrDefault(i => i.ProductId == item.ProductId && i.WarehouseId == po.WarehouseId);
                        
                        // If not in our filtered list (e.g. cross-warehouse), load it directly
                        if (inv == null)
                        {
                            inv = await _context.Inventories
                                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == po.WarehouseId);
                        }

                        if (inv != null)
                        {
                            _logger.LogInformation("Reconciliation: Deducting {Qty} units for Product {ProductId} (Warehouse {WarehouseId}).", item.Quantity, item.ProductId, inv.WarehouseId);
                            
                            inv.QuantityOnHand = Math.Max(0, inv.QuantityOnHand - item.Quantity);
                            inv.QuantityReserved = Math.Max(0, inv.QuantityReserved - item.Quantity);
                            inv.LastUpdated = DateTime.Now;

                            _context.InventoryMovements.Add(new InventoryMovement
                            {
                                ProductId = item.ProductId.Value,
                                WarehouseId = inv.WarehouseId,
                                MovementType = "PickDeduction",
                                Quantity = -item.Quantity,
                                ReferenceNumber = po.PONumber,
                                ReferenceType = "PurchaseOrder",
                                ReferenceId = po.Id,
                                DocumentReference = po.PONumber,
                                Reason = "Reconciliation: Restoring missing PickDeduction for Delivered PO",
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }
            }

            // Also repair any stale reservation statuses
            var staleReservations = await _context.InventoryReservations
                .Include(r => r.PurchaseOrder)
                .Where(r => activeStatuses.Contains(r.Status) && r.PurchaseOrder != null && 
                           (r.PurchaseOrder.Status == POStatus.Delivered || r.PurchaseOrder.Status == POStatus.Cancelled))
                .ToListAsync();

            foreach (var r in staleReservations)
            {
                r.Status = r.PurchaseOrder.Status == POStatus.Cancelled ? ReservationStatus.Cancelled : ReservationStatus.Completed;
            }
            
            // Save Stage 1 repairs before Stage 2 recalculates totals
            await _context.SaveChangesAsync();

            // 🔥 RE-FETCH AFTER REPAIR: Stage 1 may have changed QuantityOnHand. 
            // We must refresh our local list to avoid overwriting with stale 250s.
            inventoryList = await query.ToListAsync();

            // STAGE 2: Recalculate totals
            foreach (var inventory in inventoryList)
            {
                // Recalculate based on physical presence in Reservations table
                var actualReserved = await _context.InventoryReservations
                    .Where(r => r.ProductId == inventory.ProductId && 
                                r.WarehouseId == inventory.WarehouseId && 
                                activeStatuses.Contains(r.Status))
                    .SumAsync(r => (int?)r.Quantity) ?? 0;

                _logger.LogInformation("Recalculating Inventory for Product {ProductId}, Warehouse {WarehouseId}. Old Reserved: {OldReserved}, New Reserved: {NewReserved}", inventory.ProductId, inventory.WarehouseId, inventory.QuantityReserved, actualReserved);
                
                inventory.QuantityReserved = actualReserved;
                inventory.LastUpdated = DateTime.Now;

                // Also sync Product cache
                var product = await _context.Products.FindAsync(inventory.ProductId);
                if (product != null)
                {
                    product.ReservedStock = await GetReservedStockFromInventoriesAsync(inventory.ProductId);
                    product.AvailableStock = await GetAvailableStockFromInventoriesAsync(inventory.ProductId);
                    product.LastStockUpdate = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecalculateAllAsync()
        {
            _logger.LogInformation("Global Inventory Reconciliation Started...");
            await RecalculateInventoryAsync(null, null);
            _logger.LogInformation("Global Inventory Reconciliation Completed.");
        }
        // ========== INBOUND SHIPMENT IMPLEMENTATION ==========

        public async Task<int> RegisterInboundShipmentAsync(InboundShipment shipment)
        {
            shipment.ShipmentNumber = $"INB-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
            shipment.Status = "Expected";
            shipment.CreatedAt = DateTime.Now;
            shipment.UpdatedAt = DateTime.Now;

            _context.InboundShipments.Add(shipment);
            await _context.SaveChangesAsync();
            return shipment.Id;
        }

        public async Task<bool> ReceiveInboundItemAsync(int shipmentId, int productId, int receivedQty, int damagedQty, string? batchNumber = null, DateTime? expiryDate = null)
        {
            var item = await _context.InboundShipmentItems
                .FirstOrDefaultAsync(si => si.InboundShipmentId == shipmentId && si.ProductId == productId);

            if (item == null) return false;

            item.ReceivedQuantity = receivedQty;
            item.DamagedQuantity = damagedQty;
            item.BatchNumber = batchNumber;
            item.ExpiryDate = expiryDate;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FinalizeInboundShipmentAsync(int shipmentId, int performedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shipment = await _context.InboundShipments
                    .Include(s => s.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(s => s.Id == shipmentId);

                if (shipment == null || shipment.Status == "Received") return false;

                foreach (var item in shipment.Items)
                {
                    if (item.ReceivedQuantity <= 0) continue;

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == shipment.WarehouseId && i.ProductId == item.ProductId);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            WarehouseId = shipment.WarehouseId,
                            ProductId = item.ProductId,
                            QuantityOnHand = 0,
                            QuantityReserved = 0,
                            LastUpdated = DateTime.Now
                        };
                        _context.Inventories.Add(inventory);
                    }

                    var beforeAvailable = item.Product.AvailableStock;
                    var beforeReserved = item.Product.ReservedStock;

                    // Update Inventory
                    inventory.QuantityOnHand += item.ReceivedQuantity;
                    inventory.LastUpdated = DateTime.Now;

                    // Record Damage if any
                    if (item.DamagedQuantity > 0)
                    {
                        item.Product.DamagedStock += item.DamagedQuantity;
                    }

                    await _context.SaveChangesAsync();

                    // Sync Cache
                    item.Product.AvailableStock = await GetAvailableStockFromInventoriesAsync(item.ProductId);
                    item.Product.ReservedStock = await GetReservedStockFromInventoriesAsync(item.ProductId);
                    item.Product.LastStockUpdate = DateTime.Now;

                    // Record Movement
                    _context.InventoryMovements.Add(new InventoryMovement
                    {
                        ProductId = item.ProductId,
                        WarehouseId = shipment.WarehouseId,
                        MovementType = "InboundReceipt",
                        Quantity = item.ReceivedQuantity,
                        BeforeAvailableStock = beforeAvailable,
                        BeforeReservedStock = beforeReserved,
                        AfterAvailableStock = item.Product.AvailableStock,
                        AfterReservedStock = item.Product.ReservedStock,
                        ReferenceNumber = shipment.ShipmentNumber,
                        ReferenceType = "InboundShipment",
                        ReferenceId = shipment.Id,
                        DocumentReference = shipment.ShipmentNumber,
                        Reason = "Inbound Shipment Finalized",
                        PerformedBy = performedByUserId,
                        CreatedAt = DateTime.Now,
                        Remarks = $"Shipment #{shipment.ShipmentNumber} items put-away"
                    });
                }

                shipment.Status = "Received";
                shipment.ReceivedDate = DateTime.Now;
                shipment.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error finalizing inbound shipment {ShipmentId}", shipmentId);
                return false;
            }
        }

        public async Task<bool> RestockReturnedItemsAsync(int returnRequestId, int performedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var returnRequest = await _context.ReturnRequests
                    .Include(r => r.PurchaseOrder)
                        .ThenInclude(po => po.PurchaseOrderItems)
                            .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(r => r.Id == returnRequestId);

                if (returnRequest == null || returnRequest.PurchaseOrder == null) return false;

                var warehouseId = returnRequest.PurchaseOrder.WarehouseId;
                if (warehouseId == 0) return false;

                foreach (var item in returnRequest.PurchaseOrder.PurchaseOrderItems)
                {
                    if (item.Product == null) continue;

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == warehouseId && i.ProductId == item.ProductId);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            WarehouseId = warehouseId,
                            ProductId = item.ProductId.Value,
                            QuantityOnHand = 0,
                            QuantityReserved = 0,
                            LastUpdated = DateTime.Now
                        };
                        _context.Inventories.Add(inventory);
                    }

                    var beforeAvailable = item.Product.AvailableStock;
                    var beforeReserved = item.Product.ReservedStock;

                    // Restore stock
                    inventory.QuantityOnHand += item.Quantity;
                    inventory.LastUpdated = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Sync Cache
                    item.Product.AvailableStock = await GetAvailableStockFromInventoriesAsync(item.ProductId.Value);
                    item.Product.ReservedStock = await GetReservedStockFromInventoriesAsync(item.ProductId.Value);
                    item.Product.LastStockUpdate = DateTime.Now;

                    // Record Movement
                    _context.InventoryMovements.Add(new InventoryMovement
                    {
                        ProductId = item.ProductId.Value,
                        WarehouseId = warehouseId,
                        MovementType = "ReturnRestock",
                        Quantity = item.Quantity,
                        BeforeAvailableStock = beforeAvailable,
                        BeforeReservedStock = beforeReserved,
                        AfterAvailableStock = item.Product.AvailableStock,
                        AfterReservedStock = item.Product.ReservedStock,
                        ReferenceNumber = returnRequest.ReturnNumber,
                        ReferenceType = "ReturnRequest",
                        ReferenceId = returnRequest.Id,
                        DocumentReference = returnRequest.ReturnNumber,
                        Reason = "Stock Return Restock",
                        PerformedBy = performedByUserId,
                        CreatedAt = DateTime.Now,
                        Remarks = $"Stock restored from return request #{returnRequest.ReturnNumber}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error restocking items for return {ReturnRequestId}", returnRequestId);
                return false;
            }
        }
    }
}