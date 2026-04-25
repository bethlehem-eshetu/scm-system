using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class TenderController : Controller
    {
        private readonly ITenderService _tenderService;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public TenderController(ITenderService tenderService, ApplicationDbContext context, INotificationService notificationService)
        {
            _tenderService = tenderService;
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Retailer"))
            {
                return RedirectToAction(nameof(MyTenders));
            }
            else if (User.IsInRole("Supplier"))
            {
                return RedirectToAction(nameof(AvailableTenders));
            }
            
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> AvailableTenders()
        {
            var supplierId = await GetSupplierIdAsync();
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return Unauthorized();

            // Get all published tenders with category info
            var publishedTenders = await _context.Tenders
                .Include(t => t.Category)
                .Include(t => t.Retailer)
                .Where(t => t.Status == "Published")
                .ToListAsync();

            // Get supplier's registered categories
            var supplierCategoryIds = await _context.SupplierCategories
                .Where(sc => sc.SupplierId == supplierId)
                .Select(sc => sc.CategoryId)
                .ToListAsync();

            // Filter: Match if supplier has the tender category OR the parent category
            var targetedTenders = publishedTenders.Where(t => 
                supplierCategoryIds.Contains(t.CategoryId) || 
                (t.Category.ParentCategoryId.HasValue && supplierCategoryIds.Contains(t.Category.ParentCategoryId.Value))
            ).ToList();

            return View(targetedTenders);
        }

        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> MyTenders()
        {
            var retailerId = await GetRetailerIdAsync();
            var tenders = await _tenderService.GetTendersByRetailerAsync(retailerId);
            return View(tenders);
        }

        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Create()
        {
            var retailerId = await GetRetailerIdAsync();
            if (retailerId == 0) return Unauthorized();

            // Load categories hierarchically (Parent > Children)
            var allCategories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Level).ThenBy(c => c.CategoryName)
                .ToListAsync();

            var categoryList = new List<SelectListItem>();
            var mainCategories = allCategories.Where(c => c.Level <= 1).ToList();
            
            foreach (var main in mainCategories)
            {
                var group = new SelectListGroup { Name = main.CategoryName };
                categoryList.Add(new SelectListItem { Value = main.Id.ToString(), Text = main.CategoryName, Group = group });
                
                var subs = allCategories.Where(c => c.ParentCategoryId == main.Id).ToList();
                foreach (var sub in subs)
                {
                    categoryList.Add(new SelectListItem { Value = sub.Id.ToString(), Text = "— " + sub.CategoryName, Group = group });
                }
            }

            ViewBag.CategoryList = categoryList;
            ViewBag.Products = await _context.Products.OrderBy(p => p.ProductName).ToListAsync();
            
            var model = new TenderCreateViewModel();
            model.SubmissionDeadline = DateTime.Now.AddDays(14);
            model.ExpectedDeliveryDate = DateTime.Now.AddDays(30);
            model.Items.Add(new TenderItemViewModel { Quantity = 1 }); 
            
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TenderCreateViewModel model)
        {
            var retailerId = await GetRetailerIdAsync();
            if (retailerId == 0) return Unauthorized();

            if (ModelState.IsValid)
            {
                string attachmentPath = null;
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tenders");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = Guid.NewGuid().ToString() + "_" + model.Attachment.FileName;
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Attachment.CopyToAsync(fileStream);
                    }
                    attachmentPath = "/uploads/tenders/" + fileName;
                }

                var tender = new Tender
                {
                    ReferenceNumber = "RFQ-" + DateTime.Now.Ticks.ToString().Substring(8),
                    Title = model.Title,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    SubmissionDeadline = model.SubmissionDeadline,
                    ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                    RetailerId = retailerId,
                    PackagingRequirements = model.PackagingRequirements,
                    DeliveryLocation = model.DeliveryLocation,
                    InspectionRequirement = model.InspectionRequirement,
                    Language = model.Language,
                    PaymentTerms = model.PaymentTerms,
                    PriceWeight = model.PriceWeight,
                    TechnicalWeight = model.TechnicalWeight,
                    DeliveryWeight = model.DeliveryWeight,
                    BudgetMin = model.BudgetMin,
                    BudgetMax = model.BudgetMax,
                    AllowPartialBids = model.AllowPartialBids,
                    AttachmentPath = attachmentPath,
                    PreferredSuppliers = model.PreferredSuppliers
                };

                var items = model.Items.Select(i => new TenderItem
                {
                    ProductId = i.IsCustom ? (int?)null : i.ProductId,
                    ProductName = i.ProductName,
                    Description = i.Description,
                    Specifications = i.Specifications,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    EstimatedUnitPrice = i.EstimatedUnitPrice
                }).ToList();

                await _tenderService.CreateTenderAsync(tender, items);

                TempData["SuccessMessage"] = "Tender published successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            // Re-load categories on error
            var allCategories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Level).ThenBy(c => c.CategoryName)
                .ToListAsync();

            var categoryList = new List<SelectListItem>();
            var mainCategories = allCategories.Where(c => c.Level == 1).ToList();
            foreach (var main in mainCategories)
            {
                var group = new SelectListGroup { Name = main.CategoryName };
                categoryList.Add(new SelectListItem { Value = main.Id.ToString(), Text = main.CategoryName, Group = group, Selected = main.Id == model.CategoryId });
                var subs = allCategories.Where(c => c.ParentCategoryId == main.Id).ToList();
                foreach (var sub in subs)
                {
                    categoryList.Add(new SelectListItem { Value = sub.Id.ToString(), Text = "— " + sub.CategoryName, Group = group, Selected = sub.Id == model.CategoryId });
                }
            }

            ViewBag.CategoryList = categoryList;
            ViewBag.Products = await _context.Products.OrderBy(p => p.ProductName).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Json(new { name = product.ProductName, unit = product.Unit });
        }

        public async Task<IActionResult> Details(int id)
        {
            var tender = await _tenderService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();
            return View(tender);
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Award(int tenderId, int bidId)
        {
            var success = await _tenderService.AwardTenderAsync(tenderId, bidId);
            if (success)
            {
                TempData["SuccessMessage"] = "Tender awarded successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to award tender.";
            }

            return RedirectToAction(nameof(Details), new { id = tenderId });
        }



        private async Task<int> GetRetailerIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var retailer = await _context.Retailers.Include(r => r.User).FirstOrDefaultAsync(r => r.UserId == userId);
                if (retailer == null || !retailer.User.IsFaydaVerified) return 0;
                return retailer.Id;
            }
            return 0;
        }

        private async Task<int> GetSupplierIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var supplier = await _context.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
                if (supplier == null || !supplier.User.IsFaydaVerified) return 0;
                return supplier.Id;
            }
            return 0;
        }
    }
}
