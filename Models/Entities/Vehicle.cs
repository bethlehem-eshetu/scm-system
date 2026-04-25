using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        [Required(ErrorMessage = "License Plate is required")]
        [StringLength(50)]
        public required string LicensePlate { get; set; } = string.Empty;

        [StringLength(50)]
        public string? AssetCode { get; set; }

        public SCM_System.Models.Enums.VehicleType VehicleType { get; set; } = SCM_System.Models.Enums.VehicleType.Truck;

        [StringLength(100)]
        public string? Make { get; set; }
        [StringLength(100)]
        public string? Brand { get; set; }
        [StringLength(100)]
        public string? Model { get; set; }
        public int? ManufactureYear { get; set; }
        [StringLength(50)]
        public string? Color { get; set; }

        // Tech Specs
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Max Load Capacity must be greater than 0")]
        public decimal MaxLoadCapacity { get; set; }
        public decimal? InternalVolumeM3 { get; set; }
        [StringLength(20)]
        public string? FuelType { get; set; }
        public decimal? FuelTankCapacity { get; set; }
        public bool TemperatureControlled { get; set; }

        public int? WarehouseId { get; set; }
        [ForeignKey("WarehouseId")]
        public Warehouse? Warehouse { get; set; }

        // Operations
        public SCM_System.Models.Enums.VehicleStatus Status { get; set; } = SCM_System.Models.Enums.VehicleStatus.Available;
        public decimal? Mileage { get; set; }
        public decimal? CurrentMileage { get; set; }
        public decimal? FuelEfficiency { get; set; }

        // Maintenance
        public DateTime? LastServiceDate { get; set; }
        public DateTime? NextServiceDueDate { get; set; }
        public DateTime? TireChangeDue { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
        public DateTime? RegistrationExpiryDate { get; set; }
        
        // Documents & Compliance
        public string? PhotoPath { get; set; }
        public string? RegistrationCertificateUrl { get; set; }
        public string? InsuranceCertificateUrl { get; set; }
        public string? VehiclePhotosUrls { get; set; }

        [StringLength(100)]
        public string? InsuranceProvider { get; set; }
        [StringLength(50)]
        public string? FuelCardNumber { get; set; }

        public decimal? TireChangeDueMileage { get; set; }
        public int ServiceIntervalMonths { get; set; } = 6;
        public string? AccidentHistoryNote { get; set; }
        public string? DriverEligibilityType { get; set; } // Class A, B, Heavy, etc.

        // Finance
        public DateTime? PurchaseDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PurchaseCost { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CurrentEstimatedValue { get; set; }

        // Tracking
        public bool GPSInstalled { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public ICollection<VehicleAssignment> Assignments { get; set; } = [];

        public int? PrimaryDriverId { get; set; }
        [ForeignKey("PrimaryDriverId")]
        public SupplierEmployee? PrimaryDriver { get; set; }

        // Navigation
        public ICollection<SupplierEmployee> DeliveryAgents { get; set; } = [];

        // History & Tracking
        public ICollection<VehicleDriverHistory> DriverHistories { get; set; } = [];
        public ICollection<VehicleDocument> Documents { get; set; } = [];
        public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = [];
        public ICollection<DispatchTask> AssetDispatches { get; set; } = [];
        public ICollection<IncidentReport> AssetIncidents { get; set; } = [];
        public ICollection<GPSLog> GPSLogs { get; set; } = [];
    }
}
