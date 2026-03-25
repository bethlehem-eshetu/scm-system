using SCM_System.Models.Entities;

namespace SCM_System.Services
{
    public interface ICartService
    {
        Task<Cart> GetCartAsync(int retailerId);
        Task<int> GetCartItemCountAsync(int retailerId);
        Task AddToCartAsync(int retailerId, int productId, int quantity);
        Task UpdateCartItemQuantityAsync(int retailerId, int cartItemId, int quantity);
        Task RemoveFromCartAsync(int retailerId, int cartItemId);
        Task ClearCartAsync(int retailerId);
        Task CheckoutAsync(int retailerId, string deliveryAddress, DateTime expectedDeliveryDate);
    }
}
