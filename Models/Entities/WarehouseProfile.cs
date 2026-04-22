using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class WarehouseProfile
    {
        public int Id { get; set; }
        public int SupplierEmployeeId { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }

        public bool CanApproveTransfers { get; set; } = true;
        public bool CanManageInventory { get; set; } = true;
        public bool CanViewReports { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
