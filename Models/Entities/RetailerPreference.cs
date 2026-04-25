using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class RetailerPreference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        // Notification Toggles
        public bool NewTenderMatchAlert { get; set; } = true;
        public bool BidAcceptedAlert { get; set; } = true;
        public bool OrderShippedAlert { get; set; } = true;
        public bool OrderDeliveredAlert { get; set; } = true;
        public bool LowStockAlert { get; set; } = true;
        public bool PriceDropAlert { get; set; } = true;

        // Display Preferences
        public string? Theme { get; set; } = "Light";
        public string? Language { get; set; } = "English";

        // Security
        public bool TwoFactorEnabled { get; set; } = false;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
