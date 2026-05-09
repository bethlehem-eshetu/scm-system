using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCM_System.Data;
using SCM_System.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<bool> ApproveUserAsync(int userId, int approverId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Include(u => u.Supplier)
                    .Include(u => u.Retailer)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return false;

                // Update User
                user.IsApproved = true;
                user.AccountStatus = "Active";
                user.ApprovedAt = DateTime.Now;
                user.ApprovalStatus = "Approved";
                user.ApprovalStatusType = "Approved";

                // Update Sub-Entities
                if (user.Role == "Supplier" && user.Supplier != null)
                {
                    user.Supplier.VerificationStatus = "Verified";
                }
                else if (user.Role == "Retailer" && user.Retailer != null)
                {
                    user.Retailer.IsVerified = true;
                }

                // Mark registration notifications as read
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == approverId && n.ActionUrl == "/Admin/PendingUsers" && !n.IsRead)
                    .ToListAsync();
                
                foreach (var n in notifications)
                {
                    if (n.Message.Contains($"'{user.FullName}'") || n.Message.Contains(user.Email))
                    {
                        n.IsRead = true;
                        n.ReadAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send Email Notification
                try
                {
                    await _emailService.SendApprovalEmailAsync(user.Email, user.FullName, user.Role);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send approval email to {Email}", user.Email);
                }

                // Create notification for user
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "✅ Account Approved",
                    "Congratulations! Your account has been approved. You can now access the platform.",
                    "Success",
                    "/Account/Login"
                );

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RejectUserAsync(int userId, int rejecterId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Include(u => u.Supplier)
                    .Include(u => u.Retailer)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return false;

                // Update User
                user.IsApproved = false;
                user.AccountStatus = "Rejected";
                user.RejectionReason = reason;
                user.ApprovalStatus = "Rejected";
                user.ApprovalStatusType = "Rejected";

                // Update Sub-Entities
                if (user.Role == "Supplier" && user.Supplier != null)
                {
                    user.Supplier.VerificationStatus = "Rejected";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send Email Notification
                try
                {
                    await _emailService.SendRejectionEmailAsync(user.Email, user.FullName, user.Role, reason);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send rejection email to {Email}", user.Email);
                }

                // Create notification for user (though they might not login, it's for records)
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "❌ Account Rejected",
                    $"Your account application has been rejected. Reason: {reason}",
                    "Error"
                );

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting user {UserId}", userId);
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetPendingUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Supplier)
                .Include(u => u.Retailer)
                .Include(u => u.FaydaVerification)
                .Where(u => !u.IsApproved && u.Role != "Admin" && u.AccountStatus != "Rejected" && u.AccountStatus != "Suspended")
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserRiskAssessment> GetRiskAssessmentAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Supplier)
                .Include(u => u.Retailer)
                .Include(u => u.FaydaVerification)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return new UserRiskAssessment();

            var assessment = new UserRiskAssessment();
            var factors = new List<string>();
            int score = 100;

            // 1. Fayda Match Check
            // Simulating match percentage if FaydaVerification exists
            int faydaMatch = 100;
            if (user.FaydaVerification == null)
            {
                faydaMatch = 65; // High risk if no biometric data
                factors.Add("Missing biometric verification data");
                score -= 30;
            }
            else
            {
                // In a real system, this would be computed by comparing user.FullName with user.FaydaVerification.VerifiedName
                // For this demo, let's say it's 95%
                faydaMatch = 95;
            }
            assessment.ConfidenceScore = faydaMatch;

            // 2. Duplicate Check (TIN / Phone / Email)
            if (user.Role == "Supplier" && user.Supplier != null)
            {
                if (await _context.Suppliers.AnyAsync(s => s.Id != user.Supplier.Id && s.TaxIdentificationNumber == user.Supplier.TaxIdentificationNumber))
                {
                    factors.Add("Duplicate Tax Identification Number (TIN) detected");
                    score -= 40;
                }
                if (string.IsNullOrEmpty(user.Supplier.LicenseFilePath))
                {
                    factors.Add("Missing business license document");
                    score -= 20;
                }
            }
            else if (user.Role == "Retailer" && user.Retailer != null)
            {
                 if (await _context.Retailers.AnyAsync(r => r.Id != user.Retailer.Id && r.TaxIdentificationNumber == user.Retailer.TaxIdentificationNumber))
                {
                    factors.Add("Duplicate Tax Identification Number (TIN) detected");
                    score -= 20;
                }
            }

            // 3. Email/Phone Reputation (Simulated)
            if (user.Email.Contains("temp") || user.Email.Contains("trash"))
            {
                factors.Add("Disposable email address detected");
                score -= 15;
            }

            // Set final risk level
            if (score >= 80) assessment.RiskLevel = "Low";
            else if (score >= 50) assessment.RiskLevel = "Medium";
            else assessment.RiskLevel = "High";

            assessment.RiskFactors = factors;
            assessment.ConfidenceScore = Math.Min(100, Math.Max(0, score)); // Use score as a proxy for confidence here or refine

            return assessment;
        }
    }
}
