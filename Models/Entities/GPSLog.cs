using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class GPSLog
    {
        public long Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [Column(TypeName = "decimal(10, 8)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(11, 8)")]
        public decimal Longitude { get; set; }

        public decimal? SpeedKph { get; set; }

        [StringLength(100)]
        public string? NearestAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
