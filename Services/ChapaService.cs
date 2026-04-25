using System.Text;
using System.Text.Json;
using SCM_System.Models.ViewModels;

namespace SCM_System.Services
{
    public class ChapaService : IChapaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChapaService> _logger;

        public ChapaService(HttpClient httpClient, IConfiguration configuration, ILogger<ChapaService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ChapaInitializeResponse> InitializePaymentAsync(ChapaPaymentRequest request)
        {
            try
            {
                var secretKey = _configuration["Chapa:SecretKey"] ?? "CHASECK_TEST-xxxxxxxxxxxxx";
                var baseUrl = _configuration["Chapa:BaseUrl"] ?? "https://api.chapa.co/v1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

                var payload = new
                {
                    amount = request.Amount.ToString("0.00"),
                    currency = request.Currency,
                    email = request.Email,
                    first_name = request.FirstName,
                    last_name = request.LastName,
                    phone_number = request.PhoneNumber,
                    tx_ref = request.TxRef,
                    callback_url = request.CallbackUrl,
                    return_url = request.ReturnUrl,
                    customization = new
                    {
                        title = request.CustomizationTitle,
                        description = request.CustomizationDescription
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{baseUrl}/transaction/initialize", content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Chapa response: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ChapaApiResponse>(responseString);
                    return new ChapaInitializeResponse
                    {
                        Success = true,
                        TransactionId = result?.data?.tx_ref ?? string.Empty,
                        PaymentUrl = result?.data?.checkout_url ?? string.Empty,
                        Message = "Payment initialized successfully"
                    };
                }

                return new ChapaInitializeResponse
                {
                    Success = false,
                    Message = $"Chapa API error: {responseString}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chapa initialization error: {ex.Message}");
                return new ChapaInitializeResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<ChapaVerifyResponse> VerifyPaymentAsync(string transactionId)
        {
            try
            {
                var secretKey = _configuration["Chapa:SecretKey"] ?? "CHASECK_TEST-xxxxxxxxxxxxx";
                var baseUrl = _configuration["Chapa:BaseUrl"] ?? "https://api.chapa.co/v1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

                var response = await _httpClient.GetAsync($"{baseUrl}/transaction/verify/{transactionId}");
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ChapaVerifyApiResponse>(responseString);
                    return new ChapaVerifyResponse
                    {
                        Success = true,
                        Status = result?.data?.status ?? "unknown",
                        TransactionId = transactionId,
                        Amount = result?.data?.amount ?? 0,
                        Currency = result?.data?.currency ?? "ETB",
                        Email = result?.data?.email ?? string.Empty,
                        CreatedAt = result?.data?.created_at ?? DateTime.Now
                    };
                }

                return new ChapaVerifyResponse
                {
                    Success = false,
                    Status = "failed",
                    TransactionId = transactionId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chapa verification error: {ex.Message}");
                return new ChapaVerifyResponse
                {
                    Success = false,
                    Status = "error",
                    TransactionId = transactionId
                };
            }
        }

        public async Task<bool> WebhookHandlerAsync(string transactionId, string status)
        {
            // This will be called by Chapa webhook
            // Implement webhook verification logic here
            _logger.LogInformation($"Webhook received: Transaction {transactionId} - Status: {status}");
            return true;
        }

        public async Task<ChapaRefundResponse> InitiateRefundAsync(string transactionId, decimal amount)
        {
            try
            {
                var secretKey = _configuration["Chapa:SecretKey"] ?? "CHASECK_TEST-xxxxxxxxxxxxx";
                var baseUrl = _configuration["Chapa:BaseUrl"] ?? "https://api.chapa.co/v1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

                var payload = new
                {
                    transaction_id = transactionId,
                    amount = amount.ToString("0.00")
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{baseUrl}/transaction/refund", content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Chapa refund response: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    return new ChapaRefundResponse
                    {
                        Success = true,
                        Message = "Refund initiated successfully",
                        Status = "success"
                    };
                }

                return new ChapaRefundResponse
                {
                    Success = false,
                    Message = $"Chapa refund error: {responseString}",
                    Status = "failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chapa refund error: {ex.Message}");
                return new ChapaRefundResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Status = "error"
                };
            }
        }

        // Helper classes for deserialization
        private class ChapaApiResponse
        {
            public bool success { get; set; }
            public string message { get; set; } = string.Empty;
            public ChapaData data { get; set; } = new();
        }

        private class ChapaData
        {
            public string tx_ref { get; set; } = string.Empty;
            public string checkout_url { get; set; } = string.Empty;
        }

        private class ChapaVerifyApiResponse
        {
            public bool success { get; set; }
            public string message { get; set; } = string.Empty;
            public ChapaVerifyData data { get; set; } = new();
        }

        private class ChapaVerifyData
        {
            public string status { get; set; } = string.Empty;
            public decimal amount { get; set; }
            public string currency { get; set; } = string.Empty;
            public string email { get; set; } = string.Empty;
            public DateTime created_at { get; set; }
        }
    }
}