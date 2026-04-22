using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class WarehouseAssignment
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public int SupplierEmployeeId { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime? EndDate { get; set; }
        public bool IsPrimary { get; set; } = true;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
    }
}
