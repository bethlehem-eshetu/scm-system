using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;
using System.Text;

namespace SCM_System.Controllers
{
    public class AdminPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommissionService _commissionService;

        public AdminPaymentController(ApplicationDbContext context, ICommissionService commissionService)
        {
            _context = context;
            _commissionService = commissionService;
        }

        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;
            var user = _context.Users.Find(userId);
            return user != null && user.Role == "Admin";
        }

        public async Task<IActionResult> CommissionDashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var commissions = await _context.Commissions
                .Include(c => c.Order)
                .Include(c => c.Supplier)
                .Include(c => c.Retailer)
                .Where(c => c.PaymentType == "PlatformCommission")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var viewModel = new CommissionReportViewModel
            {
                TotalRevenue = commissions.Where(c => c.Status == "Paid").Sum(c => c.OrderAmount),
                TotalCommissions = commissions.Where(c => c.Status == "Paid").Sum(c => c.CommissionAmount),
                PendingPayouts = await _context.Commissions
                    .Where(c => c.PaymentType == "SupplierPayout" && c.Status == "Pending")
                    .SumAsync(c => c.CommissionAmount),
                
                RecentTransactions = commissions.Take(20).Select(c => new CommissionHistoryItem
                {
                    Id = c.Id,
                    OrderNumber = c.Order?.OrderNumber ?? "N/A",
                    SupplierName = c.Supplier?.CompanyName ?? "Unknown",
                    RetailerName = c.Retailer?.BusinessName ?? "Unknown",
                    Amount = c.OrderAmount,
                    CommissionFee = c.CommissionAmount,
                    Rate = (c.CommissionRateAtTransaction > 0 ? c.CommissionRateAtTransaction : c.CommissionRate * 100),
                    Status = c.Status,
                    Date = c.CreatedAt
                }).ToList(),

                TierStats = await _context.Suppliers
                    .GroupBy(s => s.CommissionTier)
                    .Select(g => new TierStatistic
                    {
                        TierName = g.Key,
                        SupplierCount = g.Count(),
                        TotalRevenue = _context.Commissions
                            .Where(c => c.Supplier.CommissionTier == g.Key && c.PaymentType == "PlatformCommission" && c.Status == "Paid")
                            .Sum(c => (decimal?)c.OrderAmount) ?? 0,
                        TotalCommission = _context.Commissions
                            .Where(c => c.Supplier.CommissionTier == g.Key && c.PaymentType == "PlatformCommission" && c.Status == "Paid")
                            .Sum(c => (decimal?)c.CommissionAmount) ?? 0
                    }).ToListAsync(),

                RevenueByMonth = commissions.Where(c => c.Status == "Paid" && c.CreatedAt > DateTime.Now.AddMonths(-6))
                    .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new ChartDataPoint
                    {
                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Value = g.Sum(c => c.CommissionAmount)
                    }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ExportCommissionCsv()
        {
            if (!IsAdmin()) return Unauthorized();

            var commissions = await _context.Commissions
                .Include(c => c.Order)
                .Include(c => c.Supplier)
                .Where(c => c.PaymentType == "PlatformCommission")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Date,Order #,Supplier,Order Amount,Commission Rate %,Commission Amount,Status");

            foreach (var c in commissions)
            {
                var rate = (c.CommissionRateAtTransaction > 0 ? c.CommissionRateAtTransaction : c.CommissionRate * 100);
                sb.AppendLine($"{c.Id},{c.CreatedAt:yyyy-MM-dd HH:mm},{c.Order?.OrderNumber},{c.Supplier?.CompanyName},{c.OrderAmount},{rate:F2},{c.CommissionAmount},{c.Status}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"Commission_Report_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
