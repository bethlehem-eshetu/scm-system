using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class PenaltyService : IPenaltyService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public PenaltyService(ApplicationDbContext context, INotificationService notificationService)

        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Penalty> RecordViolation(int userId, string userType, string penaltyType, string reason, int? messageId = null)
        {
            // Get violation count in last 30 days
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var recentViolations = await _context.Penalties
                .Where(p => p.UserId == userId && p.CreatedAt >= thirtyDaysAgo)
                .CountAsync();

            DateTime? expiresAt = null;
            string finalPenaltyType = penaltyType;

            // Determine penalty level based on violation count
            if (recentViolations >= 5)
            {
                finalPenaltyType = "Account Suspension";
                expiresAt = DateTime.Now.AddDays(30);
            }
            else if (recentViolations >= 3)
            {
                finalPenaltyType = "Message Restriction";
                expiresAt = DateTime.Now.AddDays(7);
            }
            else
            {
                finalPenaltyType = "Warning";
                expiresAt = DateTime.Now.AddDays(30);
            }

            var penalty = new Penalty
            {
                UserId = userId,
                UserType = userType,
                PenaltyType = finalPenaltyType,
                Reason = reason,
                CreatedAt = DateTime.Now,
                ExpiresAt = expiresAt,
                IsActive = true,
                HasAppealed = false,
                Status = (PenaltyStatus)1  // Active status
            };

            _context.Penalties.Add(penalty);
            await _context.SaveChangesAsync();

            // ✅ Send notification to user about the penalty
            await _notificationService.SendPenaltyNotificationAsync(userId, finalPenaltyType, reason, penalty.Id);

            return penalty;
        }
        public async Task<bool> CanSendMessage(int userId)
        {
            // Check for active penalty (using IsActive column)
            var activePenalty = await _context.Penalties
                .Where(p => p.UserId == userId &&
                           p.IsActive &&
                           (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now))
                .OrderByDescending(p => p.PenaltyType)
                .FirstOrDefaultAsync();

            if (activePenalty == null)
                return true;

            // Account Suspension - cannot send any messages
            if (activePenalty.PenaltyType == "Account Suspension")
                return false;

            // Message Restriction - only 1 message per day
            if (activePenalty.PenaltyType == "Message Restriction")
            {
                var today = DateTime.Now.Date;
                var messagesToday = await _context.Messages
                    .Where(m => m.SenderId == userId && m.CreatedAt.Date == today)
                    .CountAsync();
                return messagesToday < 1;
            }

            return true; // Warning only - can still send
        }

        public async Task<bool> IsUserRestricted(int userId)
        {
            var activePenalty = await _context.Penalties
                .Where(p => p.UserId == userId &&
                           p.IsActive &&
                           (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now))
                .FirstOrDefaultAsync();

            if (activePenalty == null)
                return false;

            return activePenalty.PenaltyType == "Account Suspension" ||
                   activePenalty.PenaltyType == "Message Restriction";
        }

        public async Task<List<Penalty>> GetUserPenalties(int userId)
        {
            return await _context.Penalties
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetViolationCount(int userId, int days = 30)
        {
            var since = DateTime.Now.AddDays(-days);
            return await _context.Penalties
                .Where(p => p.UserId == userId && p.CreatedAt >= since)
                .CountAsync();
        }
    }
}