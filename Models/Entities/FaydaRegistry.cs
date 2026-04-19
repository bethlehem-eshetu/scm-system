using System;
using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class FaydaRegistry
    {
        public int Id { get; set; }

        [Required]
        [StringLength(16)]
        public string FAN { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        [StringLength(20)]
        public string Gender { get; set; } = string.Empty;

        [StringLength(50)]
        public string Region { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
