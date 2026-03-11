using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; }

        public string SettingValue { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
