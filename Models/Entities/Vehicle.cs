using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required(ErrorMessage = "License Plate is required")]
        [StringLength(50)]
        public string LicensePlate { get; set; }

        public SCM_System.Models.Enums.VehicleType VehicleType { get; set; } = SCM_System.Models.Enums.VehicleType.Truck;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Max Load Capacity must be greater than 0")]
        public decimal MaxLoadCapacity { get; set; }

        public decimal? VolumeCapacity { get; set; }

        public bool HasTemperatureControl { get; set; }

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? InsuranceStatus { get; set; } // Active, Expired

        public DateTime? InsuranceExpiryDate { get; set; }

        [StringLength(100)]
        public string? RoadworthinessStatus { get; set; }

        public DateTime? LastMaintenanceDate { get; set; }

        public SCM_System.Models.Enums.VehicleStatus Status { get; set; } = SCM_System.Models.Enums.VehicleStatus.Available;

        public DateTime? UpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<SupplierEmployee> DeliveryAgents { get; set; }
    }
}
