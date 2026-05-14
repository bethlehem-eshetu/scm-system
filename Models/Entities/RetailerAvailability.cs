using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class RetailerAvailability
    {
        public int Id { get; set; }

        [Required]
        public int RetailerId { get; set; }
        public Retailer Retailer { get; set; } = null!;

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Column("StartTime")]
        public TimeSpan OpenTime { get; set; }

        [Required]
        [Column("EndTime")]
        public TimeSpan CloseTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
