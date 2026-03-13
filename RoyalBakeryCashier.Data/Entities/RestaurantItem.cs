using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities;

[Table("RestaurantItems")]
public class RestaurantItem
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int RestaurantCategoryId { get; set; }

    [ForeignKey("RestaurantCategoryId")]
    public RestaurantCategory? Category { get; set; }
}
