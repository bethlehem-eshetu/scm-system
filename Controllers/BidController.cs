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
        private readonly INotificationService _notificationService;

        public BidController(IBidService bidService, ITenderService tenderService, IPurchaseOrderService poService, ApplicationDbContext context, INotificationService notificationService)
        {
            _bidService = bidService;
            _tenderService = tenderService;
            _poService = poService;
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Supplier"))
            {
                var supplierId = await GetSupplierIdAsync();
                var bids = await _bidService.GetBidsBySupplierAsync(supplierId);
                return RedirectToAction("MyBids", "Tender");
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
                    Status = Request.Form["status"] == "Draft" ? "Draft" : "Pending",
                    PackagingPlan = model.PackagingPlan,
                    InspectionCompliance = model.InspectionCompliance,
                    PenaltyAcceptance = model.PenaltyAcceptance,
                    WarrantyPeriod = model.WarrantyPeriod,
                    WarrantyType = model.WarrantyType,
                    PreviousExperience = model.PreviousExperience,
                    PaymentTerms = model.PaymentTerms,
                    ProductSpecifications = model.ProductSpecifications,
                    QualityCertifications = model.QualityCertifications,
                    InsuranceCoverage = model.InsuranceCoverage,
                    AfterSalesSupport = model.AfterSalesSupport,
                    References = model.References,
                    Notes = model.Notes
                };

                await _bidService.SubmitBidAsync(bid);
                
                if (bid.Status == "Pending")
                {
                    var tenderDetails = await _context.Tenders.Include(t => t.Retailer).ThenInclude(r => r.User).FirstOrDefaultAsync(t => t.Id == model.TenderId);
                    var supplier = await _context.Suppliers.FindAsync(supplierId);
                    if (tenderDetails?.Retailer?.User != null && supplier != null)
                    {
                        await _notificationService.SendNotificationAsync(
                            tenderDetails.Retailer.User.Id, 
                            "New Bid Received", 
                            $"{supplier.CompanyName} submitted a bid for {tenderDetails.ReferenceNumber}", 
                            "Info", 
                            $"/Tender/Details/{model.TenderId}"
                        );
                    }
                }

                TempData["SuccessMessage"] = bid.Status == "Draft" ? "Your bid draft has been saved." : "Your bid has been submitted and evaluated.";
                return RedirectToAction("MyBids", "Tender");
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

        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> ReviewProposal(int id)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Supplier)
                .Include(b => b.Tender)
                    .ThenInclude(t => t.TenderItems)
                        .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bid == null) return NotFound();
            
            return View(bid);
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id, string deliveryAddress)
        {
            var generatedPo = await _bidService.AcceptBidAsync(id, deliveryAddress);
            if (generatedPo != null)
            {
                TempData["SuccessMessage"] = "Bid accepted successfully! Purchase order has been generated.";
                return RedirectToAction("Details", "PurchaseOrder", new { id = generatedPo.PONumber });
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to accept bid or generate purchase order.";
                var bid = await _context.TenderBids.FindAsync(id);
                if (bid != null) return RedirectToAction("Review", new { tenderId = bid.TenderId });
                return RedirectToAction("Index", "Tender");
            }
        }

        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> GetFeedback(int bidId)
        {
            var feedback = await _bidService.GetBidFeedbackAsync(bidId);
            if (feedback == null) return NotFound();
            return PartialView("_BidFeedbackPartial", feedback);
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
