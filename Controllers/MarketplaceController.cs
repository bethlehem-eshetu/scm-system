using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Services;
using System.Security.Claims;

namespace SCM_System.Controllers
{
    [Authorize(Roles = "Retailer")]
    public class MarketplaceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;

        public MarketplaceController(ApplicationDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        private async Task<int> GetRetailerIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var r = await _context.Retailers.FirstOrDefaultAsync(x => x.UserId == userId);
                return r?.Id ?? 0;
            }
            return 0;
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId, decimal? minPrice, decimal? maxPrice, string supplierCity)
        {
            var query = _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && !p.IsDeleted && p.Quantity > 0);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.ProductName.Contains(searchString) || p.Description.Contains(searchString));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.BasePrice <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(supplierCity))
            {
                query = query.Where(p => p.Supplier.City == supplierCity);
            }

            var products = await query.ToListAsync();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            ViewBag.Cities = await _context.Suppliers.Select(s => s.City).Distinct().ToListAsync();

            var rId = await GetRetailerIdAsync();
            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(rId);

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var rId = await GetRetailerIdAsync();
            if (rId == 0) return RedirectToAction("Login", "Account");

            await _cartService.AddToCartAsync(rId, productId, quantity);
            TempData["SuccessMessage"] = "Item added to cart.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Cart()
        {
            var rId = await GetRetailerIdAsync();
            if (rId == 0) return RedirectToAction("Login", "Account");

            var cart = await _cartService.GetCartAsync(rId);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
        {
            var rId = await GetRetailerIdAsync();
            await _cartService.UpdateCartItemQuantityAsync(rId, cartItemId, quantity);
            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var rId = await GetRetailerIdAsync();
            await _cartService.RemoveFromCartAsync(rId, cartItemId);
            TempData["SuccessMessage"] = "Item removed from cart.";
            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryAddress, DateTime expectedDeliveryDate)
        {
            var rId = await GetRetailerIdAsync();
            if (rId == 0) return RedirectToAction("Login", "Account");

            try
            {
                await _cartService.CheckoutAsync(rId, deliveryAddress, expectedDeliveryDate);
                TempData["SuccessMessage"] = "Checkout successful! Purchase Orders have been generated.";
                return RedirectToAction("Index", "PurchaseOrder");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Checkout failed: " + ex.Message;
                return RedirectToAction(nameof(Cart));
            }
        }
    }
}
