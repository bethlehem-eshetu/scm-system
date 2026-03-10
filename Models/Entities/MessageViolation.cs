using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class MessageViolation
    {
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }
        public Message Message { get; set; }

        [Required]
        [StringLength(20)]
        public string ViolationType { get; set; } // Phone, Email, Social

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsResolved { get; set; } = false;
    }
}