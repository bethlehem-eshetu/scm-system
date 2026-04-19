using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class ReturnController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReturnService _returnService;

        public ReturnController(ApplicationDbContext context, IReturnService returnService)
        {
            _context = context;
            _returnService = returnService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "";
        }

        // GET: /Return/Create/{purchaseOrderId}
        public async Task<IActionResult> Create(int purchaseOrderId)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var retailer = await _context.Retailers
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (retailer == null)
                return RedirectToAction("Login", "Account");

            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Order)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId && po.RetailerId == retailer.Id);

            if (purchaseOrder == null)
                return NotFound();

            // Check if already returned
            var existingReturn = await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.PurchaseOrderId == purchaseOrderId && r.Status != ReturnStatus.Cancelled);

            if (existingReturn != null)
            {
                TempData["ErrorMessage"] = "A return request already exists for this order.";
                return RedirectToAction("MyPurchaseOrders", "Retailer");
            }

            ViewBag.PurchaseOrder = purchaseOrder;
            return View();
        }

        // POST: /Return/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int purchaseOrderId, string reason, string description, decimal refundAmount, IFormFile? proofImage)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var retailer = await _context.Retailers
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (retailer == null)
                return RedirectToAction("Login", "Account");

            string imagePath = null;
            if (proofImage != null && proofImage.Length > 0)
            {
                var fileName = $"return_proof_{purchaseOrderId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(proofImage.FileName)}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "returns");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await proofImage.CopyToAsync(stream);
                }
                imagePath = $"/uploads/returns/{fileName}";
            }

            await _returnService.CreateReturnRequestAsync(purchaseOrderId, reason, description, refundAmount, imagePath);

            TempData["SuccessMessage"] = "Return request submitted successfully. The supplier will review it shortly.";
            return RedirectToAction("MyReturns");
        }

        // GET: /Return/MyReturns (Retailer)
        public async Task<IActionResult> MyReturns()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var retailer = await _context.Retailers
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (retailer == null)
                return RedirectToAction("Login", "Account");

            var returns = await _returnService.GetRetailerReturnsAsync(retailer.Id);
            return View(returns);
        }

        // GET: /Return/SupplierReturns (Supplier)
        public async Task<IActionResult> SupplierReturns()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (supplier == null)
                return RedirectToAction("Login", "Account");

            var returns = await _returnService.GetSupplierReturnsAsync(supplier.Id);
            return View(returns);
        }

        // GET: /Return/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var returnRequest = await _returnService.GetReturnRequestByIdAsync(id);
            if (returnRequest == null)
                return NotFound();

            string userRole = GetCurrentUserRole();
            bool isAuthorized = false;

            if (userRole == "Retailer")
            {
                var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == currentUserId);
                isAuthorized = retailer != null && returnRequest.RetailerId == retailer.Id;
            }
            else if (userRole == "Supplier")
            {
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                isAuthorized = supplier != null && returnRequest.SupplierId == supplier.Id;
            }

            if (!isAuthorized)
                return Unauthorized();

            return View(returnRequest);
        }

        // POST: /Return/Approve/{id} (Supplier)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string adminNotes)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            await _returnService.ApproveReturnAsync(id, adminNotes);
            TempData["SuccessMessage"] = "Return request approved.";
            return RedirectToAction("SupplierReturns");
        }

        // POST: /Return/Reject/{id} (Supplier)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            await _returnService.RejectReturnAsync(id, rejectionReason);
            TempData["SuccessMessage"] = "Return request rejected.";
            return RedirectToAction("SupplierReturns");
        }

        // POST: /Return/MarkAsShipped/{id} (Retailer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsShipped(int id, string trackingNumber)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            await _returnService.MarkAsShippedAsync(id, trackingNumber);
            TempData["SuccessMessage"] = "Return items marked as shipped.";
            return RedirectToAction("MyReturns");
        }

        // POST: /Return/ProcessRefund/{id} (Supplier)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            await _returnService.ProcessRefundAsync(id);
            TempData["SuccessMessage"] = "Refund processed successfully.";
            return RedirectToAction("SupplierReturns");
        }
    }
}