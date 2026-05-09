using SCM_System.Models.Entities;
using System.Threading.Tasks;

namespace SCM_System.Services
{
    public interface IVerificationService
    {
        Task<bool> ApproveUserAsync(int userId, int approverId);
        Task<bool> RejectUserAsync(int userId, int rejecterId, string reason);
        Task<IEnumerable<User>> GetPendingUsersAsync();
        Task<UserRiskAssessment> GetRiskAssessmentAsync(int userId);
    }

    public class UserRiskAssessment
    {
        public string RiskLevel { get; set; } = "Low"; // Low, Medium, High
        public List<string> RiskFactors { get; set; } = new();
        public int ConfidenceScore { get; set; } = 100;
    }
}
