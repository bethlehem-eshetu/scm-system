using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class VehicleAssignment
    {
        public int Id { get; set; }

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

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
