using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SCM_System.Controllers
{
    public class OtpRequestModel { public string FAN { get; set; } public string Email { get; set; } }
    public class OtpVerifyModel { public string FAN { get; set; } public string OTP { get; set; } }

    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly SCM_System.Services.IFaydaService _faydaService;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, SCM_System.Services.IFaydaService faydaService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _faydaService = faydaService;
        }

        // POST: /Account/RequestFaydaOtp
        [HttpPost]
        public async Task<IActionResult> RequestFaydaOtp([FromBody] OtpRequestModel model)
        {
            if (string.IsNullOrEmpty(model?.FAN)) return Json(new { success = false, message = "FAN is required." });
            if (string.IsNullOrEmpty(model?.Email)) return Json(new { success = false, message = "Email is required to receive OTP." });
            
            var result = await _faydaService.GenerateOtpAsync(model.FAN, model.Email);
            
            return Json(new { success = result.success, message = result.message });
        }

        // POST: /Account/VerifyFaydaOtp
        [HttpPost]
        public async Task<IActionResult> VerifyFaydaOtp([FromBody] OtpVerifyModel model)
        {
            if (string.IsNullOrEmpty(model?.FAN) || string.IsNullOrEmpty(model?.OTP)) return Json(new { success = false, message = "FAN and OTP are required." });
            var result = await _faydaService.VerifyOtpAsync(model.FAN, model.OTP);
            if (result.success)
            {
                var faydaData = await _faydaService.GetIdentityDataAsync(model.FAN ?? "");
                return Json(new { success = true, fullName = faydaData.fullName, phoneNumber = faydaData.phoneNumber, dateOfBirth = faydaData.dob });
            }
            return Json(new { success = false, message = result.message });
        }

        // GET: /Account/Register
        public async Task<IActionResult> Register()
        {
            ViewBag.Categories = await _context.ProductCategories
                .Include(c => c.SubCategories)
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            Console.WriteLine("========== REGISTER METHOD STARTED ==========");
            Console.WriteLine($"Role: {model.Role}");
            Console.WriteLine($"Email: {model.Email}");
            Console.WriteLine($"City received: '{model.City}'");

            try
            {
                // Validate common fields
                if (string.IsNullOrEmpty(model.FullName))
                    ModelState.AddModelError("FullName", "Full name is required");

                if (string.IsNullOrEmpty(model.Email))
                    ModelState.AddModelError("Email", "Email is required");

                if (string.IsNullOrEmpty(model.Password))
                    ModelState.AddModelError("Password", "Password is required");

                if (model.Password != model.ConfirmPassword)
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match");

                if (string.IsNullOrEmpty(model.PhoneNumber))
                    ModelState.AddModelError("PhoneNumber", "Phone number is required");

                if (string.IsNullOrEmpty(model.Role))
                    ModelState.AddModelError("Role", "Please select a role");

                // City is required for BOTH roles
                if (string.IsNullOrEmpty(model.City))
                    ModelState.AddModelError("City", "City is required");

                // Validate based on role
                if (model.Role == "Supplier")
                {
                    if (string.IsNullOrEmpty(model.CompanyName))
                        ModelState.AddModelError("CompanyName", "Company name is required");

                    // Industry Focus (BusinessType) is now redundant as Categories define the scope
                    
                    if (string.IsNullOrEmpty(model.LicenseNumber))
                        ModelState.AddModelError("LicenseNumber", "License number is required");

                    if (model.LicenseFile == null)
                        ModelState.AddModelError("LicenseFile", "License document is required");

                    if (string.IsNullOrEmpty(model.TaxIdentificationNumber))
                        ModelState.AddModelError("TaxIdentificationNumber", "Tax identification number is required");

                    if (string.IsNullOrEmpty(model.CompanyAddress))
                        ModelState.AddModelError("CompanyAddress", "Company address is required");

                    // Website and Description are optional
                }

                // FAN Validation (Simulated Fayda)
                if (string.IsNullOrEmpty(model.FAN) || !System.Text.RegularExpressions.Regex.IsMatch(model.FAN, @"^\d{16}$"))
                {
                    ModelState.AddModelError("FAN", "FAN must be a valid 16-digit number.");
                }

                // Duplicate FAN Check
                if (await _context.Users.AnyAsync(u => u.FAN == model.FAN))
                {
                    ModelState.AddModelError("FAN", "This Fayda ID is already registered.");
                }

                var faydaData = await _faydaService.GetIdentityDataAsync(model.FAN ?? "");
                if (!faydaData.success)
                {
                    ModelState.AddModelError("FAN", "Fayda verification is required. Please verify your FAN first.");
                }
                else
                {
                    // Force the actual verified values for FullName, ignoring what user typed
                    // PhoneNumber is editable and remains whatever they typed.
                    model.FullName = faydaData.fullName;
                }

                // Date of Birth check
                if (!model.DateOfBirth.HasValue)
                {
                    ModelState.AddModelError("DateOfBirth", "Date of birth is required.");
                }
                else if (model.Role == "Retailer")
                {
                    if (string.IsNullOrEmpty(model.BusinessName))
                        ModelState.AddModelError("BusinessName", "Business name is required");

                    if (string.IsNullOrEmpty(model.RetailerBusinessType))
                        ModelState.AddModelError("RetailerBusinessType", "Business type is required");

                    if (string.IsNullOrEmpty(model.RetailerLicenseNumber))
                        ModelState.AddModelError("RetailerLicenseNumber", "Business license number is required");

                    if (string.IsNullOrEmpty(model.BusinessAddress))
                        ModelState.AddModelError("BusinessAddress", "Business address is required");

                    if (string.IsNullOrEmpty(model.StoreSize))
                        ModelState.AddModelError("StoreSize", "Store size is required");

                    // Tax Id and Description are optional
                }

                // Check for duplicate license numbers and TIN
                if (model.Role == "Supplier")
                {
                    if (!string.IsNullOrEmpty(model.LicenseNumber))
                    {
                        if (await _context.Suppliers.AnyAsync(s => s.LicenseNumber == model.LicenseNumber))
                        {
                            ModelState.AddModelError("LicenseNumber", "This license number is already registered.");
                        }
                    }

                    if (!string.IsNullOrEmpty(model.TaxIdentificationNumber))
                    {
                        if (await _context.Suppliers.AnyAsync(s => s.TaxIdentificationNumber == model.TaxIdentificationNumber))
                        {
                            ModelState.AddModelError("TaxIdentificationNumber", "This Tax Identification Number (TIN) is already registered.");
                        }
                    }

                    if (model.SelectedCategoryIds == null || !model.SelectedCategoryIds.Any())
                    {
                        ModelState.AddModelError("SelectedCategoryIds", "Please select at least one business category.");
                    }
                }
                else if (model.Role == "Retailer")
                {
                    if (!string.IsNullOrEmpty(model.RetailerLicenseNumber))
                    {
                        if (await _context.Retailers.AnyAsync(r => r.BusinessLicenseNumber == model.RetailerLicenseNumber))
                        {
                            ModelState.AddModelError("RetailerLicenseNumber", "This business license number is already registered.");
                        }
                    }

                    // Retailers no longer select categories during registration
                    // Validation for SelectedRetailerCategoryIds removed
                }

                // If there are validation errors, return to form
                if (ModelState.ErrorCount > 0)
                {
                    Console.WriteLine($"Validation failed with {ModelState.ErrorCount} errors");
                    foreach (var key in ModelState.Keys)
                    {
                        foreach (var error in ModelState[key].Errors)
                        {
                            Console.WriteLine($"- {key}: {error.ErrorMessage}");
                        }
                    }
                    // Load hierarchical categories for the view
                    ViewBag.Categories = await _context.ProductCategories
                        .Include(c => c.SubCategories)
                        .Where(c => c.ParentCategoryId == null)
                        .OrderBy(c => c.CategoryName)
                        .ToListAsync();
                    return View(model);
                }

                // Check if email already exists
                Console.WriteLine("Checking if email exists...");
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    Console.WriteLine("Email already exists!");
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                // Start a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create new user
                    Console.WriteLine("Creating new user...");
                    var user = new User
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        PasswordHash = HashPassword(model.Password),
                        PhoneNumber = model.PhoneNumber,
                        Role = model.Role,
                        EmailVerified = false,
                        PhoneVerified = false,
                        FAN = model.FAN,
                        DateOfBirth = model.DateOfBirth,
                        IsFaydaVerified = true,
                        FaydaStatus = "Verified",
                        FaydaVerifiedAt = DateTime.Now,
                        VerifiedFullName = model.FullName,
                        VerifiedPhoneNumber = model.PhoneNumber,
                        AccountStatus = "Pending",
                        IsApproved = false,
                        ApprovalStatus = "Pending"
                    };

                    // ALL newly registered users are PENDING and restricted from logging in
                    user.IsApproved = false;
                    user.AccountStatus = "Pending";

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"User created with ID: {user.Id}");

                    // Handle file upload for supplier
                    string licenseFilePath = null;
                    if (model.Role == "Supplier" && model.LicenseFile != null)
                    {
                        Console.WriteLine("Processing file upload...");

                        // Validate file size (max 5MB)
                        if (model.LicenseFile.Length > 5 * 1024 * 1024)
                        {
                            await transaction.RollbackAsync();
                            ModelState.AddModelError("LicenseFile", "File size cannot exceed 5MB");
                            return View(model);
                        }

                        // Validate file extension
                        string[] allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
                        string fileExtension = Path.GetExtension(model.LicenseFile.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            await transaction.RollbackAsync();
                            ModelState.AddModelError("LicenseFile", "Only PDF, JPG, JPEG, and PNG files are allowed");
                            return View(model);
                        }

                        // Create unique filename
                        string fileName = $"supplier_{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";

                        string webRootPath = _webHostEnvironment.WebRootPath;
                        if (string.IsNullOrEmpty(webRootPath))
                        {
                            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        }
                        
                        // Ensure upload directory exists
                        string uploadPath = Path.Combine(webRootPath, "uploads", "licenses");
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
                        Console.WriteLine($"File saved: {licenseFilePath}");
                    }

                    // If supplier, create supplier record
                    if (model.Role == "Supplier")
                    {
                        Console.WriteLine("Creating supplier record...");

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
                            Country = string.IsNullOrEmpty(model.Country) ? "Ethiopia" : model.Country,
                            Website = model.Website,
                            Description = model.CompanyDescription,
                            VerificationStatus = "Pending",
                            CreatedAt = DateTime.Now
                        };

                        _context.Suppliers.Add(supplier);
                        await _context.SaveChangesAsync(); // Save to get Supplier.Id
                        Console.WriteLine("Supplier record added to context");

                        // Add Business Categories
                        if (model.SelectedCategoryIds != null && model.SelectedCategoryIds.Any())
                        {
                            foreach (var categoryId in model.SelectedCategoryIds)
                            {
                                _context.SupplierCategories.Add(new SupplierCategory
                                {
                                    SupplierId = supplier.Id,
                                    CategoryId = categoryId
                                });
                            }
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"{model.SelectedCategoryIds.Count} categories associated with supplier");
                        }

                        // Create notification for admin about new supplier
                        Console.WriteLine("Creating admin notification...");
                        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                        if (adminUser != null)
                        {
                            Console.WriteLine($"Admin user found: {adminUser.Email}");
                            var notification = new Notification
                            {
                                UserId = adminUser.Id,
                                Title = "New Supplier Registration",
                                Message = $"New supplier '{model.CompanyName}' has registered and is waiting for approval.",
                                Type = "Info",
                                CreatedAt = DateTime.Now,
                                IsRead = false,
                                ActionUrl = "/Admin/PendingSuppliers"  // ✅ FIXED: Added ActionUrl
                            };
                            _context.Notifications.Add(notification);
                            Console.WriteLine("Admin notification added");
                        }
                        else
                        {
                            Console.WriteLine("WARNING: No admin user found!");
                        }
                    }

                    // If retailer, create retailer record
                    if (model.Role == "Retailer")
                    {
                        Console.WriteLine("Creating retailer record...");

                        var retailer = new Retailer
                        {
                            UserId = user.Id,
                            BusinessName = model.BusinessName,
                            BusinessType = model.RetailerBusinessType,
                            TaxIdentificationNumber = model.TaxIdentificationNumber,
                            BusinessLicenseNumber = model.RetailerLicenseNumber,
                            BusinessAddress = model.BusinessAddress,
                            City = model.City,
                            Country = string.IsNullOrEmpty(model.Country) ? "Ethiopia" : model.Country,
                            StoreSize = model.StoreSize,
                            Description = model.BusinessDescription,
                            IsVerified = false,
                            CreatedAt = DateTime.Now
                        };

                        _context.Retailers.Add(retailer);
                        await _context.SaveChangesAsync(); // Save to get Retailer.Id
                        Console.WriteLine("Retailer record added to context");

                        // Add Purchase Categories for Retailer REMOVED as per new design

                        // Create notification for admin about new retailer
                        Console.WriteLine("Creating admin notification...");
                        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                        if (adminUser != null)
                        {
                            Console.WriteLine($"Admin user found: {adminUser.Email}");
                            var notification = new Notification
                            {
                                UserId = adminUser.Id,
                                Title = "New Retailer Registration",
                                Message = $"New retailer '{model.BusinessName}' has registered and is waiting for approval.",
                                Type = "Info",
                                CreatedAt = DateTime.Now,
                                IsRead = false,
                                ActionUrl = "/Admin/PendingRetailers"
                            };
                            _context.Notifications.Add(notification);
                            Console.WriteLine("Admin notification added");
                        }
                        else
                        {
                            Console.WriteLine("WARNING: No admin user found!");
                        }
                    }

                    // Save all changes
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                    Console.WriteLine("Transaction committed successfully");

                    // Set success message
                    TempData["SuccessMessage"] = $"✅ Registration successful! Your {model.Role.ToLower()} account is pending administrative review. We have recorded your Fayda verification status: {user.FaydaStatus}.";

                    Console.WriteLine("Registration completed successfully!");
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    // Rollback transaction on error
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ Transaction error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ REGISTRATION ERROR: {ex.Message}");
                string errorMsg = $"[{DateTime.Now}] ❌ REGISTRATION ERROR: {ex.Message}\nStack: {ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                    errorMsg += $"   Inner: {ex.InnerException.Message}\nInner Stack: {ex.InnerException.StackTrace}\n";
                }
                
                try { System.IO.File.AppendAllText(@"c:\SCM_System\RegLog.txt", errorMsg); } catch {}

                TempData["ErrorMessage"] = "An error occurred during registration. Please try again. " + ex.Message;
                return View(model);
            }
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            // If user is already logged in, redirect to appropriate dashboard
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (!string.IsNullOrEmpty(role))
                {
                    return RedirectToAction("Dashboard", role);
                }
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == model.Email);

                    if (user == null)
                    {
                        ModelState.AddModelError("", "❌ Invalid email or password");
                        return View(model);
                    }

                    // Check login attempts
                    if (user.LoginAttempts >= 5)
                    {
                        TempData["ErrorMessage"] = "🔒 Account locked. Too many failed attempts. Please contact support.";
                        return RedirectToAction("Login");
                    }

                    if (user.PasswordHash != HashPassword(model.Password))
                    {
                        // Increment login attempts
                        user.LoginAttempts = (user.LoginAttempts ?? 0) + 1;
                        await _context.SaveChangesAsync();

                        int remainingAttempts = 5 - (user.LoginAttempts ?? 0);
                        if (remainingAttempts > 0)
                        {
                            ModelState.AddModelError("", $"❌ Invalid password. {remainingAttempts} attempts remaining.");
                        }
                        else
                        {
                            ModelState.AddModelError("", "❌ Account locked. Too many failed attempts.");
                        }
                        return View(model);
                    }

                    // ADMIN & EMPLOYEE BYPASS: Admins and Supplier Employees (Warehouse/Delivery) bypass administrative/fayda approval checks
                    bool isSpecialAccount = user.Role == "Admin" || user.Role == "WarehouseManager" || user.Role == "DeliveryAgent";

                    if (!isSpecialAccount)
                    {
                        if (user.AccountStatus == "Rejected")
                        {
                            TempData["ErrorMessage"] = $"❌ Your account has been rejected. Reason: {user.RejectionReason ?? "No reason provided."}";
                            return RedirectToAction("Login");
                        }

                        // Check if user is approved
                        if (!user.IsApproved)
                        {
                            TempData["ErrorMessage"] = "⏳ Your account is pending administrative review. Please check back later.";
                            return RedirectToAction("Login");
                        }

                        // Check if account is active
                        if (user.AccountStatus != "Active")
                        {
                            TempData["ErrorMessage"] = $"❌ Your account is {user.AccountStatus}. Please contact support.";
                            return RedirectToAction("Login");
                        }

                        // Block login if Fayda NOT verified OR not approved (ApprovalStatus Check)
                        if (!user.IsFaydaVerified || user.ApprovalStatus != "Approved")
                        {
                            TempData["ErrorMessage"] = "⏳ Your account is pending administrative approval or Fayda Identity verification.";
                            return RedirectToAction("Login");
                        }
                    }

                    // Update last login
                    user.LastLoginAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Store user in session
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetString("UserName", user.FullName);

                    // Add proper identity claims authentication
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), 
                        authProperties);

                    // Set success message
                    TempData["SuccessMessage"] = $"👋 Welcome back, {user.FullName}!";

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
                    else if (user.Role == "WarehouseManager")
                    {
                        return RedirectToAction("Dashboard", "Warehouse");
                    }
                    else if (user.Role == "DeliveryAgent")
                    {
                        return RedirectToAction("MyDeliveries", "Delivery");
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Login Error: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred during login. Please try again.");
                    return View(model);
                }
            }

            return View(model);
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            string userName = HttpContext.Session.GetString("UserName") ?? "User";
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = $"👋 Goodbye, {userName}! You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
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