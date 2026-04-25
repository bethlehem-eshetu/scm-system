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
                .AnyAsync(c => c.OrderId == orderId && c.PurchaseOrderId == purchaseOrderId && c.PaymentType == "PlatformCommission");

            if (exists)
                return null;

            // Fetch dynamic commission rate from SystemConfiguration
            // Default to 5% if not configured
            var configKey = "CommissionBronze"; // Default tier
            var configValue = await _context.SystemConfigurations
                .Where(sc => sc.Key == configKey)
                .Select(sc => sc.Value)
                .FirstOrDefaultAsync() ?? "5.0";

            if (!decimal.TryParse(configValue, out decimal commissionPercentage))
            {
                commissionPercentage = 5.0m;
            }

            decimal commissionRate = commissionPercentage / 100m;
            decimal commissionAmount = orderAmount * commissionRate;

            var commission = new Commission
            {
                OrderId = orderId,
                PurchaseOrderId = purchaseOrderId,
                OrderAmount = orderAmount,
                CommissionRate = commissionRate,
                CommissionAmount = commissionAmount,
                PaymentType = "PlatformCommission",
                Status = "Pending",
                CreatedAt = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7)
            };

            var supplierId = await _context.PurchaseOrders
                .Where(po => po.Id == purchaseOrderId)
                .Select(po => po.SupplierId)
                .FirstOrDefaultAsync();
            
            commission.SupplierId = supplierId;
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
                await FinalizePaymentAsync(commissionId, commission.ChapaTransactionId, verification.Status);
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

        public async Task<bool> FinalizePaymentAsync(int commissionId, string transactionId, string verificationData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var mainCommission = await _context.Commissions
                    .Include(c => c.Order)
                    .Include(c => c.PurchaseOrder)
                    .Include(c => c.Supplier)
                    .Include(c => c.Retailer)
                    .FirstOrDefaultAsync(c => c.Id == commissionId);

                if (mainCommission == null) return false;

                // 1. Idempotency Check
                if (mainCommission.Status == "Paid")
                {
                    await transaction.CommitAsync();
                    return true;
                }

                // 2. Mark Master Commission as Paid
                mainCommission.Status = "Paid";
                mainCommission.PaidAt = DateTime.Now;
                mainCommission.ChapaTransactionId = transactionId;
                mainCommission.PaymentVerificationData = verificationData;

                // 3. Update Order & PurchaseOrder Status
                if (mainCommission.PurchaseOrder != null)
                {
                    mainCommission.PurchaseOrder.PaymentStatus = "Paid";
                    if (mainCommission.PurchaseOrder.Status == "Delivered")
                    {
                        mainCommission.PurchaseOrder.Status = "Completed";
                    }
                }

                if (mainCommission.Order != null)
                {
                    var otherPOs = await _context.PurchaseOrders
                        .Where(p => p.OrderId == mainCommission.OrderId && p.Id != mainCommission.PurchaseOrderId)
                        .ToListAsync();

                    if (otherPOs.All(p => p.PaymentStatus == "Paid"))
                    {
                        mainCommission.Order.PaymentStatus = "Paid";
                        if (mainCommission.Order.OrderStatus == "Delivered")
                        {
                            mainCommission.Order.OrderStatus = "Completed";
                        }
                    }
                }

                // 4. Automated Commission Split
                if (mainCommission.PaymentType == "OrderPayment")
                {
                    var supplier = mainCommission.Supplier;
                    decimal commissionRate = supplier != null 
                        ? (supplier.CommissionRate > 0 ? supplier.CommissionRate : Supplier.GetRateByTier(supplier.CommissionTier)) 
                        : 5.0m;

                    var platformCommAmount = mainCommission.OrderAmount * (commissionRate / 100);

                    // Prevents redudant splits in case of race between verify and webhook
                    var splitExists = await _context.Commissions.AnyAsync(c => 
                        c.PurchaseOrderId == mainCommission.PurchaseOrderId && 
                        (c.PaymentType == "PlatformCommission" || c.PaymentType == "SupplierPayout"));

                    if (!splitExists)
                    {
                        var platformComm = new Commission
                        {
                            PurchaseOrderId = mainCommission.PurchaseOrderId,
                            OrderId = mainCommission.OrderId,
                            SupplierId = mainCommission.SupplierId,
                            RetailerId = mainCommission.RetailerId,
                            OrderAmount = mainCommission.OrderAmount,
                            CommissionRate = commissionRate / 100,
                            CommissionAmount = platformCommAmount,
                            PaymentType = "PlatformCommission",
                            Status = "Paid",
                            CreatedAt = DateTime.Now,
                            PaidAt = DateTime.Now,
                            Notes = $"Platform commission ({commissionRate}%) automatically deducted"
                        };
                        _context.Commissions.Add(platformComm);

                        var payoutAmount = mainCommission.OrderAmount - platformCommAmount;
                        var supplierPayout = new Commission
                        {
                            PurchaseOrderId = mainCommission.PurchaseOrderId,
                            OrderId = mainCommission.OrderId,
                            SupplierId = mainCommission.SupplierId,
                            RetailerId = mainCommission.RetailerId,
                            OrderAmount = mainCommission.OrderAmount,
                            CommissionAmount = payoutAmount,
                            PaymentType = "SupplierPayout",
                            Status = "Pending",
                            CreatedAt = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(7),
                            SupplierPayoutAmount = payoutAmount,
                            SupplierPayoutStatus = "Pending",
                            Notes = $"Net earnings generated from Order #{mainCommission.Order?.OrderNumber}"
                        };
                        _context.Commissions.Add(supplierPayout);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (mainCommission.Retailer?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        mainCommission.Retailer.UserId,
                        "Payment Confirmed",
                        $"Your payment for Order #{mainCommission.Order?.OrderNumber} was successful.",
                        "Success",
                        "/Retailer/OrderTracking"
                    );
                }

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}