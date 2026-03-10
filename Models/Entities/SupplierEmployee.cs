using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class SupplierEmployee
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Employee Role")]
        public string EmployeeRole { get; set; } // warehouse_manager, delivery_person, sales_manager

        [StringLength(20)]
        [Display(Name = "Employee Phone")]
        public string Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "Employee Email")]
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Delivery> Deliveries { get; set; }
    }
}