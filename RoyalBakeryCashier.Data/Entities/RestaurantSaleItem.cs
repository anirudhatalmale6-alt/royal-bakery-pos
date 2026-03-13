using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities;

[Table("RestaurantSaleItems")]
public class RestaurantSaleItem
{
    [Key]
    public int Id { get; set; }
    public int RestaurantSaleId { get; set; }
    public int RestaurantItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PricePerItem { get; set; }
    public decimal TotalPrice { get; set; }

    [ForeignKey("RestaurantSaleId")]
    public RestaurantSale? Sale { get; set; }

    [ForeignKey("RestaurantItemId")]
    public RestaurantItem? RestaurantItem { get; set; }
}
