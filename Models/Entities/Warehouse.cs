using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(50)]
        public string? WarehouseCode { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = "Ethiopia";

        [Required(ErrorMessage = "Region is required")]
        [StringLength(100)]
        public string Region { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(300)]
        public string Address { get; set; }

        public SCM_System.Models.Enums.StorageType StorageType { get; set; } = SCM_System.Models.Enums.StorageType.General;

        public int MaxCapacity { get; set; }

        public SCM_System.Models.Enums.WarehouseStatus Status { get; set; } = SCM_System.Models.Enums.WarehouseStatus.Active;

        public bool IsDefault { get; set; }

        public bool SupportsDelivery { get; set; } = true;

        public int HandlingTimeHours { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Inventory> Inventories { get; set; }
        public ICollection<SupplierEmployee> Employees { get; set; }
    }
}