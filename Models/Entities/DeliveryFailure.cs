using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class DeliveryFailure
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string FailureReason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Reported"; // Reported, Rescheduled, CancellationRequested, Cancelled

        public int? CancellationRequestedByUserId { get; set; }
        public DateTime? CancellationRequestedAt { get; set; }

        public string? ResolutionNotes { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
