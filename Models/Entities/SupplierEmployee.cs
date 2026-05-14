using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class SupplierEmployee
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        [StringLength(50)]
        public string? EmployeeDisplayId { get; set; } // EMP-001

        [StringLength(100)]
        public string? Department { get; set; } = "Logistics";

        [NotMapped]
        public string FullName { get; set; }

        [NotMapped]
        public string Role 
        {
            get => EmployeeRole;
            set => EmployeeRole = value;
        }

        [Required]
        [StringLength(50)]
        [Display(Name = "Employee Role")]
        public string EmployeeRole { get; set; } = "Staff"; // warehouse_manager, delivery_person, sales_manager

        [StringLength(20)]
        [Display(Name = "Employee Phone")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Employee Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EmergencyContactName { get; set; }

        [StringLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlySalary { get; set; }

        [StringLength(20)]
        public string? SalaryGrade { get; set; }

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        public int? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public SupplierEmployee? Supervisor { get; set; }

        // Security & Roles
        public string? RolePermissions { get; set; } // JSON list of fine-grained permissions
        public string? DeviceAccessRestriction { get; set; } // Friendly name or ID of allowed device
        public string? AllowedLoginZones { get; set; } // JSON or CSV of GeoZones

        public bool RequireMFA { get; set; } = false;

        [StringLength(20)]
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(50)]
        public string? NationalID { get; set; }
        
        // Documents
        public string? ProfilePhotoPath { get; set; } // Renamed from PhotoUrl for enterprise consistency
        public string? ContractDocumentUrl { get; set; }
        public string? IdDocumentUrl { get; set; }
        
        public DateTime? JoinDate { get; set; }
        public SCM_System.Models.Enums.EmploymentType EmploymentType { get; set; } = SCM_System.Models.Enums.EmploymentType.FullTime;
        public SCM_System.Models.Enums.ShiftType Shift { get; set; } = SCM_System.Models.Enums.ShiftType.Day;
        public SCM_System.Models.Enums.EmployeeStatus Status { get; set; } = SCM_System.Models.Enums.EmployeeStatus.Active;

        public bool ForcePasswordChange { get; set; } = false;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Profiles
        public DriverProfile? DriverProfile { get; set; }
        public WarehouseProfile? WarehouseProfile { get; set; }

        // Assignments
        public ICollection<WarehouseAssignment> WarehouseAssignments { get; set; } = [];
        public ICollection<VehicleAssignment> VehicleAssignments { get; set; } = [];

        // History & Tracking
        public ICollection<WarehouseManagerHistory> ManagerHistories { get; set; } = [];
        public ICollection<VehicleDriverHistory> DriverHistories { get; set; } = [];
        public ICollection<EmployeeWarehouseAccess> HubAccesses { get; set; } = [];
        public ICollection<EmployeeDocument> Documents { get; set; } = [];
        public ICollection<IncidentReport> ReportedIncidents { get; set; } = [];
        public ICollection<DispatchTask> AssignedTasks { get; set; } = [];
        public ICollection<InventoryTransfer> RequestedTransfers { get; set; } = [];
        public ICollection<InventoryTransfer> ApprovedTransfers { get; set; } = [];

        // Delivery Agent Settings
        public bool IsOnDuty { get; set; } = true;
        public TimeSpan? WorkingHoursStart { get; set; }
        public TimeSpan? WorkingHoursEnd { get; set; }
        public int MaxDailyDeliveries { get; set; } = 10;
        public bool RequireProofPhoto { get; set; } = true;
        public bool RequireSignature { get; set; } = true;
        public bool AutoAcceptAssignments { get; set; } = false;
        public bool AllowNightDeliveries { get; set; } = false;
        public bool NotifyNewAssignment { get; set; } = true;
        public string? SmsNotificationNumber { get; set; }

        // Settings
        [StringLength(200)]
        [Display(Name = "Default Warehouse Location")]
        public string? DefaultWarehouseLocation { get; set; }

        [Display(Name = "Low Stock Threshold")]
        public int LowStockThreshold { get; set; } = 5;

        [StringLength(50)]
        [Display(Name = "Picklist Format")]
        public string PicklistFormat { get; set; } = "Detailed"; // Detailed, Summary, Minimal

        [Display(Name = "Auto-Accept Pick Tasks")]
        public bool AutoAcceptPickTasks { get; set; } = false;

        [Display(Name = "Notify on Low Stock")]
        public bool NotifyLowStock { get; set; } = true;

        [Display(Name = "Enable Task Alerts")]
        public bool EnableTaskAlerts { get; set; } = true;

        [Display(Name = "Enable Reminders")]
        public bool EnableReminders { get; set; } = true;

        // Advanced Warehouse Settings
        [StringLength(50)]
        [Display(Name = "Packing Priority")]
        public string DefaultPackingPriority { get; set; } = "FIFO"; // FIFO, Expiry Date, Order Value

        [Display(Name = "Daily Cut-off Time")]
        public TimeSpan? DailyCutoffTime { get; set; }

        [StringLength(50)]
        [Display(Name = "Print Label Format")]
        public string PrintLabelFormat { get; set; } = "Standard"; // Standard, QR, Barcode

        [Display(Name = "Assigned Zones")]
        public string? AssignedZones { get; set; } // JSON array of strings

        [Display(Name = "Enable Voice Picking")]
        public bool EnableVoicePicking { get; set; } = false;

        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageRating { get; set; } = 5.00m;

        // Navigation properties
        public ICollection<Delivery> Deliveries { get; set; } = [];
    }
}