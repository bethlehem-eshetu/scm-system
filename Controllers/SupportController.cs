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

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Message cannot be empty." });
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "You must be logged in to submit a ticket." });
            }

            try
            {
                var ticket = new SupportTicket
                {
                    UserId = userId,
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
