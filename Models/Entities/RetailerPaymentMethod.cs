using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class RetailerPaymentMethod
    {
        public int Id { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        [StringLength(50)]
        public string MethodType { get; set; } // Credit Card, Bank Transfer, PayPal, COD

        [Required]
        [StringLength(200)]
        public string Details { get; set; } // Masked card or bank account info

        public bool IsDefault { get; set; } = false;
        
        public string? Provider { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
