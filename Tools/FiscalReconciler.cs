using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Tools
{
    public class FiscalReconciler
    {
        private readonly ApplicationDbContext _context;

        public FiscalReconciler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> ReconcileAllAsync()
        {
            int platformFixed = 0;
            int payoutFixed = 0;
            int deleted = 0;

            var commissions = await _context.Commissions.ToListAsync();

            foreach (var c in commissions)
            {
                // Fix 1: If OrderPayment has a commission rate != 0 (legacy), fix it for display
                if (c.PaymentType == "OrderPayment" && c.CommissionRate != 0)
                {
                    c.CommissionRate = 0;
                    c.CommissionAmount = c.OrderAmount; // Full amount
                }

                // Fix 2: If a record is clearly a payout (95%) but labeled as PlatformCommission, fix it
                var fivePercent = c.OrderAmount * 0.05m;
                var ninetyFivePercent = c.OrderAmount * 0.95m;

                if (c.PaymentType == "PlatformCommission" && Math.Abs(c.CommissionAmount - ninetyFivePercent) < 0.01m)
                {
                    c.PaymentType = "SupplierPayout";
                    payoutFixed++;
                }

                // Fix 3: If a record is clearly a platform fee (5%) but labeled as OrderPayment or Payout
                if ((c.PaymentType == "OrderPayment" || c.PaymentType == "SupplierPayout") && Math.Abs(c.CommissionAmount - fivePercent) < 0.01m)
                {
                    c.PaymentType = "PlatformCommission";
                    platformFixed++;
                }
                
                // Fix 4: Hardcode 5% standard for all PlatformCommissions
                if (c.PaymentType == "PlatformCommission")
                {
                    c.CommissionRate = 0.05m;
                    c.CommissionAmount = c.OrderAmount * 0.05m;
                }
            }

            await _context.SaveChangesAsync();

            return $"Reconciliation Complete. Fixed {platformFixed} platform fees and {payoutFixed} payouts.";
        }
    }
}
