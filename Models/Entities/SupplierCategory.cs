using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class SupplierCategory
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }
        
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual ProductCategory Category { get; set; }

        public DateTime AssociatedAt { get; set; } = DateTime.Now;
    }
}
