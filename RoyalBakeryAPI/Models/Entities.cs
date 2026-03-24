using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalBakeryAPI.Models;

[Table("MenuCategories")]
public class MenuCategory
{
    [Key]
    public int Id { get; set; }
    [Required][MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

[Table("MenuItems")]
public class MenuItem
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public int MenuCategoryId { get; set; }
    public bool IsQuick { get; set; } = false;
}

[Table("Stocks")]
public class Stock
{
    [Key]
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public int Quantity { get; set; }
}

[Table("GRNs")]
public class GRN
{
    [Key]
    public int Id { get; set; }
    public string GRNNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<GRNItem> Items { get; set; } = new();
}

[Table("GRNItems")]
public class GRNItem
{
    [Key]
    public int Id { get; set; }
    public int GRNId { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public GRN? GRN { get; set; }
    [Required]
    public int Quantity { get; set; }
    [Required][Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    [Required]
    public int CurrentQuantity { get; set; }
}

[Table("GRNAdjustmentRequests")]
public class GRNAdjustmentRequest
{
    [Key]
    public int Id { get; set; }
    public int GRNId { get; set; }
    public GRN? GRN { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdminCode { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = false;
    public DateTime RequestedAt { get; set; } = DateTime.Now;
    public ICollection<GRNAdjustmentRequestItem> RequestedItems { get; set; } = new List<GRNAdjustmentRequestItem>();
}

[Table("GRNAdjustmentRequestItems")]
public class GRNAdjustmentRequestItem
{
    [Key]
    public int Id { get; set; }
    public int GRNAdjustmentRequestId { get; set; }
    public GRNAdjustmentRequest? GRNAdjustmentRequest { get; set; }
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}

[Table("Clearances")]
public class Clearance
{
    [Key]
    public int Id { get; set; }
    [Required]
    public DateTime DateTime { get; set; } = DateTime.Now;
    [Required]
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    [Required]
    public int Quantity { get; set; }
    [Required]
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
}

[Table("GRNEditLogs")]
public class GRNEditLog
{
    [Key]
    public int Id { get; set; }
    public int GRNId { get; set; }
    public GRN? GRN { get; set; }
    [Required]
    public string Reason { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
    public DateTime EditedAt { get; set; } = DateTime.Now;
}

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }
    [Required][MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    [Required][MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;
    [Required][MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;
    [Required][MaxLength(30)]
    public string Role { get; set; } = "Cashier";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// ===== Delivery Platform Integration =====

[Table("DeliveryOrders")]
public class DeliveryOrder
{
    [Key]
    public int Id { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string PlatformOrderId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? RestaurantSaleId { get; set; }
    public int? BakerySaleId { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string DeliveryMode { get; set; } = "Delivery";
    public string PlatformStatus { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal OrderTotal { get; set; }
    public string? PaymentMethod { get; set; }
    public string? DeliveryNote { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    /// <summary>0=PendingKOT, 1=KOTPrinted, 2=Completed</summary>
    public int KotStatus { get; set; } = 0;
    public string? RawOrderJson { get; set; }
    public List<DeliveryOrderItem> Items { get; set; } = new();
}

[Table("DeliveryOrderItems")]
public class DeliveryOrderItem
{
    [Key]
    public int Id { get; set; }
    public int DeliveryOrderId { get; set; }
    public int PlatformItemId { get; set; }
    public string? PlatformRefId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerItem { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? Options { get; set; }
    /// <summary>"B" for bakery, "R" for restaurant, "U" for unmapped</summary>
    public string ItemType { get; set; } = "U";
    public int? LocalItemId { get; set; }
    [ForeignKey("DeliveryOrderId")]
    public DeliveryOrder? DeliveryOrder { get; set; }
}

// Restaurant entities needed by API for delivery order processing
[Table("RestaurantItems")]
public class RestaurantItem
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public int RestaurantCategoryId { get; set; }
}

[Table("RestaurantSales")]
public class RestaurantSale
{
    [Key]
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CashAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CardAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal ChangeGiven { get; set; }
    public string? CashierName { get; set; }
    public string OrderSource { get; set; } = "Dine In";
    public List<RestaurantSaleItem> Items { get; set; } = new();
}

[Table("RestaurantSaleItems")]
public class RestaurantSaleItem
{
    [Key]
    public int Id { get; set; }
    public int RestaurantSaleId { get; set; }
    public int RestaurantItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerItem { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    [ForeignKey("RestaurantSaleId")]
    public RestaurantSale? Sale { get; set; }
}

// ===== Pending Stock (Online order shortages) =====

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
    /// <summary>Original shortage (negative value, e.g. -10)</summary>
    public int PendingQuantity { get; set; }
    /// <summary>Remaining shortage after GRN settlements</summary>
    public int CurrentPendingQuantity { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<PendingStockClearance> Clearances { get; set; } = new();
}

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
    public int QuantityUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// Bakery Sales entity for delivery stock deduction
[Table("Sales")]
public class Sale
{
    [Key]
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CashAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CardAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal ChangeGiven { get; set; }
    public string? CashierName { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public List<SaleItem> Items { get; set; } = new();
}

[Table("SaleItems")]
public class SaleItem
{
    [Key]
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerItem { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
    [ForeignKey("SaleId")]
    public Sale? Sale { get; set; }
}
