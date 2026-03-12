using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class TenderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TenderController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // GET: Tender/MyTenders
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> MyTenders()
        {
            var userId = GetCurrentUserId();
            var tenders = await _context.Tenders
                .Include(t => t.Category)
                .Include(t => t.Bids)
                .Where(t => t.RetailerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tenders);
        }

        // GET: Tender/AvailableTenders
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> AvailableTenders(int? categoryId)
        {
            var query = _context.Tenders
                .Include(t => t.Category)
                .Include(t => t.Retailer)
                .Where(t => t.Status == "Open" && t.ClosingDate >= DateTime.Today);

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            var tenders = await query
                .OrderBy(t => t.ClosingDate)
                .ToListAsync();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(tenders);
        }

        // GET: Tender/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var tender = await _context.Tenders
                .Include(t => t.Category)
                .Include(t => t.Retailer)
                .Include(t => t.Bids)
                    .ThenInclude(b => b.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (tender == null)
            {
                return NotFound();
            }

            // Security Rule: Retailers can only view their own tenders
            if (User.IsInRole("Retailer") && tender.RetailerId != GetCurrentUserId())
            {
                return Unauthorized("You can only view your own tenders.");
            }

            return View(tender);
        }

        // GET: Tender/Create
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View();
        }

        // POST: Tender/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Create([Bind("Title,CategoryId,Description,Quantity,ClosingDate")] Tender tender)
        {
            // Validation Rule: ClosingDate must be in the future
            if (tender.ClosingDate.Date <= DateTime.Today)
            {
                ModelState.AddModelError("ClosingDate", "The closing date must be in the future.");
            }

            if (ModelState.IsValid)
            {
                tender.RetailerId = GetCurrentUserId();
                tender.Status = "Open";
                tender.CreatedAt = DateTime.Now;

                _context.Add(tender);
                await _context.SaveChangesAsync();
                
                // Note: Notification system goes here
                
                TempData["SuccessMessage"] = "Tender created successfully.";
                return RedirectToAction(nameof(MyTenders));
            }

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(tender);
        }

        // POST: Tender/Close/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Close(int id)
        {
            var tender = await _context.Tenders.FindAsync(id);
            if (tender == null) return NotFound();

            // Security Rule
            if (tender.RetailerId != GetCurrentUserId()) return Unauthorized();

            if (tender.Status == "Open")
            {
                tender.Status = "Closed";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tender closed manually.";
            }

            return RedirectToAction(nameof(Details), new { id = tender.Id });
        }

        // POST: Tender/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var tender = await _context.Tenders.FindAsync(id);
            if (tender == null) return NotFound();

            // Security Rule
            if (tender.RetailerId != GetCurrentUserId()) return Unauthorized();

            if (tender.Status == "Open" || tender.Status == "Closed")
            {
                tender.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tender cancelled.";
            }

            return RedirectToAction(nameof(Details), new { id = tender.Id });
        }
    }
}
