using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class RetailerCategory
    {
        public int Id { get; set; }

        public int RetailerId { get; set; }
        
        [ForeignKey("RetailerId")]
        public virtual Retailer Retailer { get; set; }

        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual ProductCategory Category { get; set; }

        public DateTime AssociatedAt { get; set; } = DateTime.Now;
    }
}
