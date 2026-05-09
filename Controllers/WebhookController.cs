using Microsoft.AspNetCore.Mvc;
using SCM_System.Services;
using SCM_System.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using SCM_System.Models.Enums;

namespace SCM_System.Controllers
{
    [Route("api/webhooks/chapa")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ICommissionService _commissionService;
        private readonly IChapaService _chapaService;
        private readonly ILogger<WebhookController> _logger;
        private readonly ApplicationDbContext _context;

        public WebhookController(
            ICommissionService commissionService, 
            IChapaService chapaService, 
            ILogger<WebhookController> logger,
            ApplicationDbContext context)
        {
            _commissionService = commissionService;
            _chapaService = chapaService;
            _logger = logger;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ChapaWebhook()
        {
            try 
            {
                // 1. Read the payload
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                _logger.LogInformation($"Chapa Webhook Received: {body}");

                // 2. Parse basic info (assuming JSON payload with tx_ref)
                var payload = JsonDocument.Parse(body);
                if (!payload.RootElement.TryGetProperty("tx_ref", out var txRefProp))
                {
                    return BadRequest("Missing tx_ref");
                }

                var txRef = txRefProp.GetString();
                if (string.IsNullOrEmpty(txRef)) return BadRequest("tx_ref missing");

                // 3. Verify transaction with Chapa API (Mandatory Security Step)
                var verification = await _chapaService.VerifyPaymentAsync(txRef);
                if (!verification.Success || verification.Status != "success")
                {
                    _logger.LogWarning($"Webhook verification failed for {txRef}: {verification.Status}");
                    return Ok("Verification failed but acknowledged"); 
                }

                // 4. Extraction & Idempotency Check (Prevent duplicate handling)
                var parts = txRef.Split('-');
                if (parts.Length < 2 || !int.TryParse(parts[1], out int commissionId))
                {
                    return BadRequest("Invalid tx_ref format");
                }

                var commission = await _context.Commissions
                    .FirstOrDefaultAsync(c => c.Id == commissionId);

                if (commission == null) return NotFound("Commission not found");

                // Replay Protection: If already Paid, ignore but return OK
                if (commission.Status == PaymentStatus.Paid.ToString())
                {
                    _logger.LogInformation($"Webhook Replay Detected for TX {txRef}. Already Paid.");
                    return Ok("Already processed");
                }

                // Status Guard: Only transition to Paid if in a valid previous state
                if (commission.Status != PaymentStatus.Processing.ToString() && 
                    commission.Status != PaymentStatus.Failed.ToString() && 
                    commission.Status != PaymentStatus.Pending.ToString())
                {
                    _logger.LogWarning($"Invalid state transition attempted for {txRef}. Current: {commission.Status}");
                    return BadRequest("Invalid state transition");
                }

                // 5. Centralized finalization
                bool success = await _commissionService.FinalizePaymentAsync(commission.Id, txRef, body);
                _logger.LogInformation($"Webhook finalized for Commission #{commissionId}: {success}");
                return Ok(new { status = "success", message = "Processed successfully" });

                return Ok("Awaiting further processing");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Webhook Error: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
