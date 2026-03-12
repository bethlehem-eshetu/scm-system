using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize]
    public class BidController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public BidController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // GET: Bid/Submit/5
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> Submit(int tenderId)
        {
            var tender = await _context.Tenders
                .Include(t => t.Category)
                .Include(t => t.Retailer)
                .FirstOrDefaultAsync(m => m.Id == tenderId);

            if (tender == null)
            {
                return NotFound();
            }

            if (tender.Status != "Open" || tender.ClosingDate < DateTime.Today)
            {
                TempData["ErrorMessage"] = "This tender is no longer accepting bids.";
                return RedirectToAction("AvailableTenders", "Tender");
            }

            // Validation Rule: Prevent multiple bids from the same supplier
            var supplierId = GetCurrentUserId();
            var existingBid = await _context.TenderBids
                .AnyAsync(b => b.TenderId == tenderId && b.SupplierId == supplierId);

            if (existingBid)
            {
                TempData["ErrorMessage"] = "You have already submitted a bid for this tender.";
                return RedirectToAction("AvailableTenders", "Tender");
            }

            ViewBag.Tender = tender;
            return View(new TenderBid { TenderId = tenderId });
        }

        // POST: Bid/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> Submit([Bind("TenderId,BidAmount,DeliveryTimeline,BidNotes")] TenderBid bid)
        {
            var tender = await _context.Tenders.FindAsync(bid.TenderId);
            if (tender == null || tender.Status != "Open" || tender.ClosingDate < DateTime.Today)
            {
                return NotFound("Tender not found or no longer open.");
            }

            var supplierId = GetCurrentUserId();
            var existingBid = await _context.TenderBids
                .AnyAsync(b => b.TenderId == bid.TenderId && b.SupplierId == supplierId);

            if (existingBid)
            {
                ModelState.AddModelError("", "You have already submitted a bid for this tender.");
            }

            if (ModelState.IsValid)
            {
                bid.SupplierId = supplierId;
                bid.Status = "Pending";
                bid.SubmittedDate = DateTime.Now;

                _context.Add(bid);
                await _context.SaveChangesAsync();
                
                await _notificationService.SendNotificationAsync(
                    tender.RetailerId, 
                    $"A new bid of {bid.BidAmount.ToString("C")} was submitted for tender '{tender.Title}'.", 
                    "BidSubmitted");

                TempData["SuccessMessage"] = "Your bid was submitted successfully.";
                return RedirectToAction(nameof(MyBids));
            }

            ViewBag.Tender = tender;
            return View(bid);
        }

        // GET: Bid/MyBids
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> MyBids()
        {
            var supplierId = GetCurrentUserId();
            var bids = await _context.TenderBids
                .Include(b => b.Tender)
                    .ThenInclude(t => t.Category)
                .Where(b => b.SupplierId == supplierId)
                .OrderByDescending(b => b.SubmittedDate)
                .ToListAsync();

            return View(bids);
        }

        // GET: Bid/Review/5
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Review(int tenderId)
        {
            var tender = await _context.Tenders
                .Include(t => t.Category)
                .Include(t => t.Bids)
                    .ThenInclude(b => b.Supplier)
                .FirstOrDefaultAsync(t => t.Id == tenderId);

            if (tender == null)
            {
                return NotFound();
            }

            // Security Rule: Retailer must own the Tender
            if (tender.RetailerId != GetCurrentUserId())
            {
                return Unauthorized("You do not have permission to review bids for this tender.");
            }

            return View(tender);
        }

        // POST: Bid/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Accept(int id)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Tender)
                .Include(b => b.Supplier)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bid == null) return NotFound();

            var tender = bid.Tender;

            // Security Rule: Retailer must own the Tender
            if (tender.RetailerId != GetCurrentUserId()) return Unauthorized();

            // Enforce strictly ONE accepted bid
            if (tender.Status == "Awarded")
            {
                TempData["ErrorMessage"] = "This tender has already been awarded.";
                return RedirectToAction(nameof(Review), new { tenderId = tender.Id });
            }

            // Begin acceptance logic
            bid.Status = "Accepted";
            tender.Status = "Awarded";

            // Reject all other bids for this tender
            var otherBids = await _context.TenderBids
                .Where(b => b.TenderId == tender.Id && b.Id != bid.Id)
                .ToListAsync();
            
            foreach (var otherBid in otherBids)
            {
                otherBid.Status = "Rejected";
            }

            // System Action: Generate Purchase Order based on Data Snapshot rules
            var po = new PurchaseOrder
            {
                PONumber = $"PO-T{tender.Id}-{DateTime.Now:yyyyMMdd}-{bid.Id}",
                RetailerId = tender.RetailerId,
                SupplierId = bid.SupplierId,
                TenderBidId = bid.Id,
                ProductId = 1, // Fallback ID because Tender is not strictly tied to a specific System Product, handled separately
                ProductName = $"Tender: {tender.Title}",
                UnitPrice = bid.BidAmount,
                Quantity = tender.Quantity,
                TotalAmount = bid.BidAmount * tender.Quantity,
                Status = "Accepted", // Auto-accepted since it's the result of a mutual tender
                OrderDate = DateTime.Now
            };

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            // Automatically create the Order since the PO is pre-accepted via Tender rules
            var order = new Order
            {
                PurchaseOrderId = po.Id,
                SupplierId = po.SupplierId,
                OrderStatus = "Processing",
                CreatedAt = DateTime.Now
            };
            
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            
            await _notificationService.SendNotificationAsync(
                bid.SupplierId,
                $"Congratulations! Your bid for tender '{tender.Title}' was accepted. PO {po.PONumber} has been generated.",
                "BidAccepted");

            TempData["SuccessMessage"] = $"Bid accepted successfully. Purchase Order {po.PONumber} has been generated.";
            return RedirectToAction(nameof(Review), new { tenderId = tender.Id });
        }

        // POST: Bid/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Reject(int id)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Tender)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bid == null) return NotFound();

            // Security Rule: Retailer must own the Tender
            if (bid.Tender.RetailerId != GetCurrentUserId()) return Unauthorized();

            if (bid.Status == "Pending")
            {
                bid.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bid has been rejected.";
            }

            return RedirectToAction(nameof(Review), new { tenderId = bid.TenderId });
        }
    }
}
