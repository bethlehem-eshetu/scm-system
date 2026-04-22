using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(string entityType, string entityId, string actionType, string? oldValueJson = null, string? newValueJson = null, string? notes = null, int? performedByUserId = null, string? ipAddress = null, string? userAgent = null)
        {
            var log = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                ActionType = actionType,
                OldValueJson = oldValueJson,
                NewValueJson = newValueJson,
                Notes = notes,
                PerformedByUserId = performedByUserId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                PerformedAtUtc = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetLogsForEntityAsync(string entityType, string entityId)
        {
            return await _context.AuditLogs
                .Include(l => l.PerformedByUser)
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.PerformedAtUtc)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetRecentLogsAsync(int limit = 50)
        {
            return await _context.AuditLogs
                .Include(l => l.PerformedByUser)
                .OrderByDescending(l => l.PerformedAtUtc)
                .Take(limit)
                .ToListAsync();
        }
    }
}
