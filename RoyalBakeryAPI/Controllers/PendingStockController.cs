using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalBakeryAPI.Models;

namespace RoyalBakeryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PendingStockController : ControllerBase
{
    private readonly BakeryDbContext _db;
    public PendingStockController(BakeryDbContext db) => _db = db;

    /// <summary>Get all pending stocks (optionally filter by status: ACTIVE, SETTLED, or all)</summary>
    [HttpGet]
    public async Task<ActionResult<List<PendingStockResponse>>> GetAll([FromQuery] string? status = "ACTIVE")
    {
        var query = _db.PendingStocks
            .Include(ps => ps.MenuItem)
            .Include(ps => ps.DeliveryOrder)
            .Include(ps => ps.Clearances).ThenInclude(c => c.GRN)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && status.ToUpper() != "ALL")
            query = query.Where(ps => ps.Status == status.ToUpper());

        var result = await query
            .OrderByDescending(ps => ps.CreatedAt)
            .Take(100)
            .Select(ps => new PendingStockResponse
            {
                Id = ps.Id,
                DeliveryOrderId = ps.DeliveryOrderId,
                PlatformName = ps.DeliveryOrder != null ? ps.DeliveryOrder.PlatformName : "",
                PlatformOrderId = ps.DeliveryOrder != null ? ps.DeliveryOrder.PlatformOrderId : "",
                MenuItemId = ps.MenuItemId,
                ItemName = ps.MenuItem != null ? ps.MenuItem.Name : "Unknown",
                PendingQuantity = ps.PendingQuantity,
                CurrentPendingQuantity = ps.CurrentPendingQuantity,
                Status = ps.Status,
                CreatedAt = ps.CreatedAt,
                Clearances = ps.Clearances.Select(c => new PendingStockClearanceResponse
                {
                    Id = c.Id,
                    GRNId = c.GRNId,
                    GRNNumber = c.GRN != null ? c.GRN.GRNNumber : "",
                    GRNItemId = c.GRNItemId,
                    QuantityUsed = c.QuantityUsed,
                    CreatedAt = c.CreatedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(result);
    }

    /// <summary>Get summary of active pending stocks grouped by item</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<List<PendingStockSummaryResponse>>> GetSummary()
    {
        var summary = await _db.PendingStocks
            .Where(ps => ps.Status == "ACTIVE")
            .Include(ps => ps.MenuItem)
            .GroupBy(ps => new { ps.MenuItemId, ItemName = ps.MenuItem != null ? ps.MenuItem.Name : "Unknown" })
            .Select(g => new PendingStockSummaryResponse
            {
                MenuItemId = g.Key.MenuItemId,
                ItemName = g.Key.ItemName,
                TotalPending = g.Sum(ps => ps.CurrentPendingQuantity),
                ActiveRecords = g.Count()
            })
            .OrderBy(s => s.TotalPending) // most negative first
            .ToListAsync();

        return Ok(summary);
    }

    /// <summary>Get pending stock details by ID</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PendingStockResponse>> GetById(int id)
    {
        var ps = await _db.PendingStocks
            .Include(p => p.MenuItem)
            .Include(p => p.DeliveryOrder)
            .Include(p => p.Clearances).ThenInclude(c => c.GRN)
            .Where(p => p.Id == id)
            .Select(p => new PendingStockResponse
            {
                Id = p.Id,
                DeliveryOrderId = p.DeliveryOrderId,
                PlatformName = p.DeliveryOrder != null ? p.DeliveryOrder.PlatformName : "",
                PlatformOrderId = p.DeliveryOrder != null ? p.DeliveryOrder.PlatformOrderId : "",
                MenuItemId = p.MenuItemId,
                ItemName = p.MenuItem != null ? p.MenuItem.Name : "Unknown",
                PendingQuantity = p.PendingQuantity,
                CurrentPendingQuantity = p.CurrentPendingQuantity,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                Clearances = p.Clearances.Select(c => new PendingStockClearanceResponse
                {
                    Id = c.Id,
                    GRNId = c.GRNId,
                    GRNNumber = c.GRN != null ? c.GRN.GRNNumber : "",
                    GRNItemId = c.GRNItemId,
                    QuantityUsed = c.QuantityUsed,
                    CreatedAt = c.CreatedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (ps == null)
            return NotFound(new { message = $"Pending stock {id} not found" });

        return Ok(ps);
    }
}
