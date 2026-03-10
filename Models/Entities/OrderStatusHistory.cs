using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        public string Notes { get; set; }

        [Required]
        public int ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}