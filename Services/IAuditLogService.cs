using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(string entityType, string entityId, string actionType, string? oldValueJson = null, string? newValueJson = null, string? notes = null, int? performedByUserId = null, string? ipAddress = null, string? userAgent = null);
        Task<List<AuditLog>> GetLogsForEntityAsync(string entityType, string entityId);
        Task<List<AuditLog>> GetRecentLogsAsync(int limit = 50);
    }
}
