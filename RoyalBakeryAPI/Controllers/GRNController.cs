using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalBakeryAPI.Models;

namespace RoyalBakeryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GRNController : ControllerBase
{
    private readonly BakeryDbContext _db;
    public GRNController(BakeryDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<GRNResponse>>> GetAll()
    {
        var grns = await _db.GRNs
            .Include(g => g.Items).ThenInclude(i => i.MenuItem)
            .OrderByDescending(g => g.CreatedAt)
            .Take(50)
            .Select(g => new GRNResponse
            {
                Id = g.Id,
                GRNNumber = g.GRNNumber,
                CreatedAt = g.CreatedAt,
                Items = g.Items.Select(i => new GRNItemResponse
                {
                    Id = i.Id,
                    MenuItemId = i.MenuItemId,
                    ItemName = i.MenuItem != null ? i.MenuItem.Name : "Unknown",
                    Quantity = i.Quantity,
                    Price = i.Price,
                    CurrentQuantity = i.CurrentQuantity
                }).ToList()
            })
            .ToListAsync();
        return Ok(grns);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GRNResponse>> GetById(int id)
    {
        var grn = await _db.GRNs
            .Include(g => g.Items).ThenInclude(i => i.MenuItem)
            .Where(g => g.Id == id)
            .Select(g => new GRNResponse
            {
                Id = g.Id,
                GRNNumber = g.GRNNumber,
                CreatedAt = g.CreatedAt,
                Items = g.Items.Select(i => new GRNItemResponse
                {
                    Id = i.Id,
                    MenuItemId = i.MenuItemId,
                    ItemName = i.MenuItem != null ? i.MenuItem.Name : "Unknown",
                    Quantity = i.Quantity,
                    Price = i.Price,
                    CurrentQuantity = i.CurrentQuantity
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (grn == null)
            return NotFound(new { message = $"GRN {id} not found" });

        return Ok(grn);
    }

    // Direct edit — no approval needed
    [HttpPut("{id}")]
    public async Task<ActionResult> DirectEdit(int id, [FromBody] DirectEditGRNRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Reason is required" });

        var grn = await _db.GRNs
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grn == null)
            return NotFound(new { message = $"GRN {id} not found" });

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var changes = new List<string>();

            foreach (var reqItem in request.Items)
            {
                var grnItem = grn.Items.FirstOrDefault(gi => gi.MenuItemId == reqItem.MenuItemId);
                if (grnItem == null) continue;

                int soldQty = grnItem.Quantity - grnItem.CurrentQuantity;
                if (reqItem.RequestedQuantity < soldQty)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { message = $"Cannot set {reqItem.ItemName} to {reqItem.RequestedQuantity}. {soldQty} already sold." });
                }

                int qtyDiff = reqItem.RequestedQuantity - grnItem.Quantity;
                if (qtyDiff != 0)
                {
                    changes.Add($"{reqItem.ItemName}: {grnItem.Quantity} → {reqItem.RequestedQuantity}");
                    grnItem.Quantity = reqItem.RequestedQuantity;
                    grnItem.CurrentQuantity += qtyDiff;

                    var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.MenuItemId == reqItem.MenuItemId);
                    if (stock != null)
                        stock.Quantity += qtyDiff;
                }
            }

            // Check for removed items
            var requestItemIds = request.Items.Select(i => i.MenuItemId).ToHashSet();
            var removedItems = grn.Items.Where(gi => !requestItemIds.Contains(gi.MenuItemId)).ToList();
            foreach (var removed in removedItems)
            {
                int soldQty = removed.Quantity - removed.CurrentQuantity;
                if (soldQty > 0)
                {
                    await tx.RollbackAsync();
                    var mi = await _db.MenuItems.FindAsync(removed.MenuItemId);
                    return BadRequest(new { message = $"Cannot remove {mi?.Name ?? "item"}: {soldQty} already sold" });
                }

                var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.MenuItemId == removed.MenuItemId);
                if (stock != null)
                    stock.Quantity -= removed.CurrentQuantity;

                var menuItem = await _db.MenuItems.FindAsync(removed.MenuItemId);
                changes.Add($"{menuItem?.Name ?? "Item"}: REMOVED");
                _db.GRNItems.Remove(removed);
            }

            if (!changes.Any())
            {
                await tx.RollbackAsync();
                return Ok(new { message = "No changes detected" });
            }

            _db.GRNEditLogs.Add(new GRNEditLog
            {
                GRNId = id,
                Reason = request.Reason,
                ChangeSummary = string.Join("; ", changes),
                EditedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = $"GRN updated. Changes: {string.Join(", ", changes)}" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = $"Error: {ex.Message}" });
        }
    }

    [HttpGet("{id}/edits")]
    public async Task<ActionResult<List<GRNEditLogResponse>>> GetEdits(int id)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var edits = await _db.GRNEditLogs
            .Include(e => e.GRN)
            .Where(e => e.GRNId == id && e.EditedAt >= today && e.EditedAt < tomorrow)
            .OrderByDescending(e => e.EditedAt)
            .Select(e => new GRNEditLogResponse
            {
                Id = e.Id,
                GRNId = e.GRNId,
                GRNNumber = e.GRN != null ? e.GRN.GRNNumber : "",
                Reason = e.Reason,
                ChangeSummary = e.ChangeSummary,
                EditedAt = e.EditedAt
            })
            .ToListAsync();

        return Ok(edits);
    }

    [HttpPost]
    public async Task<ActionResult<GRNResponse>> Create([FromBody] CreateGRNRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "GRN must have at least one item" });

        var grnNumber = $"GRN-{DateTime.Now:yyyyMMddHHmmss}";

        var grn = new GRN
        {
            GRNNumber = grnNumber,
            CreatedAt = DateTime.Now,
            Items = request.Items.Select(i => new GRNItem
            {
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity,
                Price = i.Price,
                CurrentQuantity = i.Quantity
            }).ToList()
        };

        _db.GRNs.Add(grn);

        // Update stock for each item
        foreach (var item in request.Items)
        {
            var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.MenuItemId == item.MenuItemId);
            if (stock != null)
            {
                stock.Quantity += item.Quantity;
            }
            else
            {
                _db.Stocks.Add(new Stock
                {
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity
                });
            }
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = grn.Id }, new GRNResponse
        {
            Id = grn.Id,
            GRNNumber = grn.GRNNumber,
            CreatedAt = grn.CreatedAt,
            Items = grn.Items.Select(i => new GRNItemResponse
            {
                Id = i.Id,
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity,
                Price = i.Price,
                CurrentQuantity = i.CurrentQuantity
            }).ToList()
        });
    }
}
