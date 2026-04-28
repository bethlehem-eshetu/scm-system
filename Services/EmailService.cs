using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using SCM_System.Models;
using SCM_System.Models.Entities;
using SCM_System.Data;
using Microsoft.Extensions.Logging;

namespace SCM_System.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext _context;

        public EmailService(
            IOptions<EmailSettings> emailSettings, 
            ILogger<EmailService> logger,
            ApplicationDbContext context)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _context = context;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, string? replyTo = null)
        {
            await SendEmailInternalAsync(toEmail, subject, body, "General", null, replyTo);
        }

        private async Task SendEmailInternalAsync(string toEmail, string subject, string body, string type, string? referenceId, string? replyTo = null)
        {
            Console.WriteLine($"[EMAIL] Attempting to send '{type}' email to: {toEmail}");
            Console.WriteLine($"[EMAIL] Subject: {subject}");
            
            if (!_emailSettings.EnableEmail)
            {
                Console.WriteLine($"[EMAIL] Email sending is DISABLED in settings. [MOCK SENT]");
                _logger.LogInformation("Email sending is disabled. To: {To}, Subject: {Subject}", toEmail, subject);
                
                await LogToDatabase(toEmail, subject, type, referenceId, true, "Email disabled in settings (Mock Success)");
                return;
            }

            int retryCount = 0;
            bool success = false;
            string? lastError = null;

            while (retryCount < 3 && !success)
            {
                try
                {
                    using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                    {
                        EnableSsl = _emailSettings.EnableSsl,
                        Credentials = new NetworkCredential(_emailSettings.Email, _emailSettings.Password)
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.Email, _emailSettings.SenderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);
                    if (!string.IsNullOrEmpty(replyTo))
                    {
                        mailMessage.ReplyToList.Add(new MailAddress(replyTo));
                    }

                    await client.SendMailAsync(mailMessage);
                    success = true;
                    Console.WriteLine($"[EMAIL] SUCCESS: Email sent to {toEmail}");
                    _logger.LogInformation("Email sent successfully to {To}", toEmail);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    lastError = ex.Message;
                    Console.WriteLine($"[EMAIL] ERROR (Attempt {retryCount}): {ex.Message}");
                    _logger.LogError(ex, "Failed to send email to {To}. Attempt {Attempt}", toEmail, retryCount);
                    if (retryCount >= 3) break;
                    await Task.Delay(1000 * retryCount);
                }
            }

            await LogToDatabase(toEmail, subject, type, referenceId, success, lastError);
            if (!success) throw new Exception($"Failed to send email after 3 attempts: {lastError}");
        }

        private async Task LogToDatabase(string to, string subject, string type, string? refId, bool success, string? error)
        {
            try
            {
                var log = new EmailLog
                {
                    To = to,
                    Subject = subject,
                    Type = type,
                    ReferenceId = refId,
                    IsSuccess = success,
                    ErrorMessage = error,
                    SentAt = DateTime.Now
                };
                _context.EmailLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write email log to database.");
            }
        }

        public async Task SendApprovalEmailAsync(string toEmail, string userName, string role)
        {
            var subject = $"Your {role} Account Has Been Approved!";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #28a745; text-align: center;'>Welcome to EthioChain, {userName}!</h2>
                <p>Great news! Your application as a <strong>{role}</strong> has been approved by our administrative team.</p>
                <p>You can now log in to your dashboard and start conducting business on the platform.</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p style='margin: 0;'><strong>Login Credentials:</strong></p>
                    <ul style='margin: 10px 0;'>
                        <li>Email: {toEmail}</li>
                        <li>Platform: EthioChain SCM</li>
                    </ul>
                </div>
                <div style='text-align: center;'>
                    <a href='https://ethiochain-scm.com/Account/Login' style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Login Now</a>
                </div>
                <p style='margin-top: 30px; font-size: 0.9em; color: #6c757d;'>Best regards,<br/>The EthioChain Team</p>
            </div>";

            await SendEmailInternalAsync(toEmail, subject, body, "Approval", role);
        }

        public async Task SendRejectionEmailAsync(string toEmail, string userName, string role, string reason)
        {
            var subject = $"Update on Your {role} Account Application";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #dc3545; text-align: center;'>Application Status Update</h2>
                <p>Hello {userName},</p>
                <p>We regret to inform you that your application as a <strong>{role}</strong> has been rejected at this time.</p>
                <div style='background-color: #fff3f3; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 5px solid #dc3545;'>
                    <p style='margin: 0;'><strong>Reason for Rejection:</strong></p>
                    <p style='margin: 10px 0;'>{reason}</p>
                </div>
                <p>If you believe this is a mistake, or if you wish to adjust your application details and try again, please contact our support team at support@ethiochain.com.</p>
                <p style='margin-top: 30px; font-size: 0.9em; color: #6c757d;'>Regards,<br/>The EthioChain Team</p>
            </div>";

            await SendEmailInternalAsync(toEmail, subject, body, "Rejection", role);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName, string role)
        {
            var subject = "Welcome to EthioChain SCM!";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2>Welcome aboard, {userName}!</h2>
                <p>Thank you for registering as a {role} on EthioChain SCM.</p>
                <p>Your account has been created and is currently <strong>pending administrative review</strong>. We will notify you via email as soon as your application is processed.</p>
                <p>In the meantime, feel free to browse our public documentation.</p>
                <p>Best regards,<br/>EthioChain team</p>
            </div>";
            await SendEmailInternalAsync(toEmail, subject, body, "Welcome", role);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string token)
        {
            var subject = "Reset Your Password - EthioChain SCM";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2>Password Reset Request</h2>
                <p>Hello {userName},</p>
                <p>We received a request to reset your password. If you didn't make this request, you can safely ignore this email.</p>
                <p>To reset your password, click the link below:</p>
                <p><a href='https://ethiochain-scm.com/Account/ResetPassword?token={token}'>Reset Password</a></p>
                <p>Best regards,<br/>EthioChain Team</p>
            </div>";
            await SendEmailInternalAsync(toEmail, subject, body, "PasswordReset", null);
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            Console.WriteLine($"[OTP_DEBUG] Generating email for {toEmail} with code: {otpCode}");
            
            var subject = "Your Fayda Verification OTP - EthioChain SCM";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #007bff; text-align: center;'>Fayda Verification</h2>
                <p>Hello,</p>
                <p>You have requested a secure One-Time Password (OTP) to verify your Fayda identity on the EthioChain SCM platform.</p>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0; text-align: center;'>
                    <p style='margin: 0; color: #6c757d; font-size: 0.9em; text-transform: uppercase; font-weight: bold;'>Your Verification Code</p>
                    <h1 style='margin: 10px 0 0 0; color: #212529; letter-spacing: 5px; font-size: 32px;'>{otpCode}</h1>
                </div>
                <p style='color: #dc3545; font-size: 0.9em;'><strong>Note:</strong> This code will expire in 5 minutes. Do not share it with anyone.</p>
                <p style='margin-top: 30px; font-size: 0.9em; color: #6c757d;'>Best regards,<br/>The EthioChain Team</p>
            </div>";

            await SendEmailInternalAsync(toEmail, subject, body, "OTP", "FaydaOTP");
        }

        public async Task SendPaymentConfirmationEmailAsync(string toEmail, string userName, string orderNumber, decimal amount)
        {
            var subject = $"Payment Confirmed for Order {orderNumber}";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #28a745; text-align: center;'>Payment Successful!</h2>
                <p>Hello {userName},</p>
                <p>Your payment of <strong>{amount:C}</strong> for Order <strong>#{orderNumber}</strong> has been successfully processed.</p>
                <p>The supplier has been notified and will proceed with fulfillment.</p>
                <p style='margin-top: 30px; font-size: 0.9em; color: #6c757d;'>Regards,<br/>The EthioChain Team</p>
            </div>";
            await SendEmailInternalAsync(toEmail, subject, body, "PaymentConfirmation", orderNumber);
        }

        public async Task SendRefundInitiatedEmailAsync(string toEmail, string userName, string orderNumber, decimal amount)
        {
            var subject = $"Refund Initiated for Order {orderNumber}";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #17a2b8; text-align: center;'>Refund Initiated</h2>
                <p>Hello {userName},</p>
                <p>A refund of <strong>{amount:C}</strong> for Order <strong>#{orderNumber}</strong> has been initiated and is being processed via Chapa.</p>
                <p>It may take a few business days to reflect in your account.</p>
                <p style='margin-top: 30px; font-size: 0.9em; color: #6c757d;'>Regards,<br/>The EthioChain Team</p>
            </div>";
            await SendEmailInternalAsync(toEmail, subject, body, "RefundInitiated", orderNumber);
        }

        public async Task SendPaymentExpiryEmailAsync(string toEmail, string userName, string orderNumber)
        {
            var subject = $"Action Required: Payment Expired for Order {orderNumber}";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #dc3545; text-align: center;'>Payment Timeout</h2>
                <p>Hello {userName},</p>
                <p>The pending payment for Order <strong>#{orderNumber}</strong> has expired and the order has been cancelled.</p>
                <p>If you still wish to proceed, please place a new order.</p>
                <p style='margin-top: 30px; font-size: 0.9em; color: #6c757d;'>Regards,<br/>The EthioChain Team</p>
            </div>";
            await SendEmailInternalAsync(toEmail, subject, body, "PaymentExpiry", orderNumber);
        }
    }
}
