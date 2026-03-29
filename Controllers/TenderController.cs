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

        public TenderController(ITenderService tenderService, ApplicationDbContext context)
        {
            _tenderService = tenderService;
            _context = context;
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

            // Get all published tenders
            var allTenders = await _tenderService.GetAllTendersAsync();
            var publishedTenders = allTenders.Where(t => t.Status == "Published");

            // Filter: Only show tenders where the supplier has products in that category
            var supplierCategoryIds = await _context.Products
                .Where(p => p.SupplierId == supplierId)
                .Select(p => p.CategoryId)
                .Distinct()
                .ToListAsync();

            var targetedTenders = publishedTenders.Where(t => supplierCategoryIds.Contains(t.CategoryId));

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
            ViewBag.Categories = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName");
            ViewBag.Products = await _context.Products.OrderBy(p => p.ProductName).ToListAsync();
            
            var model = new TenderCreateViewModel();
            model.Items.Add(new TenderItemViewModel()); // Default one item
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TenderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var retailerId = await GetRetailerIdAsync();
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
                    DeliveryWeight = model.DeliveryWeight
                };

                var items = model.Items.Select(i => new TenderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    EstimatedUnitPrice = i.EstimatedUnitPrice
                }).ToList();

                await _tenderService.CreateTenderAsync(tender, items);
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.Categories = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName", model.CategoryId);
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
                var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
                return retailer?.Id ?? 0;
            }
            return 0;
        }

        private async Task<int> GetSupplierIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
                return supplier?.Id ?? 0;
            }
            return 0;
        }
    }
}
