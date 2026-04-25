using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;
using SCM_System.Models.ViewModels;

namespace SCM_System.Controllers
{
    public class CommissionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommissionService _commissionService;
        private readonly IChapaService _chapaService;
        private readonly INotificationService _notificationService;
        private readonly IOrderService _orderService;

        public CommissionController(
            ApplicationDbContext context,
            ICommissionService commissionService,
            IChapaService chapaService,
            INotificationService notificationService,
            IOrderService orderService)
        {
            _context = context;
            _commissionService = commissionService;
            _chapaService = chapaService;
            _notificationService = notificationService;
            _orderService = orderService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "";
        }

        // GET: /Commission/MyCommissions (Supplier view)
        public async Task<IActionResult> MyCommissions()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (supplier == null)
                return RedirectToAction("AccessDenied", "Home");

            // ✅ ONLY show PlatformCommission (5% - what supplier owes to admin)
            var commissions = await _context.Commissions
                .Include(c => c.Order)
                .Include(c => c.PurchaseOrder)
                .Where(c => c.SupplierId == supplier.Id && c.PaymentType == "PlatformCommission")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var totalPending = commissions.Where(c => c.Status == "Pending").Sum(c => c.CommissionAmount);
            var totalPaid = commissions.Where(c => c.Status == "Paid").Sum(c => c.CommissionAmount);

            ViewBag.TotalPending = totalPending;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.SupplierName = supplier.CompanyName;

