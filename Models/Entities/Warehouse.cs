using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(50)]
        public string? WarehouseCode { get; set; }

        public SCM_System.Models.Enums.HubType HubType { get; set; } = SCM_System.Models.Enums.HubType.Warehouse;

        // Location
        [StringLength(100)]
        public string Country { get; set; } = "Ethiopia";
        [StringLength(100)]
        public string Region { get; set; }
        [StringLength(100)]
        public string City { get; set; }
        [StringLength(100)]
        public string? SubCityZone { get; set; }
        [StringLength(300)]
        public string Address { get; set; }
        [StringLength(200)]
        public string? Landmark { get; set; }
        [Column(TypeName = "decimal(10, 8)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(11, 8)")]
        public decimal? Longitude { get; set; }

        // Primary Manager Profile
        public int? PrimaryManagerId { get; set; }
        [ForeignKey("PrimaryManagerId")]
        public SupplierEmployee? PrimaryManager { get; set; }

        // Operations
        public TimeSpan? OperatingHoursFrom { get; set; }
        public TimeSpan? OperatingHoursTo { get; set; }
        [StringLength(100)]
        public string? WorkingDays { get; set; }
        public int AvgProcessingTimeHours { get; set; }
        
        [StringLength(50)]
        public string? Timezone { get; set; }
        [StringLength(100)]
        public string? WeekendDays { get; set; }
        public SCM_System.Models.Enums.WarehouseStatus Status { get; set; } = SCM_System.Models.Enums.WarehouseStatus.Active;
        public bool IsDefault { get; set; }
        public bool SupportsDelivery { get; set; } = true;
        
        // Logistics Infrastructure
        public decimal? ReceivingAreaSizeM2 { get; set; }
        public int PackingStationsCount { get; set; } = 1;
        public bool HasInternet { get; set; } = true;
        public bool HasBackupPower { get; set; } = false;

        [StringLength(500)]
        public string? CoverageRegions { get; set; } // JSON array of cities/regions served
        public int MaxDeliveryDistanceKM { get; set; } = 100;
        public int CurrentWorkload { get; set; } = 0;

        // Storage
        public SCM_System.Models.Enums.StorageArchitecture StorageArchitecture { get; set; } = SCM_System.Models.Enums.StorageArchitecture.General;
        public int MaxCapacity { get; set; }
        public int? CapacityUsed { get; set; }
        public int ReservedSpace { get; set; } = 0;
        public int OverflowWarningThreshold { get; set; } = 90; // Percentage
        public string? TemperatureZoneTypes { get; set; } // JSON list
        public bool HazardStorageAllowed { get; set; } = false;
        
        public SCM_System.Models.Enums.OccupancyStatus OccupancyStatus { get; set; } = SCM_System.Models.Enums.OccupancyStatus.Normal;

        [NotMapped]
        public int CurrentUtilizationPercent => MaxCapacity > 0 ? ((CapacityUsed ?? 0) * 100) / MaxCapacity : 0;
        
        [NotMapped]
        public int AvailableCapacity => MaxCapacity - (CapacityUsed ?? 0);

        public DateTime? LastInventoryCount { get; set; }

        // Infrastructure
        public int? LoadingBays { get; set; }
        public int? ForkliftsAvailable { get; set; }
        public bool CCTVEnabled { get; set; }
        public bool FireSafetyInstalled { get; set; }
        public string? PhotoPath { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public ICollection<WarehouseAssignment> Assignments { get; set; } = new List<WarehouseAssignment>();

        // Navigation properties
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public ICollection<SupplierEmployee> Employees { get; set; } = new List<SupplierEmployee>();

        // History & Tracking
        public ICollection<WarehouseManagerHistory> ManagerHistories { get; set; } = new List<WarehouseManagerHistory>();
        public ICollection<EmployeeWarehouseAccess> StaffAccesses { get; set; } = new List<EmployeeWarehouseAccess>();
        public ICollection<DispatchTask> HubDispatches { get; set; } = new List<DispatchTask>();
        public ICollection<InventoryTransfer> OutgoingTransfers { get; set; } = new List<InventoryTransfer>();
        public ICollection<InventoryTransfer> IncomingTransfers { get; set; } = new List<InventoryTransfer>();
        public ICollection<IncidentReport> HubIncidents { get; set; } = new List<IncidentReport>();
    }
}