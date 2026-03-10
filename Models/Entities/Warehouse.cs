using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        public int Capacity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Inventory> Inventories { get; set; }
    }
}