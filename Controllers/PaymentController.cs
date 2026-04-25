using Microsoft.AspNetCore.Mvc;
using SCM_System.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using SCM_System.Models.Entities;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaymentController> _logger;
        private readonly IEmailService _emailService;
        private readonly ICommissionService _commissionService;

        public PaymentController(
            ApplicationDbContext context, 
            IConfiguration config, 
            IHttpClientFactory httpClientFactory, 
            ILogger<PaymentController> logger,
            IEmailService emailService,
            ICommissionService commissionService)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _emailService = emailService;
            _commissionService = commissionService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        [HttpPost]
        public async Task<IActionResult> InitializePayment(int commissionId)
        {
            try
            {
                var commission = await _context.Commissions
                    .Include(c => c.Supplier)
                        .ThenInclude(s => s.User)
                    .Include(c => c.Retailer)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(c => c.Id == commissionId);

                if (commission == null)
                    return NotFound();

                if (commission.Status == "Paid")
                {
                    TempData["SuccessMessage"] = "This payment has already been successfully processed.";
                    return RedirectToAction("MyPayments", "Payment");
                }

                // ✅ STEP 1: create tx_ref FIRST
                var tx_ref = $"PAY-{commission.Id}-{DateTime.Now.Ticks}";

                var secretKey = _config["Chapa:SecretKey"];
                var baseUrl = _config["Chapa:BaseUrl"] ?? "https://api.chapa.co/v1";

                string email, firstName, lastName;

                string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/Verify?tx_ref={tx_ref}";

                if (commission.PaymentType == "OrderPayment")
                {
                    email = commission.Retailer?.User?.Email ?? "retailer@example.com";
                    firstName = commission.Retailer?.BusinessName ?? "Retailer";
                    lastName = "Retailer";
                }
                else
                {
                    email = commission.Supplier?.User?.Email ?? "supplier@example.com";
                    firstName = commission.Supplier?.CompanyName ?? "Supplier";
                    lastName = "Supplier";
                }

                var payload = new
                {
                    amount = commission.CommissionAmount.ToString("0.00"),
                    currency = "ETB",
                    email = email,
                    first_name = firstName,
                    last_name = lastName,
                    tx_ref = tx_ref,

                    // ✅ IMPORTANT: both must lead to Verify
                    callback_url = $"{Request.Scheme}://{Request.Host}/Payment/Verify?tx_ref={tx_ref}",
                    return_url = returnUrl,

                    customization = new
                    {
                        title = commission.PaymentType == "OrderPayment"
                            ? "Order Payment"
                            : "Platform Commission",

                        description = $"Payment {commission.CommissionAmount} ETB"
                    }
                };

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{baseUrl}/transaction/initialize", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusElement) &&
                    statusElement.GetString() == "success")
                {
                    var checkoutUrl = root.GetProperty("data").GetProperty("checkout_url").GetString();

                    // ✅ IMPORTANT FIX: store YOUR tx_ref (not Chapa response)
                    commission.ChapaTransactionId = tx_ref;
                    commission.Status = "Processing";
                    commission.PaymentRequestData = responseContent;

                    await _context.SaveChangesAsync();

                    return Redirect(checkoutUrl);
                }

                var message = root.TryGetProperty("message", out var msg)
                    ? msg.GetString()
                    : "Payment initialization failed";

                TempData["ErrorMessage"] = $"Payment failed: {message}";
                return RedirectToAction("MyPayments", "Payment");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("MyPayments", "Payment");
            }
        }

        // GET: /Payment/Verify (this is called by Chapa after payment)
        [HttpGet]
        public async Task<IActionResult> Verify(string tx_ref)
        {
            Console.WriteLine($"===== VERIFY CALLBACK RECEIVED =====");
            Console.WriteLine($"tx_ref: {tx_ref}");

            Commission? commission = null;
            try
            {
                commission = await _context.Commissions
                    .Include(c => c.Retailer)
                    .Include(c => c.PurchaseOrder)  // ✅ Added: Include PurchaseOrder
                    .FirstOrDefaultAsync(c => c.ChapaTransactionId == tx_ref);

                if (commission == null)
                {
                    Console.WriteLine($"❌ Commission not found for tx_ref: {tx_ref}");
                    TempData["ErrorMessage"] = "Commission not found";
                    return RedirectToAction("MyPayments", "Payment");
                }

                if (commission.Status == "Paid")
                {
                    Console.WriteLine($"ℹ️ Payment already processed - tx_ref: {tx_ref}");
                    TempData["SuccessMessage"] = "Payment has already been successfully processed.";
                    return RedirectToAction("MyPayments", "Payment");
                }

                Console.WriteLine($"✅ Commission found - ID: {commission.Id}, Current Status: {commission.Status}");

                var secretKey = _config["Chapa:SecretKey"];
                var baseUrl = _config["Chapa:BaseUrl"];

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

                var response = await client.GetAsync($"{baseUrl}/transaction/verify/{tx_ref}");
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Verification Response: {responseContent}");

                commission.PaymentVerificationData = responseContent;

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("status", out var statusElement))
                    {
                        var paymentStatus = statusElement.GetString();
                        Console.WriteLine($"Payment Status from Chapa: {paymentStatus}");

                        if (paymentStatus == "success")
                        {
                            // Centralized finalization logic
                            await _commissionService.FinalizePaymentAsync(commission.Id, tx_ref, responseContent);
                            TempData["SuccessMessage"] = $"Payment of {commission.CommissionAmount:C} completed successfully!";
                        }
                        else
                        {
                            commission.Status = "Failed";
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"❌ Commission {commission.Id} marked as FAILED");
                            TempData["ErrorMessage"] = "Payment verification failed.";
                        }
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (commission != null)
                {
                    // Reload the entity to see if it was updated by the webhook concurrently
                    await _context.Entry(commission).ReloadAsync();
                    if (commission.Status == "Paid")
                    {
                        Console.WriteLine($"ℹ️ Concurrency handled: Payment was already marked as PAID by another process.");
                        TempData["SuccessMessage"] = "Payment confirmed.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "A concurrency error occurred while updating the payment.";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Verification error: {ex.Message}");
                TempData["ErrorMessage"] = $"Verification error: {ex.Message}";
            }

            return RedirectToAction("MyPayments", "Payment");
        }

        public async Task<IActionResult> MyPayments(string dateRange = "all", string status = "all", string search = "")
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null) return RedirectToAction("Login", "Account");

            IQueryable<Commission> query = _context.Commissions
                .Include(c => c.Order)
                .Include(c => c.PurchaseOrder);

            if (user.Role == "Retailer")
            {
                var retailer = await _context.Retailers
                    .FirstOrDefaultAsync(r => r.UserId == currentUserId);

                if (retailer != null)
                {
                    query = query.Where(c => c.RetailerId == retailer.Id && c.PaymentType == "OrderPayment");
                }
            }
            else if (user.Role == "Supplier")
            {
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId);

                if (supplier != null)
                {
                    // Suppliers see their payouts and the platform commissions deducted from them
                    query = query.Where(c => c.SupplierId == supplier.Id && (c.PaymentType == "SupplierPayout" || c.PaymentType == "PlatformCommission" || c.PaymentType == "OrderPayment"));
                }
            }

            // Apply Filters
            if (status != "all")
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => (c.Order != null && c.Order.OrderNumber.Contains(search)) || 
                                       (c.PurchaseOrder != null && c.PurchaseOrder.PONumber.Contains(search)));
            }

            if (dateRange != "all")
            {
                var now = DateTime.Now;
                if (dateRange == "7days") query = query.Where(c => c.CreatedAt >= now.AddDays(-7));
                else if (dateRange == "30days") query = query.Where(c => c.CreatedAt >= now.AddDays(-30));
                else if (dateRange == "month") query = query.Where(c => c.CreatedAt.Month == now.Month && c.CreatedAt.Year == now.Year);
            }

            var payments = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            ViewBag.UserRole = user.Role;
            return View(payments);
        }

        // ✅ Result Page
        public IActionResult Result()
        {
            return View();
        }

        // GET: /Payment/SupplierEarnings
        public async Task<IActionResult> SupplierEarnings()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null || user.Role != "Supplier") return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (supplier == null) return NotFound();

            var earnings = await _context.Commissions
                .Include(c => c.PurchaseOrder)
                .Where(c => c.SupplierId == supplier.Id && (c.PaymentType == "PlatformCommission" || c.PaymentType == "SupplierPayout"))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.TotalCleared = earnings.Where(c => c.PaymentType == "SupplierPayout" && c.Status == "Paid").Sum(c => c.CommissionAmount);
            ViewBag.TotalPending = earnings.Where(c => c.PaymentType == "SupplierPayout" && c.Status == "Pending").Sum(c => c.CommissionAmount);
            ViewBag.TotalDeducted = earnings.Where(c => c.PaymentType == "PlatformCommission").Sum(c => c.CommissionAmount);

            return View(earnings);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var jsonString = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["x-chapa-signature"].ToString();

            try
            {
                if (!VerifySignature(jsonString, signature))
                {
                    await LogToDeadLetter(jsonString, "Invalid Signature", 0);
                    _logger.LogWarning("Invalid Chapa webhook signature received.");
                    return BadRequest("Invalid Signature");
                }

                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                
                var txRef = root.TryGetProperty("tx_ref", out var txRefProp) ? txRefProp.GetString() : null;
                var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

                if (string.IsNullOrEmpty(txRef))
                {
                    await LogToDeadLetter(jsonString, "tx_ref missing", 0);
                    return BadRequest("tx_ref missing");
                }

                // Idempotency check: find commission by tx_ref
                var commission = await _context.Commissions
                    .Include(c => c.PurchaseOrder)
                    .Include(c => c.Supplier)
                    .FirstOrDefaultAsync(t => t.ChapaTransactionId == txRef);

                if (commission == null)
                {
                    await LogToDeadLetter(jsonString, "Commission not found", 0);
                    _logger.LogWarning($"Commission not found for tx_ref: {txRef}");
                    return Ok(); // Acknowledge to stop Chapa retries
                }

                if (commission.Status == "Paid")
                {
                    return Ok(); // Already processed
                }

                if (status == "success")
                {
                    await _commissionService.FinalizePaymentAsync(commission.Id, txRef, jsonString);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook processing error: {ex.Message}");
                await LogToDeadLetter(jsonString, ex.Message, 0);
                return Ok(); // Acknowledge to stop retries if we can't process it
            }
        }

        private bool VerifySignature(string payload, string signature)
        {
            var secretKey = _config["Chapa:SecretKey"];
            if (string.IsNullOrEmpty(secretKey)) return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();

            return hashString == signature;
        }

        private async Task LogToDeadLetter(string payload, string error, int retryCount)
        {
            var deadLetter = new DeadLetterWebhook
            {
                Payload = payload,
                ErrorMessage = error,
                RetryCount = retryCount,
                CreatedAt = DateTime.Now
            };
            _context.DeadLetterWebhooks.Add(deadLetter);
            await _context.SaveChangesAsync();
        }
    }
}