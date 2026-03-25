using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalBakeryAPI.Models;

namespace RoyalBakeryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnlineOrderSalesMapController : ControllerBase
{
    private readonly BakeryDbContext _db;
    public OnlineOrderSalesMapController(BakeryDbContext db) => _db = db;

    /// <summary>Get all mappings, optionally filter by type (BAKERY, RESTAURANT, or ALL)</summary>
    [HttpGet]
    public async Task<ActionResult<List<OnlineOrderSalesMapResponse>>> GetAll([FromQuery] string? type = "ALL")
    {
        var query = _db.OnlineOrderSalesMaps
            .Include(m => m.OnlineOrder)
            .Include(m => m.Sale)
            .Include(m => m.RestaurantSale)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type) && type.ToUpper() != "ALL")
            query = query.Where(m => m.Type == type.ToUpper());

        var result = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .Select(m => new OnlineOrderSalesMapResponse
            {
                Id = m.Id,
                OnlineOrderId = m.OnlineOrderId,
                PlatformName = m.OnlineOrder != null ? m.OnlineOrder.PlatformName : "",
                PlatformOrderId = m.OnlineOrder != null ? m.OnlineOrder.PlatformOrderId : "",
                SaleId = m.SaleId,
                SaleInvoice = m.Sale != null ? m.Sale.InvoiceNumber : null,
                RestaurantSaleId = m.RestaurantSaleId,
                RestaurantSaleInvoice = m.RestaurantSale != null ? m.RestaurantSale.InvoiceNumber : null,
                Type = m.Type,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(result);
    }

    /// <summary>Get mappings for a specific online order</summary>
    [HttpGet("by-order/{onlineOrderId}")]
    public async Task<ActionResult<List<OnlineOrderSalesMapResponse>>> GetByOrder(int onlineOrderId)
    {
        var result = await _db.OnlineOrderSalesMaps
            .Include(m => m.OnlineOrder)
            .Include(m => m.Sale)
            .Include(m => m.RestaurantSale)
            .Where(m => m.OnlineOrderId == onlineOrderId)
            .Select(m => new OnlineOrderSalesMapResponse
            {
                Id = m.Id,
                OnlineOrderId = m.OnlineOrderId,
                PlatformName = m.OnlineOrder != null ? m.OnlineOrder.PlatformName : "",
                PlatformOrderId = m.OnlineOrder != null ? m.OnlineOrder.PlatformOrderId : "",
                SaleId = m.SaleId,
                SaleInvoice = m.Sale != null ? m.Sale.InvoiceNumber : null,
                RestaurantSaleId = m.RestaurantSaleId,
                RestaurantSaleInvoice = m.RestaurantSale != null ? m.RestaurantSale.InvoiceNumber : null,
                Type = m.Type,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(result);
    }
}
