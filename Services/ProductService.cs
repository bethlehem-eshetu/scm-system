using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<IEnumerable<Product>> GetProductsBySupplierAsync(int supplierId);
        Task<IEnumerable<Product>> GetAvailableProductsAsync(string searchTerm = null, int? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null);
        Task<IEnumerable<Product>> GetSupplierProfileProductsAsync(int supplierId);
    }

    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsAvailable && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .Where(p => p.BasePrice >= minPrice && p.BasePrice <= maxPrice && p.IsAvailable && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsBySupplierAsync(int supplierId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SupplierId == supplierId && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetSupplierProfileProductsAsync(int supplierId)
        {
             return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SupplierId == supplierId && p.IsAvailable && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetAvailableProductsAsync(
            string searchTerm = null, 
            int? categoryId = null, 
            decimal? minPrice = null, 
            decimal? maxPrice = null)
        {
            var query = _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(p => 
                    p.ProductName.ToLower().Contains(lowerSearchTerm) || 
                    (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)));
            }

            if (categoryId.HasValue)
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

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }
    }
}
