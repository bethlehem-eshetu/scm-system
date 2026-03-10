using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class DeliveryTracking
    {
        public int Id { get; set; }

        [Required]
        public int DeliveryId { get; set; }
        public Delivery Delivery { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string StatusNote { get; set; }
    }
}