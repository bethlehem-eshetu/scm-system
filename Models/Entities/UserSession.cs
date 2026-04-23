using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionToken { get; set; } = string.Empty;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public DateTime LastActivityTime { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
