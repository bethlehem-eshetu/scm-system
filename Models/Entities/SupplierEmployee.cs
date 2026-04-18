using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class SupplierEmployee
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int? WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public int? VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [StringLength(100)]
        public string? DrivingLicenseNumber { get; set; }

        public DateTime? LicenseExpiryDate { get; set; }

        public bool IsLicenseVerified { get; set; } = false;

        [Required]
        [StringLength(50)]
        [Display(Name = "Employee Role")]
        public string EmployeeRole { get; set; } // warehouse_manager, delivery_person, sales_manager

        [StringLength(20)]
        [Display(Name = "Employee Phone")]
        public string Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "Employee Email")]
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

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

        // Navigation properties
        public ICollection<Delivery> Deliveries { get; set; }
    }
}