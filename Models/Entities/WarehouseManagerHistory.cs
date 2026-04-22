using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class WarehouseManagerHistory
    {
        public int Id { get; set; }

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        [Required]
        public int SupplierEmployeeId { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string? ChangeReason { get; set; } // Initial, Reassigned, Deactivated, Replacement, etc.

        public bool IsPrimary { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
