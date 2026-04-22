using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class MaintenanceRecord
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [Required]
        public DateTime ServiceDate { get; set; }

        [Required]
        public decimal OdometerAtService { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceType { get; set; } // Routine, Repair, Inspection, Tire Change

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        [StringLength(200)]
        public string? ServiceProvider { get; set; } // Garage Name / Vendor

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime NextServiceDue { get; set; }
        public decimal NextServiceMileage { get; set; }

        public string? InvoiceDocumentUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
    }
}
