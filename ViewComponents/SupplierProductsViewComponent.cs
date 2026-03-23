using Microsoft.AspNetCore.Mvc;
using SCM_System.Services;

namespace SCM_System.ViewComponents
{
    public class SupplierProductsViewComponent : ViewComponent
    {
        private readonly IProductService _productService;

        public SupplierProductsViewComponent(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int supplierId)
        {
            var products = await _productService.GetSupplierProfileProductsAsync(supplierId);
            return View(products);
        }
    }
}
