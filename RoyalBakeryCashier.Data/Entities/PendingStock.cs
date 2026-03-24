using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities
{
    /// <summary>
    /// Tracks stock shortages from online platform orders (PickMe/UberEats).
    /// Created when an order is accepted but stock is insufficient.
    /// Settled when GRNs arrive via PendingStockClearance records.
    /// </summary>
    [Table("PendingStocks")]
    public class PendingStock
    {
        [Key]
        public int Id { get; set; }

        public int DeliveryOrderId { get; set; }

        [ForeignKey("DeliveryOrderId")]
        public DeliveryOrder? DeliveryOrder { get; set; }

        public int MenuItemId { get; set; }

        [ForeignKey("MenuItemId")]
        public MenuItem? MenuItem { get; set; }

        /// <summary>Original shortage quantity (negative value, e.g. -10)</summary>
        public int PendingQuantity { get; set; }

        /// <summary>Remaining shortage after GRN settlements (starts equal to PendingQuantity, moves toward 0)</summary>
        public int CurrentPendingQuantity { get; set; }

        /// <summary>ACTIVE or SETTLED</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PendingStockClearance> Clearances { get; set; } = new List<PendingStockClearance>();
    }
}
