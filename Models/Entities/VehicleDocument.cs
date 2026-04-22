using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class VehicleDocument
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } // InsuranceCode, RegistrationCard, RoadWorthinessCert, etc.

        [Required]
        [StringLength(100)]
        public string DocumentName { get; set; }

        [Required]
        public string DocumentUrl { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public DateTime? IssueDate { get; set; }

        [StringLength(100)]
        public string? IssuingAuthority { get; set; }

        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
