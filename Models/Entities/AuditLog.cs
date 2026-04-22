using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        // The entity being acted upon
        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty; // e.g., "SupplierEmployee", "Vehicle", "Warehouse"
        
        [Required]
        public string EntityId { get; set; } = string.Empty; // Store as string for flexibility

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty; // e.g., "Create", "Update", "Delete", "Reassign", "Restore"

        public string? OldValueJson { get; set; } // State before change
        public string? NewValueJson { get; set; } // State after change

        public int? PerformedByUserId { get; set; } // The ID of the user who performed the action
        
        [ForeignKey("PerformedByUserId")]
        public virtual User? PerformedByUser { get; set; }

        public string? Notes { get; set; }
        
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
