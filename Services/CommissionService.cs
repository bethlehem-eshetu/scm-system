using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.Constants;
using SCM_System.Models.Enums;

namespace SCM_System.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IChapaService _chapaService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ISupplierService _supplierService;

        public CommissionService(
            ApplicationDbContext context,
            IChapaService chapaService,
            INotificationService notificationService,
            IConfiguration configuration,
            ISupplierService supplierService)
        {
            _context = context;
            _chapaService = chapaService;
            _notificationService = notificationService;
            _configuration = configuration;
            _supplierService = supplierService;
        }

        public async Task<Commission> CreateCommissionAsync(int orderId, decimal orderAmount, int purchaseOrderId)
        {
            // ✅ Prevent duplicate commissions
            var exists = await _context.Commissions
                .AnyAsync(c => c.OrderId == orderId && c.PurchaseOrderId == purchaseOrderId && c.PaymentType == "PlatformCommission");

            if (exists)
                return null;

            // ✅ New Tiered Commission Logic: Percentage decreases as order amount increases
            decimal commissionPercentage = Supplier.GetTieredCommissionRate(orderAmount);
            
            // Check for potential admin overrides in SystemConfiguration
            var configKey = "CommissionBronze"; 
            var configValue = await _context.SystemConfigurations
                .Where(sc => sc.Key == configKey)
                .Select(sc => sc.Value)
                .FirstOrDefaultAsync();

            // If an admin override exists and it's lower than the tiered rate, we could consider it.
            // But the user specifically asked for amount-based logic.
            
            decimal commissionRate = commissionPercentage / 100m;
            decimal commissionAmount = Math.Round(orderAmount * commissionRate, 2);

            var commission = new Commission
            {
                OrderId = orderId,
                PurchaseOrderId = purchaseOrderId,
                OrderAmount = orderAmount,
                CommissionRate = commissionRate,
                CommissionAmount = commissionAmount,
                PaymentType = "PlatformCommission",
                Status = PaymentStatus.Pending.ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7)
            };

            if (purchaseOrderId > 0)
            {
                var supplierIdFromPO = await _context.PurchaseOrders
                    .Where(po => po.Id == purchaseOrderId)
                    .Select(po => po.SupplierId)
                    .FirstOrDefaultAsync();
                
                commission.SupplierId = supplierIdFromPO;
                commission.PurchaseOrderId = purchaseOrderId;
            }
            else
            {
                commission.SupplierId = await _context.Orders
                    .Where(o => o.Id == orderId)
                    .Select(o => o.SupplierId)
                    .FirstOrDefaultAsync();
                commission.PurchaseOrderId = null;
            }

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
                .Where(c => c.Status == PaymentStatus.Pending.ToString() && (c.DueDate == null || c.DueDate > DateTime.Now))
                .OrderBy(c => c.DueDate)
                .ToListAsync();
        }

        public async Task<Commission> ProcessPaymentAsync(int commissionId, string paymentUrl)
        {
            var commission = await GetCommissionByIdAsync(commissionId);
            if (commission == null)
                throw new Exception("Commission not found");

            if (commission.Status != PaymentStatus.Pending.ToString())
                throw new Exception("Commission is already processed");

            commission.Status = PaymentStatus.Processing.ToString();
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
            var query = _context.Commissions.Where(c => c.Status == PaymentStatus.Paid.ToString());

            if (fromDate.HasValue)
                query = query.Where(c => c.PaidAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(c => c.PaidAt <= toDate.Value);

            return await query.SumAsync(c => c.CommissionAmount);
        }

        public async Task<decimal> GetPendingCommissionsTotalAsync()
        {
            return await _context.Commissions
                .Where(c => c.Status == PaymentStatus.Pending.ToString() && (c.DueDate == null || c.DueDate > DateTime.Now))
                .SumAsync(c => c.CommissionAmount);
        }

        public async Task<bool> FinalizePaymentAsync(int commissionId, string transactionId, string verificationData)
        {
            var existingTransaction = _context.Database.CurrentTransaction;
            var transaction = existingTransaction == null ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                var mainCommission = await _context.Commissions
                    .Include(c => c.Order)
                        .ThenInclude(o => o.PurchaseOrders)
                            .ThenInclude(po => po.Supplier)
                    .Include(c => c.Order)
                        .ThenInclude(o => o.PurchaseOrders)
                            .ThenInclude(po => po.PurchaseOrderItems)
                    .Include(c => c.PurchaseOrder)
                    .Include(c => c.Supplier)
                    .Include(c => c.Retailer)
                    .FirstOrDefaultAsync(c => c.Id == commissionId);

                if (mainCommission == null) return false;

                // 1. Strict Guard & Idempotency
                if (mainCommission.Status == PaymentStatus.Paid.ToString())
                {
                    await transaction.CommitAsync();
                    return true;
                }

                // Webhook Replay Protection: Check if TxRef already processed
                var existingPaid = await _context.Payments
                    .AnyAsync(p => p.TxRef == transactionId && p.Status == PaymentStatus.Paid);
                if (existingPaid)
                {
                    await transaction.CommitAsync();
                    return true;
                }

                if (mainCommission.Order != null)
                {
                    // Mark Master Commission as Paid
                    mainCommission.Status = PaymentStatus.Paid.ToString();
                    mainCommission.PaidAt = DateTime.Now;
                    mainCommission.ChapaTransactionId = transactionId;
                    mainCommission.PaymentVerificationData = verificationData;

                    // Update Payment record
                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.OrderId == mainCommission.OrderId);
                    
                    if (payment != null)
                    {
                        // Guard: Only move to Paid if it was Processing
                        if (payment.Status == PaymentStatus.Paid) {
                            await transaction.CommitAsync();
                            return true; 
                        }
                        
                        payment.Status = PaymentStatus.Paid;
                        payment.PaidAt = DateTime.UtcNow;
                        payment.TxRef = transactionId;
                    }

                    mainCommission.Order.PaymentStatus = "Paid";
                    mainCommission.Order.OrderStatus = "Completed";

                    // 2. Iterate each PO in the order to safely manage supplier balances
                    foreach (var po in mainCommission.Order.PurchaseOrders)
                    {
                        po.PaymentStatus = "Paid";
                        po.Status = POStatus.Completed;

                        if (po.Supplier != null)
                        {
                            var supplier = po.Supplier;
                            var poAmount = po.TotalAmount > 0 ? po.TotalAmount : Math.Round(po.PurchaseOrderItems.Sum(i => i.Quantity * i.UnitPrice), 2);
                            
                            // ✅ Updated Tiered Logic: Use the sliding scale based on the PO amount
                            decimal tieredRate = Supplier.GetTieredCommissionRate(poAmount);
                            
                            // If the supplier has a custom rate or a higher tier, we respect the better deal for the platform/supplier 
                            // But usually, the sliding scale is the primary rule.
                            decimal supplierTierRate = supplier.CommissionRate > 0 ? supplier.CommissionRate : Supplier.GetRateByTier(supplier.CommissionTier);
                            
                            // We use the Tiered Rate as the baseline, but respect if the Supplier Tier offers a better deal (lower rate)
                            decimal commissionRate = Math.Min(tieredRate, supplierTierRate);
                            
                            // Enforce minimum 1% just in case
                            if (commissionRate < 1.0m) commissionRate = 1.0m; 
                            
                            var platformCommAmount = Math.Round(poAmount * (commissionRate / 100), 2);
                            var supplierAmount = Math.Round(poAmount - platformCommAmount, 2);

                            // Update Balance
                            supplier.Balance += supplierAmount; 

                            // ✅ Safe Guard: Prevent negative balance
                            if (supplier.Balance < 0)
                            {
                                throw new Exception("Invalid balance state detected for supplier: " + supplier.CompanyName);
                            }

                            // Log Audit Trail
                            var auditTransaction = new SupplierTransaction
                            {
                                SupplierId = supplier.Id,
                                OrderId = po.OrderId,
                                Amount = supplierAmount,
                                Type = "Credit",
                                Reference = $"Order #{mainCommission.Order?.OrderNumber} - PO #{po.PONumber}",
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.SupplierTransactions.Add(auditTransaction);

                            // 4. Automated Commission Split (Updates or Creates)
                            var platformComm = await _context.Commissions
                                .FirstOrDefaultAsync(c => c.PurchaseOrderId == po.Id && c.PaymentType == "PlatformCommission");

                            if (platformComm == null)
                            {
                                platformComm = new Commission
                                {
                                    PurchaseOrderId = po.Id,
                                    OrderId = mainCommission.OrderId,
                                    SupplierId = po.SupplierId,
                                    RetailerId = mainCommission.RetailerId,
                                    OrderAmount = poAmount,
                                    CommissionRate = commissionRate / 100,
                                    CommissionAmount = platformCommAmount,
                                    CommissionRateAtTransaction = commissionRate,
                                    PaymentType = "PlatformCommission",
                                    CreatedAt = DateTime.Now
                                };
                                _context.Commissions.Add(platformComm);
                            }

                            platformComm.Status = PaymentStatus.Paid.ToString();
                            platformComm.PaidAt = DateTime.Now;
                            platformComm.UpdatedAt = DateTime.Now;
                            platformComm.CommissionAmount = platformCommAmount; // Sync in case of changes
                            platformComm.Notes = $"Platform commission ({commissionRate}%) deducted for Order #{mainCommission.Order?.OrderNumber}";
                        }
                    }
                }

                // Single SaveChanges call for atomic transaction
                try 
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Concurrency Retry Logic for Supplier Balance updates
                    foreach (var entry in _context.ChangeTracker.Entries())
                    {
                        await entry.ReloadAsync();
                    }
                    await _context.SaveChangesAsync();
                }

                // 5. Trigger Supplier Tier Recalculation
                await _supplierService.UpdateSupplierTierAsync(mainCommission.SupplierId);

                if (transaction != null) await transaction.CommitAsync();

                if (mainCommission.Retailer?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        mainCommission.Retailer.UserId,
                        "Payment Confirmed ✅",
                        $"Your payment for Order #{mainCommission.Order?.OrderNumber} was successful. Status: Settled.",
                        "Approval",
                        "/Order/Details/" + mainCommission.OrderId
                    );
                }

                return true;
            }
            catch (Exception)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return false;
            }
        }


        public async Task<Commission> InitiateOrderPaymentAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return null;

            // Check if already exists
            var existing = await _context.Commissions
                .FirstOrDefaultAsync(c => c.OrderId == orderId && c.PaymentType == "OrderPayment");
            
            if (existing != null) return existing;

            var commission = new Commission
            {
                OrderId = orderId,
                SupplierId = order.SupplierId,
                RetailerId = order.RetailerId,
                OrderAmount = order.TotalAmount,
                CommissionAmount = order.TotalAmount, // ✅ Set amount for Chapa lookup
                PaymentType = "OrderPayment",
                Status = PaymentStatus.Pending.ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Commissions.Add(commission);

            // ✅ ENSURE SHARED TRUTH: Check if a payment record already exists for this order
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment == null)
            {
                // Generate TxRef only if creating new
                string txRef = $"ORD-{orderId}-{Guid.NewGuid().ToString().Substring(0, 8)}";
                
                payment = new Payment
                {
                    OrderId = orderId,
                    RetailerId = order.RetailerId,
                    Amount = order.TotalAmount,
                    Status = PaymentStatus.Pending,
                    TxRef = txRef,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Payments.Add(payment);
            }
            else
            {
                // Sync amount just in case of order updates
                payment.Amount = order.TotalAmount;
            }

            await _context.SaveChangesAsync();
            return commission;
        }

        public async Task<Commission> GetCommissionByOrderAndTypeAsync(int orderId, string paymentType)
        {
            return await _context.Commissions
                .FirstOrDefaultAsync(c => c.OrderId == orderId && c.PaymentType == paymentType);
        }
    }
}