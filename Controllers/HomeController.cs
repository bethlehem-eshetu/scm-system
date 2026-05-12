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
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Supplier"))
                    return RedirectToAction("Dashboard", "Supplier");
                if (User.IsInRole("Retailer"))
                    return RedirectToAction("Dashboard", "Retailer");
                if (User.IsInRole("Warehouse") || User.IsInRole("WarehouseManager"))
                    return RedirectToAction("Dashboard", "Warehouse");
                if (User.IsInRole("Delivery"))
                    return RedirectToAction("Dashboard", "Delivery");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Services()
        {
            return View();
        }

        public IActionResult HowItWorks()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Contact(string fullName, string email, string subject, string message)
        {
            try
            {
                var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0b3d60;'>New Contact Message</h2>
                    <p><strong>From:</strong> {fullName} ({email})</p>
                    <p><strong>Subject:</strong> {subject}</p>
                    <hr />
                    <p style='white-space: pre-wrap;'>{message}</p>
                </div>";

                await _emailService.SendEmailAsync("admin@ethiochain.com", $"Contact Form: {subject}", emailBody, replyTo: email);
                TempData["SuccessMessage"] = "Thank you for contacting us! We will get back to you shortly.";
            }
            catch (System.Exception)
            {
                TempData["ErrorMessage"] = "Sorry, there was an error sending your message. Please try again later.";
            }
            
            return RedirectToAction("Contact");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}