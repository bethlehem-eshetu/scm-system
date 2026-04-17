using Microsoft.AspNetCore.Mvc;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEmailService _emailService;

        public HomeController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> TestEmail()
        {
            try
            {
                await _emailService.SendEmailAsync("beshetu19@gmail.com", "Test Email", "<h1>✅ Working!</h1><p>Your SCM email is configured correctly.</p>");
                return Content("✅ Email sent! Check your inbox.");
            }
            catch (Exception ex)
            {
                return Content($"❌ Error: {ex.Message}");
            }
        }
    }
}