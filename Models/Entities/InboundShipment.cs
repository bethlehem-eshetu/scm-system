using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SCM_System.Models.Entities
{
    public class InboundShipment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ShipmentNumber { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Expected"; // Expected, PartiallyReceived, Received, Damaged, Cancelled

        public DateTime? ExpectedArrival { get; set; }
        public DateTime? ReceivedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<InboundShipmentItem> Items { get; set; }
    }

    public class InboundShipmentItem
    {
        public int Id { get; set; }

        [Required]
        public int InboundShipmentId { get; set; }
        public InboundShipment InboundShipment { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int ExpectedQuantity { get; set; }

        public int ReceivedQuantity { get; set; } = 0;
        public int DamagedQuantity { get; set; } = 0;

        [StringLength(200)]
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
