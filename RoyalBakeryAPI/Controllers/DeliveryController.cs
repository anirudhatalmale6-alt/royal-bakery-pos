using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalBakeryAPI.Models;

namespace RoyalBakeryAPI.Controllers;

[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly BakeryDbContext _db;

    public DeliveryController(BakeryDbContext db) => _db = db;

    /// <summary>
    /// Get delivery orders pending KOT printing.
    /// Called by Restaurant POS every 5 seconds.
    /// </summary>
    [HttpGet("pending-kot")]
    public async Task<IActionResult> GetPendingKot()
    {
        var orders = await _db.DeliveryOrders
            .Where(d => d.KotStatus == 0) // PendingKOT
            .Include(d => d.Items)
            .OrderBy(d => d.ReceivedAt)
            .ToListAsync();

        var result = orders.Select(d => new
        {
            d.Id,
            d.PlatformName,
            d.PlatformOrderId,
            d.AccountName,
            d.CustomerPhone,
            d.CustomerAddress,
            d.DeliveryMode,
            d.DeliveryNote,
            d.OrderTotal,
            d.PaymentMethod,
            d.ReceivedAt,
            Items = d.Items.Select(i => new
            {
                i.ItemName,
                i.Quantity,
                i.PricePerItem,
                i.TotalPrice,
                i.SpecialInstructions,
                i.Options,
                i.ItemType
            })
        });

        return Ok(result);
    }

    /// <summary>
    /// Mark a delivery order's KOT as printed.
    /// Called by Restaurant POS after printing.
    /// </summary>
    [HttpPost("{id}/kot-done")]
    public async Task<IActionResult> MarkKotDone(int id)
    {
        var order = await _db.DeliveryOrders.FindAsync(id);
        if (order == null) return NotFound();

        order.KotStatus = 1; // KOTPrinted
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    /// <summary>
    /// Get delivery orders with filters for Admin Panel.
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? platform = null,
        [FromQuery] string? date = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.DeliveryOrders
            .Include(d => d.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(d => d.PlatformName == platform);

        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var filterDate))
        {
            var nextDay = filterDate.AddDays(1);
            query = query.Where(d => d.ReceivedAt >= filterDate && d.ReceivedAt < nextDay);
        }

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(d => d.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = orders.Select(d => new
        {
            d.Id,
            d.PlatformName,
            d.PlatformOrderId,
            d.AccountName,
            d.PlatformStatus,
            d.OrderTotal,
            d.PaymentMethod,
            d.DeliveryMode,
            d.CustomerPhone,
            d.CustomerAddress,
            d.DeliveryNote,
            d.RestaurantSaleId,
            d.BakerySaleId,
            d.KotStatus,
            d.ReceivedAt,
            d.CompletedAt,
            ItemCount = d.Items.Count,
            Items = d.Items.Select(i => new
            {
                i.ItemName,
                i.Quantity,
                i.PricePerItem,
                i.TotalPrice,
                i.ItemType,
                i.PlatformRefId,
                i.SpecialInstructions,
                i.Options
            })
        });

        return Ok(new { total, page, pageSize, data = result });
    }

    /// <summary>
    /// Get single delivery order detail.
    /// </summary>
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _db.DeliveryOrders
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (order == null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.PlatformName,
            order.PlatformOrderId,
            order.AccountName,
            order.PlatformStatus,
            order.OrderTotal,
            order.PaymentMethod,
            order.DeliveryMode,
            order.CustomerPhone,
            order.CustomerAddress,
            order.DeliveryNote,
            order.RestaurantSaleId,
            order.BakerySaleId,
            order.KotStatus,
            order.ReceivedAt,
            order.CompletedAt,
            Items = order.Items.Select(i => new
            {
                i.ItemName,
                i.Quantity,
                i.PricePerItem,
                i.TotalPrice,
                i.ItemType,
                i.PlatformRefId,
                i.SpecialInstructions,
                i.Options
            })
        });
    }
}
