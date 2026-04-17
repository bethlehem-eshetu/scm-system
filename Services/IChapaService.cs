using SCM_System.Models.ViewModels;

namespace SCM_System.Services
{
    public interface IChapaService
    {
        Task<ChapaInitializeResponse> InitializePaymentAsync(ChapaPaymentRequest request);
        Task<ChapaVerifyResponse> VerifyPaymentAsync(string transactionId);
        Task<bool> WebhookHandlerAsync(string transactionId, string status);
    }

    public class ChapaPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ETB";
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string TxRef { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CustomizationTitle { get; set; } = string.Empty;
        public string CustomizationDescription { get; set; } = string.Empty;
    }

    public class ChapaInitializeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
    }

    public class ChapaVerifyResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}