using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class DeadLetterWebhook
    {
        [Key]
        public int Id { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string Payload { get; set; }
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        public int RetryCount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
