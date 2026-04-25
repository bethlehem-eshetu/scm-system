using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class SystemConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string DataType { get; set; } = "string"; // string, int, decimal, bool, json
    }
}
