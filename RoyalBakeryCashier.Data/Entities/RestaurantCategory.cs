using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities;

[Table("RestaurantCategories")]
public class RestaurantCategory
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
