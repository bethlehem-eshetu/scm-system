using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class EmailTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
