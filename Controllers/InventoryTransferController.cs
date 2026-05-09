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
    [Route("InventoryTransfer")]
    public class InventoryTransferController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryTransferController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(int productId, int quantity, int sourceWarehouseId, string? remarks)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var manager = await _context.SupplierEmployees
                .Include(e => e.HubAccesses)
                .Include(e => e.WarehouseAssignments)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (manager == null) return Unauthorized();

            int destinationWarehouseId = manager.WarehouseId ?? manager.WarehouseAssignments.FirstOrDefault(wa => wa.IsActive)?.WarehouseId ?? 0;

            if (destinationWarehouseId == 0) return BadRequest("Destination warehouse not found.");

            var transfer = new InventoryTransfer
            {
                ProductId = productId,
                Quantity = quantity,
                SourceWarehouseId = sourceWarehouseId,
                DestinationWarehouseId = destinationWarehouseId,
                SupplierId = manager.SupplierId,
                RequestedById = manager.Id,
                Status = TransferStatus.Requested,
                RequestedDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                Remarks = remarks
            };

            _context.InventoryTransfers.Add(transfer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Replenishment request submitted successfully.";
            return RedirectToAction("Dashboard", "Warehouse");
        }
    }
}
