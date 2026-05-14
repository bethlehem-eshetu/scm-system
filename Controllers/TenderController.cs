using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;
using System.Security.Claims;
using SCM_System.Models.Enums;

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

        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> MyBids()
        {
            var supplierId = await GetSupplierIdAsync();
            if (supplierId == 0) return Unauthorized();

            var bids = await _context.TenderBids
                .Include(b => b.Tender)
                    .ThenInclude(t => t.Category)
                .Where(b => b.SupplierId == supplierId)
                .OrderByDescending(b => b.SubmittedAt)
                .ToListAsync();

            return View(bids);
        }

        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> AwardedContracts()
        {
            var supplierId = await GetSupplierIdAsync();
            if (supplierId == 0) return Unauthorized();

            var bids = await _context.TenderBids
                .Include(b => b.Tender)
                    .ThenInclude(t => t.Category)
                .Where(b => b.SupplierId == supplierId && b.IsWinningBid)
                .OrderByDescending(b => b.SubmittedAt)
                .ToListAsync();

            return View(bids);
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

                var productIds = model.Items.Where(i => !i.IsCustom && i.ProductId.HasValue).Select(i => i.ProductId.Value).ToList();
                var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ProductName);

                var items = model.Items.Select(i => new TenderItem
                {
                    ProductId = i.IsCustom ? (int?)null : i.ProductId,
                    ProductName = i.IsCustom ? (i.ProductName ?? "Custom Product") : (i.ProductId.HasValue && products.ContainsKey(i.ProductId.Value) ? products[i.ProductId.Value] : (i.ProductName ?? "Unknown Product")),
                    Description = i.Description,
                    Specifications = i.Specifications,
                    Quantity = i.Quantity,
                    Unit = i.Unit ?? "pcs",
                    EstimatedUnitPrice = i.EstimatedUnitPrice
                }).ToList();

                await _tenderService.CreateTenderAsync(tender, items);

                var targetSuppliers = await _context.SupplierCategories
                    .Where(sc => sc.CategoryId == tender.CategoryId)
                    .Select(sc => sc.Supplier.UserId)
                    .ToListAsync();

                foreach (var suppUserId in targetSuppliers)
                {
                    await _notificationService.SendNotificationAsync(
                        suppUserId, 
                        "New Tender Published", 
                        $"A new tender '{tender.Title}' has been published in your preferred category.", 
                        "Alert", 
                        $"/Tender/Details/{tender.Id}"
                    );
                }

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
            // Pre-load associations for notifications
            var tenderAssociations = await _context.Tenders
                .Include(t => t.Bids)
                    .ThenInclude(b => b.Supplier)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(t => t.Id == tenderId);

            var success = await _tenderService.AwardTenderAsync(tenderId, bidId);
            if (success && tenderAssociations != null)
            {
                // Trigger notification for the winning supplier
                var winningBid = tenderAssociations.Bids.FirstOrDefault(b => b.Id == bidId);
                if (winningBid?.Supplier?.User != null)
                {
                    await _notificationService.SendNotificationAsync(
                        winningBid.Supplier.User.Id, 
                        "Bid Accepted! 🎉", 
                        $"Your bid for {tenderAssociations.ReferenceNumber} was accepted! Create PO from Awarded Contracts.", 
                        "Approval", 
                        "/Tender/AwardedContracts"
                    );

                    // PO Notification directly per user prompt specifications
                    await _notificationService.SendNotificationAsync(
                        winningBid.Supplier.User.Id, 
                        "PO Generated", 
                        $"New PO created from won tender {tenderAssociations.ReferenceNumber}.", 
                        "System", 
                        "/Tender/AwardedContracts"
                    );
                }

                // Trigger reject notifications for losing suppliers
                var losingBids = tenderAssociations.Bids.Where(b => b.Id != bidId);
                foreach (var bid in losingBids)
                {
                    if (bid.Supplier?.User != null)
                    {
                        await _notificationService.SendNotificationAsync(
                            bid.Supplier.User.Id, 
                            "Bid Rejected", 
                            $"Your bid for {tenderAssociations.ReferenceNumber} was rejected.", 
                            "Warning", 
                            "/Tender/MyBids"
                        );
                    }
                }

                TempData["SuccessMessage"] = "Tender awarded successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to award tender.";
            }

            return RedirectToAction(nameof(Details), new { id = tenderId });
        }

        [Authorize(Roles = "Retailer, Supplier")]
        public async Task<IActionResult> ReviewProposal(int id)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Tender)
                    .ThenInclude(t => t.Retailer)
                .Include(b => b.Tender)
                    .ThenInclude(t => t.TenderItems)
                        .ThenInclude(i => i.Product)
                .Include(b => b.Tender)
                    .ThenInclude(t => t.Bids)
                .Include(b => b.Supplier)
                .FirstOrDefaultAsync(b => b.Id == id);
            
            if (bid == null) return NotFound();
            
            if (User.IsInRole("Supplier"))
            {
                var suppId = await GetSupplierIdAsync();
                if (bid.SupplierId != suppId) return Unauthorized();
            }
            
            return View(bid);
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTender(int id)
        {
            var tender = await _context.Tenders
                .Include(t => t.Bids)
                    .ThenInclude(b => b.Supplier)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tender == null) return NotFound();

            tender.Status = "Cancelled";
            if (tender.Bids != null)
            {
                foreach(var bid in tender.Bids)
                {
                    bid.Status = "Rejected";
                    if (bid.Supplier?.User != null)
                    {
                        await _notificationService.SendNotificationAsync(
                            bid.Supplier.User.Id, 
                            "Tender Cancelled", 
                            $"Tender {tender.ReferenceNumber} was cancelled by the Retailer.", 
                            "Warning", 
                            "/Tender/MyBids"
                        );
                    }
                }
            }
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tender has been cancelled and all bids rejected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FulfillAward(int tenderId)
        {
            var tender = await _context.Tenders.Include(t => t.Bids).FirstOrDefaultAsync(t => t.Id == tenderId);
            if (tender == null) return NotFound();

            var winningBidIds = tender.Bids.Where(b => b.IsWinningBid).Select(b => b.Id).ToList();
            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.TenderBidId.HasValue && winningBidIds.Contains(p.TenderBidId.Value));
            
            if (po != null)
            {
                return RedirectToAction("Details", "PurchaseOrder", new { id = po.Id });
            }
            
            TempData["ErrorMessage"] = "A Purchase Order has not yet been generated for this award.";
            return RedirectToAction(nameof(AwardedContracts));
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawProposal(int id)
        {
            var bid = await _context.TenderBids.FindAsync(id);
            if (bid == null) return NotFound();
            
            var suppId = await GetSupplierIdAsync();
            if (bid.SupplierId != suppId) return Unauthorized();

            if (bid.Status == "Accepted" || bid.Status == "Rejected")
            {
                TempData["ErrorMessage"] = "Cannot withdraw a proposal that has already been awarded or rejected.";
                return RedirectToAction(nameof(ReviewProposal), new { id });
            }

            bid.Status = "Withdrawn";
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Proposal withdrawn successfully.";
            return RedirectToAction("MyBids");
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBid(int id, string reason)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Supplier)
                    .ThenInclude(s => s.User)
                .Include(b => b.Tender)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bid == null) return NotFound();
            
            bid.Status = "Rejected";
            bid.Notes = (bid.Notes ?? "") + "\nRejection Reason: " + reason;
            
            await _context.SaveChangesAsync();

            if (bid.Supplier?.User != null)
            {
                await _notificationService.SendNotificationAsync(
                    bid.Supplier.User.Id,
                    "Proposal Rejected",
                    $"Your proposal for tender {bid.Tender?.ReferenceNumber} has been rejected.",
                    "Warning",
                    "/Tender/MyBids"
                );
            }

            TempData["SuccessMessage"] = "Proposal rejected successfully.";
            return RedirectToAction(nameof(Details), new { id = bid.TenderId });
        }

        [HttpPost]
        [Authorize(Roles = "Retailer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRevision(int id, string comments)
        {
            var bid = await _context.TenderBids
                .Include(b => b.Supplier)
                    .ThenInclude(s => s.User)
                .Include(b => b.Tender)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bid == null) return NotFound();
            
            bid.Status = "RevisionRequested";
            bid.Notes = (bid.Notes ?? "") + "\nRevision Comments: " + comments;
            
            await _context.SaveChangesAsync();

            if (bid.Supplier?.User != null)
            {
                await _notificationService.SendNotificationAsync(
                    bid.Supplier.User.Id,
                    "Revision Requested",
                    $"The retailer has requested a revision for your proposal on tender {bid.Tender?.ReferenceNumber}.",
                    "Warning",
                    "/Tender/MyBids"
                );
            }

            TempData["SuccessMessage"] = "Revision request sent to supplier.";
            return RedirectToAction(nameof(ReviewProposal), new { id });
        }
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> EditProposal(int id)
        {
            var bid = await _context.TenderBids.Include(b => b.Tender).FirstOrDefaultAsync(b => b.Id == id);
            if (bid == null) return NotFound();
            
            var suppId = await GetSupplierIdAsync();
            if (bid.SupplierId != suppId) return Unauthorized();

            if (bid.Status == "Accepted" || bid.Status == "Rejected")
            {
                TempData["ErrorMessage"] = "Cannot edit a proposal that has already been awarded or rejected.";
                return RedirectToAction(nameof(ReviewProposal), new { id });
            }

            var model = new BidSubmitViewModel
            {
                TenderId = bid.TenderId,
                UnitPrice = bid.UnitPrice,
                Quantity = bid.Quantity,
                DiscountPercentage = bid.DiscountPercentage,
                VATPercentage = bid.VATPercentage,
                DeliveryLeadTimeDays = bid.DeliveryLeadTimeDays,
                ProposedDeliveryDate = bid.ProposedDeliveryDate,
                DeliveryMethod = bid.DeliveryMethod,
                DeliveryCapacity = bid.DeliveryCapacity,
                ValidityPeriodDays = bid.ValidityPeriodDays,
                TechnicalProposal = bid.TechnicalProposal,
                PackagingPlan = bid.PackagingPlan,
                InspectionCompliance = bid.InspectionCompliance,
                PenaltyAcceptance = bid.PenaltyAcceptance,
                WarrantyPeriod = bid.WarrantyPeriod,
                WarrantyType = bid.WarrantyType,
                PreviousExperience = bid.PreviousExperience,
                PaymentTerms = bid.PaymentTerms,
                ProductSpecifications = bid.ProductSpecifications,
                QualityCertifications = bid.QualityCertifications,
                InsuranceCoverage = bid.InsuranceCoverage,
                AfterSalesSupport = bid.AfterSalesSupport,
                References = bid.References,
                Notes = bid.Notes
            };

            ViewBag.Tender = bid.Tender;
            ViewBag.BidId = id;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Supplier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProposal(int id, BidSubmitViewModel model)
        {
            if (ModelState.IsValid)
            {
                var bid = await _context.TenderBids.FindAsync(id);
                if (bid == null) return NotFound();

                var suppId = await GetSupplierIdAsync();
                if (bid.SupplierId != suppId) return Unauthorized();

                // Recalculate Totals
                decimal subtotal = model.UnitPrice * model.Quantity;
                decimal discountAmount = subtotal * (model.DiscountPercentage / 100.0m);
                decimal vatableAmount = subtotal - discountAmount;
                decimal vatAmount = vatableAmount * (model.VATPercentage / 100.0m);
                decimal totalAmount = vatableAmount + vatAmount;

                bid.UnitPrice = model.UnitPrice;
                bid.Quantity = model.Quantity;
                bid.Subtotal = subtotal;
                bid.DiscountPercentage = model.DiscountPercentage;
                bid.VATPercentage = model.VATPercentage;
                bid.ProposedTotalAmount = totalAmount;
                bid.DeliveryLeadTimeDays = model.DeliveryLeadTimeDays;
                bid.ProposedDeliveryDate = model.ProposedDeliveryDate;
                bid.DeliveryMethod = model.DeliveryMethod;
                bid.DeliveryCapacity = model.DeliveryCapacity;
                bid.ValidityPeriodDays = model.ValidityPeriodDays;
                bid.TechnicalProposal = model.TechnicalProposal;
                bid.PackagingPlan = model.PackagingPlan;
                bid.InspectionCompliance = model.InspectionCompliance;
                bid.PenaltyAcceptance = model.PenaltyAcceptance;
                bid.WarrantyPeriod = model.WarrantyPeriod;
                bid.WarrantyType = model.WarrantyType;
                bid.PreviousExperience = model.PreviousExperience;
                bid.PaymentTerms = model.PaymentTerms;
                bid.ProductSpecifications = model.ProductSpecifications;
                bid.QualityCertifications = model.QualityCertifications;
                bid.InsuranceCoverage = model.InsuranceCoverage;
                bid.AfterSalesSupport = model.AfterSalesSupport;
                bid.References = model.References;
                bid.Notes = model.Notes;
                bid.Status = "Pending";
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Proposal updated successfully.";
                return RedirectToAction(nameof(ReviewProposal), new { id });
            }

            ViewBag.Tender = await _context.Tenders.FindAsync(model.TenderId);
            ViewBag.BidId = id;
            return View(model);
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
