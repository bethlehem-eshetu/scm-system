namespace SCM_System.Services
{
    public interface IFaydaService
    {
        Task<(bool success, string message, string? otpCode)> GenerateOtpAsync(string fan, string userEmail);
        Task<(bool success, string message)> VerifyOtpAsync(string fan, string otp);
        Task<(bool success, string fullName, string phoneNumber, DateTime? dob)> GetIdentityDataAsync(string fan);
        Task CleanupUncompletedVerificationsAsync();
    }
}
