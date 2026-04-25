using System.Threading.Tasks;

namespace SCM_System.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendApprovalEmailAsync(string toEmail, string userName, string role);
        Task SendRejectionEmailAsync(string toEmail, string userName, string role, string reason);
        Task SendWelcomeEmailAsync(string toEmail, string userName, string role);
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string token);
        Task SendOtpEmailAsync(string toEmail, string otpCode);
        Task SendPaymentConfirmationEmailAsync(string toEmail, string userName, string orderNumber, decimal amount);
        Task SendRefundInitiatedEmailAsync(string toEmail, string userName, string orderNumber, decimal amount);
        Task SendPaymentExpiryEmailAsync(string toEmail, string userName, string orderNumber);
    }
}
