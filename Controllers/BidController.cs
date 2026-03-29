using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class BidController : Controller
    {
        private readonly IBidService _bidService;
        private readonly ITenderService _tenderService;
        private readonly IPurchaseOrderService _poService;
        private readonly ApplicationDbContext _context;

        public BidController(IBidService bidService, ITenderService tenderService, IPurchaseOrderService poService, ApplicationDbContext context)
        {
            _bidService = bidService;
            _tenderService = tenderService;
            _poService = poService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Supplier"))
            {
                var supplierId = await GetSupplierIdAsync();
                var bids = await _bidService.GetBidsBySupplierAsync(supplierId);
                return View("MyBids", bids);
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> Submit(int tenderId)
        {
            var tender = await _context.Tenders.Include(t => t.TenderItems).FirstOrDefaultAsync(t => t.Id == tenderId);
            if (tender == null || tender.Status != "Published") return NotFound();

            var firstItem = tender.TenderItems.FirstOrDefault();
            var model = new BidSubmitViewModel 
            { 
                TenderId = tenderId,
                Quantity = firstItem?.Quantity ?? 0,
                UnitPrice = firstItem?.EstimatedUnitPrice ?? 0,
                ValidityPeriodDays = 30
            };
            
            ViewBag.Tender = tender;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(BidSubmitViewModel model)
        {
            if (ModelState.IsValid)
            {
                var supplierId = await GetSupplierIdAsync();
                
                // Calculate Totals
                decimal subtotal = model.UnitPrice * model.Quantity;
                decimal discountAmount = subtotal * (model.DiscountPercentage / 100.0m);
                decimal vatableAmount = subtotal - discountAmount;
                decimal vatAmount = vatableAmount * (model.VATPercentage / 100.0m);
                decimal totalAmount = vatableAmount + vatAmount;

                var bid = new TenderBid
                {
                    TenderId = model.TenderId,
                    SupplierId = supplierId,
                    UnitPrice = model.UnitPrice,
                    Quantity = model.Quantity,
                    Subtotal = subtotal,
                    DiscountPercentage = model.DiscountPercentage,
                    VATPercentage = model.VATPercentage,
                    ProposedTotalAmount = totalAmount,
                    DeliveryLeadTimeDays = model.DeliveryLeadTimeDays,
                    ProposedDeliveryDate = model.ProposedDeliveryDate,
                    DeliveryMethod = model.DeliveryMethod,
                    DeliveryCapacity = model.DeliveryCapacity,
                    ValidityPeriodDays = model.ValidityPeriodDays,
                    TechnicalProposal = model.TechnicalProposal,
                    PackagingPlan = model.PackagingPlan,
                    InspectionCompliance = model.InspectionCompliance,
                    PenaltyAcceptance = model.PenaltyAcceptance,
                    Notes = model.Notes
                };

                await _bidService.SubmitBidAsync(bid);
                TempData["SuccessMessage"] = "Your bid has been submitted and evaluated.";
                return RedirectToAction(nameof(Index));
            }
            
            ViewBag.Tender = await _context.Tenders.Include(t => t.TenderItems).FirstOrDefaultAsync(t => t.Id == model.TenderId);
            return View(model);
        }

        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Review(int tenderId)
        {
            var bids = await _bidService.GetBidsForTenderAsync(tenderId);
            ViewBag.Tender = await _tenderService.GetTenderByIdAsync(tenderId);
            return View(bids);
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id, string deliveryAddress)
        {
            var bid = await _bidService.AcceptBidAsync(id);
            if (bid != null)
            {
                await _poService.GeneratePurchaseOrderFromBidAsync(bid.Id, string.IsNullOrEmpty(deliveryAddress) ? "Default Store Address" : deliveryAddress);
            }
            return RedirectToAction("Index", "PurchaseOrder");
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
