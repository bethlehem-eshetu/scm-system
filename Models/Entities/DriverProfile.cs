using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class DriverProfile
    {
        public int Id { get; set; }
        public int SupplierEmployeeId { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }

        [StringLength(100)]
        public string? DrivingLicenseNumber { get; set; }

        [StringLength(50)]
        public string? LicenseType { get; set; }

        public DateTime? LicenseIssueDate { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public DateTime? MedicalFitnessExpiry { get; set; }

        [StringLength(100)]
        public string? DeliveryRegion { get; set; }

        [StringLength(100)]
        public string? CityCoverage { get; set; }

        [StringLength(500)]
        public string? CoverageArea { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
