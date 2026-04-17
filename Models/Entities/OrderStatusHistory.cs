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
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Comments { get; set; }

        public int? ChangedByUserId { get; set; }
        public User? ChangedByUser { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}