using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using System.Text.RegularExpressions;

namespace SCM_System.Services
{
    public class FaydaService : IFaydaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public FaydaService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<(bool success, string message, string? otpCode)> GenerateOtpAsync(string fan, string userEmail)
        {
            if (string.IsNullOrEmpty(fan) || !Regex.IsMatch(fan, @"^\d{16}$"))
                return (false, "Invalid FAN format. Must be 16 digits.", null);

            if (string.IsNullOrEmpty(userEmail))
                return (false, "User email is required to receive the OTP.", null);

            var record = await _context.FaydaRegistries.FirstOrDefaultAsync(x => x.FAN == fan);
            if (record == null || !record.IsActive)
                return (false, "FAN not found in Fayda Registry.", null);

            var verification = await _context.FaydaVerifications.FirstOrDefaultAsync(x => x.FAN == fan);

            // Multi-session handling: Invalidate old state if it wasn't verified
            // Rate Limiting: Max 5 requests per hour per FAN or Email
            var oneHourAgo = DateTime.Now.AddHours(-1);
            var previousRequestTime = verification?.LastOtpRequestTime ?? DateTime.MinValue;

            if (verification != null)
            {
                // Check per FAN (existing verification)
                if (verification.LastOtpRequestTime > oneHourAgo && verification.ResendCount >= 5)
                {
                    return (false, "Rate limit exceeded for this FAN. Maximum 5 OTP requests per hour.", null);
                }

                // Check per Email (across all FANs)
                var emailRequestCount = await _context.FaydaVerifications
                    .CountAsync(v => v.UserEmail == userEmail && v.LastOtpRequestTime > oneHourAgo);

                if (emailRequestCount >= 5)
                {
                    return (false, "Rate limit exceeded for this email. Maximum 5 OTP requests per hour.", null);
                }

                if (!verification.IsVerified)
                {
                    if ((DateTime.Now - verification.LastOtpRequestTime).TotalSeconds < 30)
                    {
                        return (false, "Please wait before requesting another OTP.", null);
                    }

                    if (verification.IsLocked)
                    {
                        return (false, "Your Fayda verification is locked.", null);
                    }

                    // Reset for new transaction
                    verification.TransactionId = Guid.NewGuid().ToString("N").ToUpper();
                    verification.UserEmail = userEmail; // Update the email if changed
                }
            }
            else
            {
                verification = new FaydaVerification
                {
                    FAN = fan,
                    UserEmail = userEmail,
                    TransactionId = Guid.NewGuid().ToString("N").ToUpper()
                };
                _context.FaydaVerifications.Add(verification);
            }

            verification.OTP = new Random().Next(100000, 999999).ToString();
            verification.ExpiryTime = DateTime.Now.AddMinutes(5);
            verification.OtpExpiry = verification.ExpiryTime;
            verification.LastOtpRequestTime = DateTime.Now;
            verification.Attempts = 0;
            verification.IsVerified = false;

            // Reset or increment ResendCount
            if (previousRequestTime < oneHourAgo)
            {
                verification.ResendCount = 1;
            }
            else
            {
                verification.ResendCount++;
            }

            await _context.SaveChangesAsync();

            // Send the OTP via Email
            try
            {
                await _emailService.SendOtpEmailAsync(userEmail, verification.OTP);
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"[FAYDA] Error sending OTP email to {userEmail}: {ex.Message}");
                return (false, $"Failed to send OTP email: {ex.Message}. Please check your connection or contact support.", null);
            }
 
            // Log OTP for testing
            Console.WriteLine($"[FAYDA] TR-ID: {verification.TransactionId} | FAN: {fan} | OTP: {verification.OTP} | Sent To: {userEmail}");
 
            return (true, "OTP sent successfully to your email.", verification.OTP);
        }

        public async Task<(bool success, string message)> VerifyOtpAsync(string fan, string otp)
        {
            var verification = await _context.FaydaVerifications.FirstOrDefaultAsync(x => x.FAN == fan);

            if (verification == null)
                return (false, "No OTP request found for this FAN.");

            if (verification.IsLocked)
                return (false, "Account is locked. Please try again later.");

            if (verification.ExpiryTime < DateTime.Now)
                return (false, "OTP has expired.");

            if (verification.OTP != otp)
            {
                verification.Attempts++;
                if (verification.Attempts >= 3)
                {
                    verification.IsLocked = true;
                    await _context.SaveChangesAsync();
                    return (false, "Too many failed attempts. Verification locked.");
                }

                await _context.SaveChangesAsync();
                return (false, "Invalid OTP.");
            }

            verification.IsVerified = true;
            verification.OTP = null;
            verification.IsLocked = true; // Still lock after success to prevent reuse of session

            // Populate identity snapshots
            var registry = await _context.FaydaRegistries.FirstOrDefaultAsync(r => r.FAN == fan);
            if (registry != null)
            {
                verification.VerifiedName = registry.FullName;
                verification.VerifiedDob = registry.DateOfBirth;
                verification.VerifiedPhone = registry.PhoneNumber;
            }

            await _context.SaveChangesAsync();
            return (true, "Verification successful.");
        }

        public async Task<(bool success, string fullName, string phoneNumber, DateTime? dob)> GetIdentityDataAsync(string fan)
        {
            var verification = await _context.FaydaVerifications.FirstOrDefaultAsync(x => x.FAN == fan);
            if (verification == null || !verification.IsVerified)
            {
                return (false, string.Empty, string.Empty, null);
            }

            var record = await _context.FaydaRegistries.FirstOrDefaultAsync(x => x.FAN == fan);
            if (record == null)
            {
                return (false, string.Empty, string.Empty, null);
            }

            return (true, record.FullName, record.PhoneNumber, record.DateOfBirth);
        }

        public async Task CleanupUncompletedVerificationsAsync()
        {
            var threshold = DateTime.Now.AddHours(-24);
            var uncompleted = await _context.FaydaVerifications
                .Where(v => !v.IsVerified && v.LastOtpRequestTime < threshold)
                .ToListAsync();

            if (uncompleted.Any())
            {
                _context.FaydaVerifications.RemoveRange(uncompleted);
                await _context.SaveChangesAsync();
            }
        }
    }
}
