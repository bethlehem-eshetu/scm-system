using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class PenaltyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPenaltyService _penaltyService;

        public PenaltyController(ApplicationDbContext context, IPenaltyService penaltyService)
        {
            _context = context;
            _penaltyService = penaltyService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "";
        }

        // GET: /Penalty
        public async Task<IActionResult> Index()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var penalties = await _context.Penalties
                .Where(p => p.UserId == currentUserId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Get violation count
            ViewBag.ViolationCount = await _penaltyService.GetViolationCount(currentUserId);
            ViewBag.CanSendMessage = await _penaltyService.CanSendMessage(currentUserId);
            ViewBag.UserRole = GetCurrentUserRole();

            return View(penalties);
        }

       
        // POST: /Penalty/Appeal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Appeal(int penaltyId, string appealReason)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            try
            {
                var penalty = await _context.Penalties.FindAsync(penaltyId);

                if (penalty == null)
                {
                    TempData["ErrorMessage"] = "Penalty not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (penalty.UserId != currentUserId)
                {
                    TempData["ErrorMessage"] = "You can only appeal your own penalties.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(appealReason))
                {
                    TempData["ErrorMessage"] = "Please provide a reason for your appeal.";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ Update penalty with appeal info
                penalty.HasAppealed = true;
                penalty.AppealReason = appealReason;
                penalty.AppealDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your appeal has been submitted successfully. An admin will review it shortly.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error submitting appeal: {ex.Message}";
                Console.WriteLine($"Appeal Error: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Penalty/DebugAppeals
        public async Task<IActionResult> DebugAppeals()
        {
            int currentUserId = GetCurrentUserId();
            var penalties = await _context.Penalties
                .Where(p => p.UserId == currentUserId)
                .Select(p => new {
                    p.Id,
                    p.HasAppealed,
                    p.AppealReason,
                    p.AppealDate
                })
                .ToListAsync();

            return Json(penalties);
        }
    }
}