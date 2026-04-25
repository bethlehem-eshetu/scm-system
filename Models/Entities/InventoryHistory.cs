using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class InventoryHistory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [Required]
        public int WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; }

        [Required]
        public int SupplierEmployeeId { get; set; }
        public virtual SupplierEmployee PerformedBy { get; set; }

        public int Quantity { get; set; }

        [StringLength(50)]
        public string? BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string? Notes { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
