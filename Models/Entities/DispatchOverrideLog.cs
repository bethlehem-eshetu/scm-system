using System.ComponentModel.DataAnnotations;

namespace SCM_System.Models.Entities
{
    public class DispatchOverrideLog
    {
        public int Id { get; set; }
        
        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        
        [Required]
        public int AgentId { get; set; }
        public SupplierEmployee Agent { get; set; } = null!;
        
        [Required]
        public int PerformedByUserId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [Required]
        public int CurrentLoad { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