            return View(commissions);
        }

        // GET: /Commission/Pay/{id}
        public async Task<IActionResult> Pay(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var commission = await _commissionService.GetCommissionByIdAsync(id);
            if (commission == null)
                return NotFound();

            // Verify supplier owns this commission
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (supplier == null || commission.SupplierId != supplier.Id)
                return Unauthorized();

            if (commission.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This commission has already been processed.";
                return RedirectToAction("MyCommissions");
            }

            // ✅ STEP 1: Create tx_ref FIRST (IMPORTANT)
            var txRef = $"COMM-{commission.Id}-{DateTime.Now.Ticks}";

            // Get user
            var user = await _context.Users.FindAsync(currentUserId);

            // ✅ STEP 2: Build request correctly
            var request = new ChapaPaymentRequest
            {
                Amount = commission.CommissionAmount,
                Currency = "ETB",
                Email = user?.Email ?? "",
                FirstName = user?.FullName?.Split(' ').FirstOrDefault() ?? "",
                LastName = user?.FullName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",

                TxRef = txRef,

                // ✅ BOTH point to Callback
                CallbackUrl = $"{Request.Scheme}://{Request.Host}/Commission/Callback",
                ReturnUrl = $"{Request.Scheme}://{Request.Host}/Commission/Callback?tx_ref={txRef}",

                CustomizationTitle = "SCM Commission",
                CustomizationDescription = $"Commission Payment ID {commission.Id}"
            };

            // 🔍 Debug (optional but useful)
            Console.WriteLine($"TX_REF: {txRef}");
            Console.WriteLine($"RETURN URL: {request.ReturnUrl}");

            var result = await _chapaService.InitializePaymentAsync(request);

            if (result.Success)
            {
                // ✅ Save tx_ref properly
                commission.ChapaTransactionId = txRef; // 🔥 IMPORTANT: use YOUR txRef, not result.TransactionId
                commission.ChapaPaymentUrl = result.PaymentUrl;
                commission.Status = "Processing";

                await _context.SaveChangesAsync();

                return Redirect(result.PaymentUrl);
            }

            TempData["ErrorMessage"] = $"Failed to initialize payment: {result.Message}";
            return RedirectToAction("MyCommissions");
        }

        // GET: /Commission/Callback (Chapa callback)
        public async Task<IActionResult> Callback()
        {
            var tx_ref = Request.Query["tx_ref"].ToString();
            var status = Request.Query["status"].ToString();
            var transaction_id = Request.Query["transaction_id"].ToString();

            Console.WriteLine($"===== CHAPA CALLBACK =====");
            Console.WriteLine($"tx_ref: {tx_ref}");
            Console.WriteLine($"status: {status}");
            Console.WriteLine($"transaction_id: {transaction_id}");

            if (string.IsNullOrEmpty(tx_ref))
            {
                TempData["ErrorMessage"] = "Invalid payment reference.";
                return RedirectToAction("MyCommissions");
            }

            // 🔥 VERIFY WITH CHAPA
            var verifyResult = await _chapaService.VerifyPaymentAsync(tx_ref);

            if (!verifyResult.Success || verifyResult.Status != "success")
            {
                TempData["ErrorMessage"] = "Payment verification failed.";
                return RedirectToAction("MyCommissions");
            }

            // 🔥 FIND COMMISSION
            var commission = await _context.Commissions
                .FirstOrDefaultAsync(c => c.ChapaTransactionId == tx_ref);

            if (commission == null)
            {
                TempData["ErrorMessage"] = "Commission not found.";
                return RedirectToAction("MyCommissions");
            }

            // 🔥 UPDATE STATUS
            commission.Status = "Paid";
            commission.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment completed successfully!";

            return RedirectToAction("MyCommissions");
        }

        // POST: /Commission/Webhook (Chapa webhook)
        [HttpPost]
        public async Task<IActionResult> Webhook([FromBody] ChapaWebhookPayload payload)
        {
            if (payload == null)
                return BadRequest();

            await _chapaService.WebhookHandlerAsync(payload.tx_ref, payload.status);

            var commission = await _context.Commissions
                .FirstOrDefaultAsync(c => c.ChapaTransactionId == payload.tx_ref);

            if (commission != null)
            {
                var verifyResult = await _chapaService.VerifyPaymentAsync(payload.tx_ref);

                if (verifyResult.Success && verifyResult.Status == "success")
                {
                    commission.Status = "Paid";
                    commission.PaidAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
            }

            return Ok();
        }

        // GET: /Commission/Admin/All (Admin view)
        public async Task<IActionResult> AdminAll()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != "Admin")
                return RedirectToAction("AccessDenied", "Home");

            var commissions = await _context.Commissions
                .Include(c => c.Supplier)
                    .ThenInclude(s => s.User)
                .Include(c => c.Order)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.TotalEarned = await _commissionService.GetTotalCommissionsEarnedAsync();
            ViewBag.TotalPending = await _commissionService.GetPendingCommissionsTotalAsync();

            return View(commissions);
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(currentUserId);
            if (user?.Role != "Admin")
                return RedirectToAction("AccessDenied", "Home");

            var viewModel = new CommissionDashboardViewModel
            {
                TotalEarned = await _context.Commissions.Where(c => c.Status == "Paid" && c.PaymentType == "PlatformCommission").SumAsync(c => c.CommissionAmount),
                TotalPending = await _context.Commissions.Where(c => c.Status == "Pending").SumAsync(c => c.CommissionAmount),
                TotalSettled = await _context.Commissions.Where(c => c.Status == "Paid" && c.PaymentType == "SupplierPayout").SumAsync(c => c.CommissionAmount),
                PendingCount = await _context.Commissions.CountAsync(c => c.Status == "Pending"),
                ActiveSuppliersCount = await _context.Suppliers.CountAsync()
            };

            // Mock Chart Data for the last 7 days
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                viewModel.ChartLabels.Add(date.ToString("MMM dd"));
                viewModel.ChartData.Add(await _context.Commissions
                    .Where(c => c.PaidAt.HasValue && c.PaidAt.Value.Date == date.Date && c.Status == "Paid")
                    .SumAsync(c => c.CommissionAmount));
            }

            // Tier Distribution
            var distribution = await _context.Suppliers
                .GroupBy(s => s.CommissionTier ?? "Bronze")
                .Select(g => new { Tier = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in distribution)
            {
                viewModel.TierLabels.Add(item.Tier);
                viewModel.TierData.Add(item.Count);
                viewModel.TierDistribution[item.Tier] = item.Count;
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> FixMissingCommissions()
        {
            var count = await _orderService.CreateMissingCommissionsForDeliveredOrders();
            return Json(new { success = true, message = $"Created {count} missing commissions" });
        }
    }

  

    public class ChapaWebhookPayload
    {
        public string tx_ref { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string transaction_id { get; set; } = string.Empty;
    }
}