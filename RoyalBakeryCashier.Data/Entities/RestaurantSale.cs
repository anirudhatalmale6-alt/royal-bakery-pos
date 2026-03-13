using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryCashier.Data.Entities;

[Table("RestaurantSales")]
public class RestaurantSale
{
    [Key]
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal ChangeGiven { get; set; }
    public string? CashierName { get; set; }

    /// <summary>
    /// Order source: "Dine In", "Pickme Food", "Ubereats"
    /// </summary>
    public string OrderSource { get; set; } = "Dine In";

    public ICollection<RestaurantSaleItem> Items { get; set; } = new List<RestaurantSaleItem>();
}
