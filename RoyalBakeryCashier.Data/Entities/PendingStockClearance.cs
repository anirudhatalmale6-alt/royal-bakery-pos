using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities
{
    /// <summary>
    /// Records how GRNs settle pending stock shortages.
    /// Many-to-many: one GRN can settle many pending stocks, one pending stock can be settled by multiple GRNs.
    /// </summary>
    [Table("PendingStockClearances")]
    public class PendingStockClearance
    {
        [Key]
        public int Id { get; set; }

        public int PendingStockId { get; set; }

        [ForeignKey("PendingStockId")]
        public PendingStock? PendingStock { get; set; }

        public int GRNId { get; set; }

        [ForeignKey("GRNId")]
        public GRN? GRN { get; set; }

        public int GRNItemId { get; set; }

        [ForeignKey("GRNItemId")]
        public GRNItem? GRNItem { get; set; }

        public int MenuItemId { get; set; }

        [ForeignKey("MenuItemId")]
        public MenuItem? MenuItem { get; set; }

        /// <summary>Quantity from this GRN used to settle the pending stock</summary>
        public int QuantityUsed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
