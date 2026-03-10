using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace SCM_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                // Create new user
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    PhoneNumber = model.PhoneNumber,
                    Role = model.Role,
                    AccountStatus = "Pending",
                    IsApproved = false,
                    CreatedAt = DateTime.Now,
                    LoginAttempts = 0,
                    EmailVerified = false,
                    PhoneVerified = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Handle file upload for supplier
                string licenseFilePath = null;
                if (model.Role == "Supplier" && model.LicenseFile != null)
                {
                    // Validate file size (max 5MB)
                    if (model.LicenseFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("LicenseFile", "File size cannot exceed 5MB");
                        return View(model);
                    }

                    // Validate file extension
                    string[] allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
                    string fileExtension = Path.GetExtension(model.LicenseFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("LicenseFile", "Only PDF, JPG, JPEG, and PNG files are allowed");
                        return View(model);
                    }

                    // Create unique filename
                    string fileName = $"supplier_{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";

                    // Ensure upload directory exists
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "licenses");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Save file
                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.LicenseFile.CopyToAsync(fileStream);
                    }

                    licenseFilePath = $"/uploads/licenses/{fileName}";
                }

                // If supplier, create supplier record
                if (model.Role == "Supplier")
                {
                    var supplier = new Supplier
                    {
                        UserId = user.Id,
                        CompanyName = model.CompanyName,
                        BusinessType = model.BusinessType,
                        LicenseNumber = model.LicenseNumber,
                        LicenseFilePath = licenseFilePath,
                        TaxIdentificationNumber = model.TaxIdentificationNumber,
                        CompanyAddress = model.CompanyAddress,
                        City = model.City,
                        Country = model.Country,
                        Website = model.Website,
                        Description = model.CompanyDescription,
                        VerificationStatus = "Pending",
                        CreatedAt = DateTime.Now
                    };
                    _context.Suppliers.Add(supplier);
                }

                // If retailer, create retailer record
                if (model.Role == "Retailer")
                {
                    var retailer = new Retailer
                    {
                        UserId = user.Id,
                        BusinessName = model.BusinessName,
                        BusinessType = model.RetailerBusinessType,
                        TaxIdentificationNumber = model.TaxIdentificationNumber,
                        BusinessLicenseNumber = model.RetailerLicenseNumber,
                        BusinessAddress = model.BusinessAddress,
                        City = model.City,
                        Country = model.Country,
                        StoreSize = model.StoreSize,
                        Description = model.BusinessDescription,
                        IsVerified = false,
                        CreatedAt = DateTime.Now
                    };
                    _context.Retailers.Add(retailer);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration successful! Please wait for admin approval.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                // Check login attempts (basic brute force protection)
                if (user.LoginAttempts >= 5)
                {
                    ModelState.AddModelError("", "Account locked. Please contact support.");
                    return View(model);
                }

                if (user.PasswordHash != HashPassword(model.Password))
                {
                    // Increment login attempts
                    user.LoginAttempts = (user.LoginAttempts ?? 0) + 1;
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                // Reset login attempts on successful login
                user.LoginAttempts = 0;

                if (!user.IsApproved)
                {
                    // Check if supplier and needs verification
                    if (user.Role == "Supplier")
                    {
                        var supplier = await _context.Suppliers
                            .FirstOrDefaultAsync(s => s.UserId == user.Id);

                        if (supplier != null && supplier.VerificationStatus == "Pending")
                        {
                            ModelState.AddModelError("", "Your supplier account is pending verification. Admin will review your license document.");
                        }
                        else if (supplier != null && supplier.VerificationStatus == "Rejected")
                        {
                            ModelState.AddModelError("", "Your supplier application was rejected. Please contact admin.");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Your account is pending admin approval");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Your account is pending admin approval");
                    }
                    return View(model);
                }

                // Update last login
                user.LastLoginAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Store user in session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.FullName);

                // Redirect based on role
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (user.Role == "Supplier")
                {
                    return RedirectToAction("Dashboard", "Supplier");
                }
                else if (user.Role == "Retailer")
                {
                    return RedirectToAction("Dashboard", "Retailer");
                }

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}