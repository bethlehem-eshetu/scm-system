using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class ReturnService : IReturnService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ReturnService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ReturnRequest> CreateReturnRequestAsync(int purchaseOrderId, string reason, string? description, decimal refundAmount, string? images = null)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Order)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
                throw new Exception("Purchase Order not found");

            var returnRequest = new ReturnRequest
            {
                ReturnNumber = $"RET-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                OrderId = purchaseOrder.OrderId,
                PurchaseOrderId = purchaseOrderId,
                RetailerId = purchaseOrder.RetailerId,
                SupplierId = purchaseOrder.SupplierId,
                Reason = reason,
                Description = description,
                RefundAmount = refundAmount,
                Images = images,
                Status = ReturnStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.ReturnRequests.Add(returnRequest);
            await _context.SaveChangesAsync();

            // Notify supplier
            var supplier = await _context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == purchaseOrder.SupplierId);

            if (supplier?.UserId != null)
            {
                await _notificationService.SendNotificationAsync(
                    supplier.UserId,
                    "🔄 New Return Request",
                    $"A return request has been created for Order #{purchaseOrder.Order?.OrderNumber}",
                    "Warning",
                    $"/Return/SupplierReturns"
                );
            }

            return returnRequest;
        }

        public async Task<ReturnRequest> GetReturnRequestByIdAsync(int id)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.Retailer)
                    .ThenInclude(r => r.User)
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<ReturnRequest>> GetRetailerReturnsAsync(int retailerId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.Supplier)
                .Where(r => r.RetailerId == retailerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ReturnRequest>> GetSupplierReturnsAsync(int supplierId)
        {
            return await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.Retailer)
                .Where(r => r.SupplierId == supplierId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ReturnRequest>> GetPendingReturnsAsync()
        {
            return await _context.ReturnRequests
                .Include(r => r.Supplier)
                .Include(r => r.Retailer)
                .Where(r => r.Status == ReturnStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ReturnRequest> ApproveReturnAsync(int returnId, string adminNotes)
        {
            var returnRequest = await GetReturnRequestByIdAsync(returnId);
            if (returnRequest == null)
                throw new Exception("Return request not found");

            returnRequest.Status = ReturnStatus.Approved;
            returnRequest.AdminNotes = adminNotes;
            returnRequest.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Notify retailer
            await _notificationService.SendNotificationAsync(
                returnRequest.Retailer.UserId,
                "✅ Return Request Approved",
                $"Your return request #{returnRequest.ReturnNumber} has been approved. Please ship the items back.",
                "Success",
                $"/Return/MyReturns"
            );

            return returnRequest;
        }

        public async Task<ReturnRequest> RejectReturnAsync(int returnId, string rejectionReason)
        {
            var returnRequest = await GetReturnRequestByIdAsync(returnId);
            if (returnRequest == null)
                throw new Exception("Return request not found");

            returnRequest.Status = ReturnStatus.Rejected;
            returnRequest.RejectionReason = rejectionReason;

            await _context.SaveChangesAsync();

            // Notify retailer
            await _notificationService.SendNotificationAsync(
                returnRequest.Retailer.UserId,
                "❌ Return Request Rejected",
                $"Your return request #{returnRequest.ReturnNumber} has been rejected. Reason: {rejectionReason}",
                "Error",
                $"/Return/MyReturns"
            );

            return returnRequest;
        }

        public async Task<ReturnRequest> MarkAsShippedAsync(int returnId, string trackingNumber)
        {
            var returnRequest = await GetReturnRequestByIdAsync(returnId);
            if (returnRequest == null)
                throw new Exception("Return request not found");

            returnRequest.TrackingNumber = trackingNumber;
            returnRequest.ItemsShippedAt = DateTime.Now;
            returnRequest.Status = ReturnStatus.Processing;

            await _context.SaveChangesAsync();

            // Notify supplier
            await _notificationService.SendNotificationAsync(
                returnRequest.Supplier.UserId,
                "📦 Return Items Shipped",
                $"Items for return #{returnRequest.ReturnNumber} have been shipped. Tracking: {trackingNumber}",
                "Info",
                $"/Return/SupplierReturns"
            );

            return returnRequest;
        }

        public async Task<ReturnRequest> MarkAsReceivedAsync(int returnId)
        {
            var returnRequest = await GetReturnRequestByIdAsync(returnId);
            if (returnRequest == null)
                throw new Exception("Return request not found");

            returnRequest.ItemsReceivedAt = DateTime.Now;
            returnRequest.Status = ReturnStatus.Processing;

            await _context.SaveChangesAsync();

            return returnRequest;
        }

        public async Task<ReturnRequest> ProcessRefundAsync(int returnId)
        {
            var returnRequest = await GetReturnRequestByIdAsync(returnId);
            if (returnRequest == null)
                throw new Exception("Return request not found");

            returnRequest.Status = ReturnStatus.Completed;
            returnRequest.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Notify retailer
            await _notificationService.SendNotificationAsync(
                returnRequest.Retailer.UserId,
                "💰 Refund Processed",
                $"Your refund of {returnRequest.RefundAmount:C} for return #{returnRequest.ReturnNumber} has been processed.",
                "Success",
                $"/Return/MyReturns"
            );

            return returnRequest;
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.ReturnRequests
                .CountAsync(r => r.Status == ReturnStatus.Pending);
        }
    }
}