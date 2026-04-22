using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class EmployeeDocument
    {
        public int Id { get; set; }

        [Required]
        public int SupplierEmployeeId { get; set; }
        public SupplierEmployee SupplierEmployee { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } // FaydaID, Contract, License, MedicalCert, etc.

        [Required]
        [StringLength(100)]
        public string DocumentName { get; set; }

        [Required]
        public string DocumentUrl { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public DateTime? IssueDate { get; set; }

        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
