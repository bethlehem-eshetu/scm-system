using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;

using SCM_System.Models.Enums;

namespace SCM_System.Services
{
    public class VerificationService : IVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerificationService> _logger;

        public VerificationService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<VerificationService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PendingUsersViewModel> GetPendingUsersAsync(string? roleFilter, string? statusFilter, string? searchTerm)
        {
            var query = _context.Users
                .Include(u => u.Supplier)
                .Include(u => u.Retailer)
                .Include(u => u.FaydaVerification)
                .Where(u => u.Role != "Admin")
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All")
            {
                query = query.Where(u => u.Role == roleFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                if (statusFilter == "Pending")
                    query = query.Where(u => !u.IsApproved && u.AccountStatus != "Rejected");
                else if (statusFilter == "Active")
                    query = query.Where(u => u.AccountStatus == "Active");
                else if (statusFilter == "Rejected")
                    query = query.Where(u => u.AccountStatus == "Rejected");
            }
            else
            {
                // Default to pending if no filter
                query = query.Where(u => !u.IsApproved && u.AccountStatus != "Rejected");
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm) || u.FAN.Contains(searchTerm));
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            
            var model = new PendingUsersViewModel
            {
                TotalPendingCount = await _context.Users.CountAsync(u => !u.IsApproved && u.AccountStatus != "Rejected" && u.Role != "Admin"),
                VerifiedTodayCount = await _context.Users.CountAsync(u => u.IsApproved && u.ApprovedAt.HasValue && u.ApprovedAt.Value.Date == DateTime.Today),
                RejectedTodayCount = await _context.Users.CountAsync(u => u.AccountStatus == "Rejected" && u.ApprovedAt.HasValue && u.ApprovedAt.Value.Date == DateTime.Today),
                WaitlistCount = await _context.Users.CountAsync(u => u.AccountStatus == "Pending" && u.Role != "Admin"), // Users who registered but haven't started full verification
                AverageApprovalTimeHours = 4.5, // Mock value
                PendingUsers = new List<PendingUserCard>()
            };

            foreach (var user in users)
            {
                var card = new PendingUserCard
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Role = user.Role,
                    AppliedAt = user.CreatedAt,
                    Status = user.AccountStatus,
                    FaydaId = user.FAN,
                    IsFaydaVerified = user.IsFaydaVerified,
                    ConfidenceScore = user.Role == "Supplier" ? 85 : 92, // Mocking based on FaydaId presence
                    RiskScore = "Low"
                };

                if (user.Role == "Supplier" && user.Supplier != null)
                {
                    card.CompanyName = user.Supplier.CompanyName;
                    card.TaxId = user.Supplier.TaxIdentificationNumber;
                    if (!string.IsNullOrEmpty(user.Supplier.LicenseFilePath)) card.Documents.Add("License");
                    if (!string.IsNullOrEmpty(user.Supplier.TaxIdentificationNumber)) card.Documents.Add("Tax ID");
                }
                else if (user.Role == "Retailer" && user.Retailer != null)
                {
                    card.CompanyName = user.Retailer.BusinessName;
                    card.TaxId = user.Retailer.TaxIdentificationNumber;
                    if (!string.IsNullOrEmpty(user.Retailer.BusinessLicenseNumber)) card.Documents.Add("License");
                }

                // Calculate Risk
                var riskScore = await CalculateRiskScoreAsync(user.Id);
                card.ConfidenceScore = 100 - riskScore; // Inverse relationship for mockup simplicity
                
                if (riskScore > 60) card.RiskScore = "High";
                else if (riskScore > 30) card.RiskScore = "Medium";
                else card.RiskScore = "Low";

                if (user.FaydaVerification == null) card.RiskFactors.Add("Missing Fayda verification");

                model.PendingUsers.Add(card);
            }

            return model;
        }

        public async Task<bool> ApproveUserAsync(int userId, string adminName, string? ipAddress = null, string? userAgent = null)
        {
            var user = await _context.Users
                .Include(u => u.Supplier)
                .Include(u => u.Retailer)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;

            user.IsApproved = true;
            user.AccountStatus = "Active";
            user.ApprovedAt = DateTime.UtcNow;
            user.IsFaydaVerified = true; // Implicitly verified if admin approves

            if (user.Role == "Supplier" && user.Supplier != null)
            {
                user.Supplier.VerificationStatus = "Verified";
            }
            else if (user.Role == "Retailer" && user.Retailer != null)
            {
                user.Retailer.IsVerified = true;
            }

            // Create Notification
            await _notificationService.CreateNotificationAsync(
                user.Id,
                "✅ Account Approved",
                $"Congratulations! Your {user.Role} account has been approved. You can now access all features.",
                "Approval",
                "/Dashboard"
            );

            // Send Email
            try
            {
                await _emailService.SendApprovalEmailAsync(user.Email, user.FullName, user.Role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval email to {Email}", user.Email);
            }

            // Audit Log
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.FullName == adminName && u.Role == "Admin");
            var auditLog = new AuditLog
            {
                EntityType = "User",
                EntityId = user.Id.ToString(),
                ActionType = "Approve",
                PerformedByUserId = adminUser?.Id,
                Notes = $"Approved {user.Role}: {user.FullName}",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                PerformedAtUtc = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectUserAsync(int userId, string reason, string? notes, string adminName, string? ipAddress = null, string? userAgent = null)
        {
            var user = await _context.Users
                .Include(u => u.Supplier)
                .Include(u => u.Retailer)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;

            user.IsApproved = false;
            user.AccountStatus = "Rejected";
            user.RejectionReason = reason;
            user.ApprovedAt = DateTime.UtcNow; // Using this as the rejection timestamp too

            if (user.Role == "Supplier" && user.Supplier != null)
            {
                user.Supplier.VerificationStatus = "Rejected";
            }
            else if (user.Role == "Retailer" && user.Retailer != null)
            {
                user.Retailer.IsVerified = false;
            }

            // Create Notification
            await _notificationService.CreateNotificationAsync(
                user.Id,
                "❌ Account Rejected",
                $"Your {user.Role} account application has been rejected. Reason: {reason}",
                "Alert"
            );

            // Send Email
            try
            {
                await _emailService.SendRejectionEmailAsync(user.Email, user.FullName, user.Role, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection email to {Email}", user.Email);
            }

            // Audit Log
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.FullName == adminName && u.Role == "Admin");
            var auditLog = new AuditLog
            {
                EntityType = "User",
                EntityId = user.Id.ToString(),
                ActionType = "Reject",
                PerformedByUserId = adminUser?.Id,
                Notes = $"Rejected {user.Role}: {user.FullName}. Reason: {reason}. Admin Notes: {notes}",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                PerformedAtUtc = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CalculateRiskScoreAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Supplier)
                .Include(u => u.Retailer)
                .Include(u => u.FaydaVerification)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return 0;

            int score = 0;

            // Fayda score < 70% (Simulated here)
            if (user.FaydaVerification == null) score += 40;
            
            // Duplicate email (Already handled by registration but double check)
            if (await _context.Users.CountAsync(u => u.Email == user.Email) > 1) score += 30;

            // Missing documents
            if (user.Role == "Supplier" && (user.Supplier == null || string.IsNullOrEmpty(user.Supplier.LicenseFilePath)))
            {
                score += 20;
            }

            // Suspicious registration pattern (mock logic: register within 1 min of another user with same IP)
            // Just a placeholder for now
            
            return Math.Min(score, 100);
        }
    }
}
