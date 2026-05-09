using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace SCM_System.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            IQueryable<SupportTicket> query = _context.SupportTickets.Include(t => t.User);

            // Admins can see all tickets, normal users only see their own
            if (userRole != "Admin")
            {
                query = query.Where(t => t.UserId == userId);
            }

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(tickets);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Message cannot be empty." });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "You must be logged in to submit a ticket." });
            }

            try
            {
                var ticket = new SupportTicket
                {
                    UserId = userId.Value,
                    Subject = string.IsNullOrWhiteSpace(subject) ? "General Support" : subject,
                    Message = message,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow
                };

                _context.SupportTickets.Add(ticket);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Your ticket has been submitted. Support will contact you shortly." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while saving your ticket: " + ex.Message });
            }
        }
    }
}
