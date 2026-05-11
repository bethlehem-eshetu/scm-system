using SCM_System.Models.ViewModels;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IVerificationService
    {
        Task<PendingUsersViewModel> GetPendingUsersAsync(string? roleFilter, string? statusFilter, string? searchTerm);
        Task<bool> ApproveUserAsync(int userId, string adminName, string? ipAddress = null, string? userAgent = null);
        Task<bool> RejectUserAsync(int userId, string reason, string? notes, string adminName, string? ipAddress = null, string? userAgent = null);
        Task<int> CalculateRiskScoreAsync(int userId);
    }
}
