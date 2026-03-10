using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Conversation
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastMessageAt { get; set; }

        // Navigation properties
        public ICollection<Message> Messages { get; set; }
    }
}