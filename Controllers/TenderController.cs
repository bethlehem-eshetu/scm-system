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
                var retailerId = await GetRetailerIdAsync();
                var tenders = await _tenderService.GetTendersByRetailerAsync(retailerId);
                return View("MyTenders", tenders);
            }
            else if (User.IsInRole("Supplier"))
            {
                var tenders = await _tenderService.GetAllTendersAsync();
                return View("AvailableTenders", tenders.Where(t => t.Status == "Open").ToList());
            }
            
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.ProductCategories.ToListAsync(), "Id", "CategoryName");
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
                    RetailerId = retailerId
                };

                var items = model.Items.Select(i => new TenderItem
                {
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
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tender = await _tenderService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();
            return View(tender);
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
    }
}
