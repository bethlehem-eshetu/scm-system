using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class VehicleDriverHistory
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [Required]
        public int SupplierEmployeeId { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string? ChangeReason { get; set; } // Hub Transfer, Maintenance Break, Driver Rotation, etc.
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
