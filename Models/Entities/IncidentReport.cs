using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class IncidentReport
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public int ReportedById { get; set; }
        [ForeignKey("ReportedById")]
        public SupplierEmployee ReportedBy { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public int? DispatchTaskId { get; set; }
        public DispatchTask? DispatchTask { get; set; }

        [Required]
        public SCM_System.Models.Enums.IncidentType Type { get; set; }

        [Required]
        public SCM_System.Models.Enums.IncidentSeverity Severity { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        public string? PhotoUrl { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Long { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Reported"; // Reported, Investigating, Resolved, Closed

        public string? ResolutionNotes { get; set; }

        public DateTime ObservedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
