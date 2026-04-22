using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class EmployeeWarehouseAccess
    {
        public int Id { get; set; }

        [Required]
        public int SupplierEmployeeId { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        [Required]
        [StringLength(50)]
        public string PermissionLevel { get; set; } // PrimaryManager, Supervisor, Picker, Packer, ViewOnly

        public bool CanApproveDispatch { get; set; } = false;
        public bool CanManageStock { get; set; } = false;

        public DateTime GrantedAt { get; set; } = DateTime.Now;
        public string? GrantedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
