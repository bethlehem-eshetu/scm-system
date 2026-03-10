using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Inventory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int QuantityOnHand { get; set; }

        public int QuantityReserved { get; set; }

        public int QuantityAvailable { get; set; }

        [StringLength(100)]
        public string WarehouseLocation { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public int? WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }
    }
}