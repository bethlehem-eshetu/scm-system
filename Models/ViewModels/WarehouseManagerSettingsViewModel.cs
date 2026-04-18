using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.ViewModels
{
    public class WarehouseManagerSettingsViewModel
    {
        public int EmployeeId { get; set; }

        [Display(Name = "Default Warehouse Location")]
        [StringLength(200)]
        public string? DefaultWarehouseLocation { get; set; }

        [Required]
        [Display(Name = "Low Stock Threshold")]
        [Range(1, 1000, ErrorMessage = "Threshold must be between 1 and 1000")]
        public int LowStockThreshold { get; set; } = 5;

        [Required]
        [Display(Name = "Picklist Format")]
        public string PicklistFormat { get; set; } = "Detailed";

        [Display(Name = "Auto-Accept Pick Tasks")]
        public bool AutoAcceptPickTasks { get; set; }

        [Display(Name = "Notify on Low Stock")]
        public bool NotifyLowStock { get; set; } = true;
    }
}
