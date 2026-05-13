using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class DeliveryRating
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }

        [Required]
        public int DriverEmployeeId { get; set; }
        public SupplierEmployee DriverEmployee { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; }

        // Rating fields (1-5)
        [Range(1, 5)]
        public int Timeliness { get; set; }

        [Range(1, 5)]
        public int Professionalism { get; set; }

        [Range(1, 5)]
        public int VehicleCondition { get; set; }

        [Range(1, 5)]
        public int Communication { get; set; }

        // Overall rating (calculated average)
        [NotMapped]
        public double OverallRating => (Timeliness + Professionalism + VehicleCondition + Communication) / 4.0;

        [StringLength(500)]
        public string? Comment { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
