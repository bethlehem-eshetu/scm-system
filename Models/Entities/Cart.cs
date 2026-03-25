using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
