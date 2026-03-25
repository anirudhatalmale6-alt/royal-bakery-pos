using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities
{
    /// <summary>
    /// Maps online platform orders to their corresponding sales records.
    /// One online order can have multiple mappings (bakery + restaurant).
    /// Acts as a central relationship bridge between DeliveryOrders and Sales/RestaurantSales.
    /// </summary>
    [Table("OnlineOrderSalesMap")]
    public class OnlineOrderSalesMap
    {
        [Key]
        public int Id { get; set; }

        public int OnlineOrderId { get; set; }

        [ForeignKey("OnlineOrderId")]
        public DeliveryOrder? OnlineOrder { get; set; }

        public int? SaleId { get; set; }

        [ForeignKey("SaleId")]
        public Sale? Sale { get; set; }

        public int? RestaurantSaleId { get; set; }

        [ForeignKey("RestaurantSaleId")]
        public RestaurantSale? RestaurantSale { get; set; }

        /// <summary>BAKERY or RESTAURANT</summary>
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
