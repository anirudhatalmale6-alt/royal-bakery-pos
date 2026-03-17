using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities
{
    /// <summary>
    /// Tracks orders received from delivery platforms (PickMe, UberEats).
    /// Links to RestaurantSale or Sale depending on item type.
    /// </summary>
    [Table("DeliveryOrders")]
    public class DeliveryOrder
    {
        [Key]
        public int Id { get; set; }

        /// <summary>"PickMe" or "UberEats"</summary>
        public string PlatformName { get; set; } = string.Empty;

        /// <summary>Platform's order ID (pickme_job_id or uber order_id)</summary>
        public string PlatformOrderId { get; set; } = string.Empty;

        /// <summary>Which API key/account this came from</summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>FK to RestaurantSales.Id (null if bakery-only order)</summary>
        public int? RestaurantSaleId { get; set; }

        /// <summary>FK to Sales.Id (null if restaurant-only order)</summary>
        public int? BakerySaleId { get; set; }

        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }

        /// <summary>"Delivery" or "Pickup"</summary>
        public string DeliveryMode { get; set; } = "Delivery";

        /// <summary>Current status on the platform</summary>
        public string PlatformStatus { get; set; } = string.Empty;

        public decimal OrderTotal { get; set; }
        public string? PaymentMethod { get; set; }
        public string? DeliveryNote { get; set; }

        public DateTime ReceivedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        /// <summary>0=PendingKOT, 1=KOTPrinted, 2=Completed</summary>
        public int KotStatus { get; set; } = 0;

        /// <summary>Full JSON of the platform order for debugging</summary>
        public string? RawOrderJson { get; set; }

        public ICollection<DeliveryOrderItem> Items { get; set; } = new List<DeliveryOrderItem>();
    }
}
