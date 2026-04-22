using Microsoft.AspNetCore.Mvc;
using SCM_System.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using SCM_System.Models.Entities;

namespace SCM_System.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentController(ApplicationDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
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

                if (commission.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "This payment has already been processed.";
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

            try
            {
                var commission = await _context.Commissions
                    .Include(c => c.Retailer)
                    .Include(c => c.PurchaseOrder)  // ✅ Added: Include PurchaseOrder
                    .FirstOrDefaultAsync(c => c.ChapaTransactionId == tx_ref);

                if (commission == null)
                {
                    Console.WriteLine($"❌ Commission not found for tx_ref: {tx_ref}");
                    TempData["ErrorMessage"] = "Commission not found";
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
                            commission.Status = "Paid";
                            commission.PaidAt = DateTime.Now;

                            // ✅ Update PurchaseOrder PaymentStatus
                            if (commission.PurchaseOrder != null)
                            {
                                commission.PurchaseOrder.PaymentStatus = "Paid";
                                _context.PurchaseOrders.Update(commission.PurchaseOrder);
                                Console.WriteLine($"✅ PurchaseOrder {commission.PurchaseOrder.PONumber} PaymentStatus updated to Paid");
                            }

                            // Populate Platform Commission and Supplier Payout upon successful payment
                            if (commission.PaymentType == "OrderPayment")
                            {
                                var existingPlatform = await _context.Commissions
                                    .AnyAsync(c => c.PurchaseOrderId == commission.PurchaseOrderId && c.PaymentType == "PlatformCommission");

                                if (!existingPlatform)
                                {
                                    var platformComm = new Commission
                                    {
                                        PurchaseOrderId = commission.PurchaseOrderId,
                                        OrderId = commission.OrderId,
                                        SupplierId = commission.SupplierId,
                                        RetailerId = commission.RetailerId,
                                        OrderAmount = commission.OrderAmount,
                                        CommissionRate = 0.05m,
                                        CommissionAmount = commission.OrderAmount * 0.05m,
                                        PaymentType = "PlatformCommission",
                                        Status = "Paid",
                                        CreatedAt = DateTime.Now,
                                        DueDate = DateTime.Now,
                                        PaidAt = DateTime.Now,
                                        Notes = "Platform commission deducted from Order Payment"
                                    };
                                    _context.Commissions.Add(platformComm);

                                    var supplierPayout = new Commission
                                    {
                                        PurchaseOrderId = commission.PurchaseOrderId,
                                        OrderId = commission.OrderId,
                                        SupplierId = commission.SupplierId,
                                        RetailerId = commission.RetailerId,
                                        OrderAmount = commission.OrderAmount,
                                        CommissionRate = 0.95m,
                                        CommissionAmount = commission.OrderAmount * 0.95m,
                                        PaymentType = "SupplierPayout",
                                        Status = "Pending",
                                        CreatedAt = DateTime.Now,
                                        DueDate = DateTime.Now.AddDays(7),
                                        Notes = "Pending payout to supplier for Order Payment"
                                    };
                                    _context.Commissions.Add(supplierPayout);
                                }
                            }

                            await _context.SaveChangesAsync();
                            Console.WriteLine($"✅ Commission {commission.Id} updated to PAID");
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

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Verification error: {ex.Message}");
                TempData["ErrorMessage"] = $"Verification error: {ex.Message}";
            }

            return RedirectToAction("MyPayments", "Payment");
        }

        public async Task<IActionResult> MyPayments()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null) return RedirectToAction("Login", "Account");

            List<Commission> payments = new List<Commission>();

            if (user.Role == "Retailer")
            {
                var retailer = await _context.Retailers
                    .FirstOrDefaultAsync(r => r.UserId == currentUserId);

                if (retailer != null)
                {
                    // ✅ ONLY show OrderPayment (100% - what retailer owes to supplier)
                    payments = await _context.Commissions
                        .Include(c => c.Order)
                        .Include(c => c.PurchaseOrder)
                        .Where(c => c.RetailerId == retailer.Id && c.PaymentType == "OrderPayment")
                        .OrderByDescending(c => c.CreatedAt)
                        .ToListAsync();
                }
            }
            else if (user.Role == "Supplier")
            {
                // ✅ Suppliers can also see payments they receive (if needed)
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId);

                if (supplier != null)
                {
                    payments = await _context.Commissions
                        .Include(c => c.Order)
                        .Include(c => c.PurchaseOrder)
                        .Where(c => c.SupplierId == supplier.Id && c.PaymentType == "OrderPayment" && c.Status == "Paid")
                        .OrderByDescending(c => c.CreatedAt)
                        .ToListAsync();
                }
            }

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
    }
}