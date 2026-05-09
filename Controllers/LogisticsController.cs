using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Enums;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class LogisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LogisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> DispatchCenter()
        {
            return View();
        }

        public async Task<IActionResult> InventoryTransfers()
        {
            var supplierId = await GetSupplierIdAsync();
            if (supplierId == 0) return Unauthorized();

            var transfers = await _context.InventoryTransfers
                .Include(t => t.Product)
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Where(t => t.SupplierId == supplierId)
                .OrderByDescending(t => t.RequestedDate)
                .ToListAsync();

            // Calculate KPIs
            ViewBag.PendingCount = transfers.Count(t => t.Status == TransferStatus.Requested);
            ViewBag.InTransitCount = transfers.Count(t => t.Status == TransferStatus.InTransit);
            ViewBag.CompletedCount = transfers.Count(t => t.Status == TransferStatus.Received);
            ViewBag.UnitsRebalanced = transfers.Where(t => t.Status == TransferStatus.Received).Sum(t => t.Quantity);

            return View(transfers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTransfer(int id)
        {
            var transfer = await _context.InventoryTransfers
                .Include(t => t.SourceWarehouse)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null) return NotFound();
            if (transfer.Status != TransferStatus.Requested)
            {
                TempData["ErrorMessage"] = "Transfer is not in a state that can be approved.";
                return RedirectToAction(nameof(InventoryTransfers));
            }

            // Guard 3: Transfer Integrity - Check source stock
            if (transfer.ProductId.HasValue)
            {
                var sourceInv = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.WarehouseId == transfer.SourceWarehouseId && i.ProductId == transfer.ProductId.Value);
                
                var available = (sourceInv?.QuantityOnHand ?? 0) - (sourceInv?.QuantityReserved ?? 0);
                if (available < transfer.Quantity)
                {
                    TempData["ErrorMessage"] = "Cannot approve transfer: Source warehouse has insufficient available stock.";
                    return RedirectToAction(nameof(InventoryTransfers));
                }

                // Reserve the stock in source warehouse
                if (sourceInv != null)
                {
                    sourceInv.QuantityReserved += transfer.Quantity;
                    _context.Update(sourceInv);
                }
            }

            transfer.Status = TransferStatus.Approved;
            transfer.ApprovedDate = DateTime.Now;
            _context.Update(transfer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transfer approved and stock reserved.";
            return RedirectToAction(nameof(InventoryTransfers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipTransfer(int id)
        {
            var transfer = await _context.InventoryTransfers.FindAsync(id);
            if (transfer == null) return NotFound();

            if (transfer.Status != TransferStatus.Approved)
            {
                TempData["ErrorMessage"] = "Transfer must be approved before it can be shipped.";
                return RedirectToAction(nameof(InventoryTransfers));
            }

            transfer.Status = TransferStatus.InTransit;
            _context.Update(transfer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transfer marked as In Transit.";
            return RedirectToAction(nameof(InventoryTransfers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveTransfer(int id)
        {
            var transfer = await _context.InventoryTransfers.FindAsync(id);
            if (transfer == null) return NotFound();

            // Guard 3: Transfer Integrity - Cannot receive twice
            if (transfer.Status == TransferStatus.Received)
            {
                TempData["ErrorMessage"] = "Transfer has already been received.";
                return RedirectToAction(nameof(InventoryTransfers));
            }

            if (transfer.Status != TransferStatus.InTransit)
            {
                TempData["ErrorMessage"] = "Transfer must be in transit to be received.";
                return RedirectToAction(nameof(InventoryTransfers));
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (transfer.ProductId.HasValue)
                {
                    // 1. Remove from source (Decrement OnHand and Reserved)
                    var sourceInv = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == transfer.SourceWarehouseId && i.ProductId == transfer.ProductId.Value);
                    if (sourceInv != null)
                    {
                        sourceInv.QuantityOnHand -= transfer.Quantity;
                        sourceInv.QuantityReserved -= transfer.Quantity;
                        _context.Update(sourceInv);
                    }

                    // 2. Add to destination (Increment OnHand)
                    var destInv = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == transfer.DestinationWarehouseId && i.ProductId == transfer.ProductId.Value);
                    
                    if (destInv == null)
                    {
                        destInv = new Inventory
                        {
                            WarehouseId = transfer.DestinationWarehouseId,
                            ProductId = transfer.ProductId.Value,
                            QuantityOnHand = transfer.Quantity,
                            QuantityReserved = 0,
                            LastUpdated = DateTime.Now
                        };
                        _context.Inventories.Add(destInv);
                    }
                    else
                    {
                        destInv.QuantityOnHand += transfer.Quantity;
                        destInv.LastUpdated = DateTime.Now;
                        _context.Update(destInv);
                    }
                }

                transfer.Status = TransferStatus.Received;
                transfer.CompletionDate = DateTime.Now;
                _context.Update(transfer);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                TempData["SuccessMessage"] = "Transfer received. Inventory updated.";
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                TempData["ErrorMessage"] = "Error receiving transfer: " + ex.Message;
            }

            return RedirectToAction(nameof(InventoryTransfers));
        }

        public IActionResult CreateTransfer()
        {
            return View();
        }

        private async Task<int> GetSupplierIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var s = await _context.Suppliers.FirstOrDefaultAsync(x => x.UserId == userId);
                return s?.Id ?? 0;
            }
            return 0;
        }
    }
}
