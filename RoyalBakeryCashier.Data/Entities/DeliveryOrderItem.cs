using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities
{
    /// <summary>
    /// Line item in a delivery order. Maps platform item to local item via ref_id.
    /// Ref_id format: "B-{MenuItemId}" for bakery, "R-{RestaurantItemId}" for restaurant.
    /// </summary>
    [Table("DeliveryOrderItems")]
    public class DeliveryOrderItem
    {
        [Key]
        public int Id { get; set; }

        public int DeliveryOrderId { get; set; }

        /// <summary>Platform's item ID</summary>
        public int PlatformItemId { get; set; }

        /// <summary>Merchant reference ID from platform (e.g. "B-123" or "R-45")</summary>
        public string? PlatformRefId { get; set; }

        /// <summary>Item name as shown on the platform</summary>
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        /// <summary>Price per item on the platform (may differ from in-store)</summary>
        public decimal PricePerItem { get; set; }

        public decimal TotalPrice { get; set; }

        /// <summary>Special instructions from customer</summary>
        public string? SpecialInstructions { get; set; }

        /// <summary>Options/toppings chosen, stored as comma-separated string</summary>
        public string? Options { get; set; }

        /// <summary>"B" for bakery (MenuItem), "R" for restaurant (RestaurantItem), "U" for unmapped</summary>
        public string ItemType { get; set; } = "U";

        /// <summary>Local MenuItemId (if bakery) or RestaurantItemId (if restaurant)</summary>
        public int? LocalItemId { get; set; }

        [ForeignKey("DeliveryOrderId")]
        public DeliveryOrder? DeliveryOrder { get; set; }
    }
}
