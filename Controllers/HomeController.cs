using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;

        public HomeController(IEmailService emailService, ApplicationDbContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        [HttpGet("FixDatabase")]
        public async Task<IActionResult> FixDatabase()
        {
            try
            {
                string sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeliveryRatings]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE DeliveryRatings (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            PurchaseOrderId INT NOT NULL,
                            DriverEmployeeId INT NOT NULL,
                            RetailerId INT NOT NULL,
                            Timeliness INT NOT NULL,
                            Professionalism INT NOT NULL,
                            VehicleCondition INT NOT NULL,
                            Communication INT NOT NULL,
                            Comment NVARCHAR(500) NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_DeliveryRatings_PurchaseOrders FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrders(Id),
                            CONSTRAINT FK_DeliveryRatings_Retailers FOREIGN KEY (RetailerId) REFERENCES Retailers(Id),
                            CONSTRAINT FK_DeliveryRatings_SupplierEmployees FOREIGN KEY (DriverEmployeeId) REFERENCES SupplierEmployees(Id)
                        )
                    END";
                
                await _context.Database.ExecuteSqlRawAsync(sql);
                return Content("Database table 'DeliveryRatings' checked/created successfully.");
            }
            catch (Exception ex)
            {
                return Content("Error fixing database: " + ex.Message + (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
            }
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