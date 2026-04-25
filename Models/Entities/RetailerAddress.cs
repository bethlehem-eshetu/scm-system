using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class RetailerAddress
    {
        public int Id { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        [Required]
        [StringLength(50)]
        public string AddressType { get; set; } // Billing, Shipping

        [Required]
        [StringLength(200)]
        public string AddressLine { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string? Region { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
