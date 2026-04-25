using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Account Holder Name")]
        public string AccountHolderName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        [StringLength(100)]
        public string? Branch { get; set; }

        [StringLength(20)]
        [Display(Name = "SWIFT/BIC Code")]
        public string? SwiftCode { get; set; }

        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
