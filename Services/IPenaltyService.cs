using SCM_System.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCM_System.Services
{
    public interface IPenaltyService
    {
        Task<Penalty> RecordViolation(int userId, string userType, string penaltyType, string reason, int? messageId = null);
        Task<bool> CanSendMessage(int userId);
        Task<bool> IsUserRestricted(int userId);
        Task<List<Penalty>> GetUserPenalties(int userId);
        Task<int> GetViolationCount(int userId, int days = 30);
    }
}