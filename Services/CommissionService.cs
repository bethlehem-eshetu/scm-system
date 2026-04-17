using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IChapaService _chapaService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public CommissionService(
            ApplicationDbContext context,
            IChapaService chapaService,
            INotificationService notificationService,
            IConfiguration configuration)
        {
            _context = context;
            _chapaService = chapaService;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        public async Task<Commission> CreateCommissionAsync(int orderId, decimal orderAmount, int purchaseOrderId)
        {
            // ✅ Prevent duplicate commissions
            var exists = await _context.Commissions
                .AnyAsync(c => c.OrderId == orderId && c.PurchaseOrderId == purchaseOrderId);

            if (exists)
                return null;

            // Example: 10% commission
            decimal commissionRate = 0.10m;
            decimal commissionAmount = orderAmount * commissionRate;

            var commission = new Commission
            {
                OrderId = orderId,
                PurchaseOrderId = purchaseOrderId, // ✅ IMPORTANT FIX
                OrderAmount = orderAmount,
                CommissionRate = commissionRate,
                CommissionAmount = commissionAmount,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7)
            };
            Console.WriteLine("🔥 Commission method HIT!");
            Console.WriteLine($"💰 Creating commission for Order {orderId}, PO {purchaseOrderId}, Amount {orderAmount}");
            _context.Commissions.Add(commission);
            await _context.SaveChangesAsync();

            return commission;
        }

        public async Task<Commission> GetCommissionByIdAsync(int id)
        {
            return await _context.Commissions
                .Include(c => c.Order)
                .Include(c => c.Supplier)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Commission>> GetSupplierCommissionsAsync(int supplierId)
        {
            return await _context.Commissions
                .Include(c => c.Order)
                .Where(c => c.SupplierId == supplierId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Commission>> GetPendingCommissionsAsync()
        {
            return await _context.Commissions
                .Include(c => c.Supplier)
                    .ThenInclude(s => s.User)
                .Where(c => c.Status == "Pending" && (c.DueDate == null || c.DueDate > DateTime.Now))
                .OrderBy(c => c.DueDate)
                .ToListAsync();
        }

        public async Task<Commission> ProcessPaymentAsync(int commissionId, string paymentUrl)
        {
            var commission = await GetCommissionByIdAsync(commissionId);
            if (commission == null)
                throw new Exception("Commission not found");

            if (commission.Status != "Pending")
                throw new Exception("Commission is already processed");

            commission.Status = "Processing";
            commission.ChapaPaymentUrl = paymentUrl;
            await _context.SaveChangesAsync();

            return commission;
        }

        public async Task<Commission> VerifyPaymentAsync(int commissionId)
        {
            var commission = await GetCommissionByIdAsync(commissionId);
            if (commission == null)
                throw new Exception("Commission not found");

            if (string.IsNullOrEmpty(commission.ChapaTransactionId))
                throw new Exception("No transaction ID found");

            var verification = await _chapaService.VerifyPaymentAsync(commission.ChapaTransactionId);

            if (verification.Success && verification.Status == "success")
            {
                commission.Status = "Paid";
                commission.PaidAt = DateTime.Now;

                // ✅ Update PurchaseOrder PaymentStatus
                if (commission.PurchaseOrder != null)
                {
                    commission.PurchaseOrder.PaymentStatus = "Paid";
                    _context.PurchaseOrders.Update(commission.PurchaseOrder);
                }

                // Notify supplier
                if (commission.Supplier?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        commission.Supplier.UserId,
                        "✅ Payment Successful",
                        $"Your commission payment of {commission.CommissionAmount:C} for Order #{commission.Order?.OrderNumber} has been confirmed.",
                        "Success",
                        $"/Supplier/Payments"
                    );
                }

                await _context.SaveChangesAsync();
            }

            return commission;
        }

        public async Task<decimal> GetTotalCommissionsEarnedAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Commissions.Where(c => c.Status == "Paid");

            if (fromDate.HasValue)
                query = query.Where(c => c.PaidAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(c => c.PaidAt <= toDate.Value);

            return await query.SumAsync(c => c.CommissionAmount);
        }

        public async Task<decimal> GetPendingCommissionsTotalAsync()
        {
            return await _context.Commissions
                .Where(c => c.Status == "Pending" && (c.DueDate == null || c.DueDate > DateTime.Now))
                .SumAsync(c => c.CommissionAmount);
        }
    }
}