using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RoyalBakeryAPI.Models;

namespace RoyalBakeryAPI.Services;

/// <summary>
/// Background service that polls PickMe API for new orders.
/// When a "Merchant Confirmed" order is found:
/// - Maps items via ref_id (B-{id} for bakery, R-{id} for restaurant)
/// - Creates RestaurantSale for restaurant items (no stock deduction)
/// - Creates Sale + deducts Stock for bakery items
/// - Saves DeliveryOrder record for tracking
/// - Sets KotStatus=0 (PendingKOT) so Restaurant POS can print
/// </summary>
public class PickMePollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PickMePollingService> _logger;

    public PickMePollingService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<PickMePollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _config.GetValue<bool>("PickMe:Enabled");
        if (!enabled)
        {
            _logger.LogInformation("PickMe polling is disabled in config.");
            return;
        }

        // Wait 10 seconds for DB init to complete before first poll
        await Task.Delay(10_000, stoppingToken);

        var pollInterval = _config.GetValue<int>("PickMe:PollIntervalSeconds", 30);
        var hoursBack = _config.GetValue<int>("PickMe:PollHoursBack", 2);

        _logger.LogInformation("PickMe polling started. Interval: {Interval}s, Hours back: {Hours}", pollInterval, hoursBack);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllAccounts(hoursBack, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PickMe polling error");
            }

            await Task.Delay(pollInterval * 1000, stoppingToken);
        }
    }

    private async Task PollAllAccounts(int hoursBack, CancellationToken ct)
    {
        var sandbox = _config.GetValue<bool>("PickMe:SandboxMode");
        var baseUrl = sandbox
            ? _config["PickMe:SandboxBaseUrl"]
            : _config["PickMe:LiveBaseUrl"];

        var accounts = _config.GetSection("PickMe:Accounts").GetChildren();

        foreach (var account in accounts)
        {
            var apiKey = account["ApiKey"];
            var name = account["Name"] ?? "Default";
            var hasBakery = account.GetValue<bool>("HasBakeryItems");
            var hasRestaurant = account.GetValue<bool>("HasRestaurantItems");

            if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
            {
                _logger.LogDebug("Skipping PickMe account '{Name}' — no API key configured", name);
                continue;
            }

            try
            {
                await PollAccount(baseUrl!, apiKey, name, hasBakery, hasRestaurant, hoursBack, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling PickMe account '{Name}'", name);
            }
        }
    }

    private async Task PollAccount(string baseUrl, string apiKey, string accountName,
        bool hasBakery, bool hasRestaurant, int hoursBack, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("PickMe");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

        int page = 1;
        int totalProcessed = 0;

        while (true)
        {
            var url = $"{baseUrl}/pickme/pos/v1/joblist?page={page}&hours={hoursBack}";
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PickMe joblist returned {Status} for account '{Account}'",
                    response.StatusCode, accountName);
                break;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PickMeJobListResponse>(json);

            if (result?.Data == null || result.Data.Count == 0)
                break;

            foreach (var job in result.Data)
            {
                await ProcessJob(job, accountName, hasBakery, hasRestaurant, json, ct);
                totalProcessed++;
            }

            // Check if more pages
            if (result.Params?.Pagination != null &&
                page * result.Params.Pagination.Size < result.Params.Pagination.TotalRecords)
            {
                page++;
            }
            else
            {
                break;
            }
        }

        if (totalProcessed > 0)
            _logger.LogInformation("PickMe [{Account}]: Processed {Count} jobs", accountName, totalProcessed);
    }

    private async Task ProcessJob(PickMeJob job, string accountName,
        bool hasBakery, bool hasRestaurant, string rawJson, CancellationToken ct)
    {
        // Only process orders that have been confirmed by merchant
        var status = job.Status?.Name ?? "";
        if (string.IsNullOrEmpty(status)) return;

        // Skip statuses we don't care about
        if (status == "Job Declined" || status == "Job Timed Out") return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BakeryDbContext>();

        // Check if we already processed this order
        var exists = await db.DeliveryOrders
            .AnyAsync(d => d.PlatformOrderId == job.PickmeJobId && d.PlatformName == "PickMe", ct);
        if (exists)
        {
            // Update status if changed
            var existing = await db.DeliveryOrders
                .FirstAsync(d => d.PlatformOrderId == job.PickmeJobId && d.PlatformName == "PickMe", ct);
            if (existing.PlatformStatus != status)
            {
                existing.PlatformStatus = status;
                if (status == "Job Completed")
                    existing.CompletedAt = DateTime.Now;
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        // Only create sale for "Merchant Confirmed" status
        if (status != "Merchant Confirmed") return;

        _logger.LogInformation("New PickMe order: {JobId} from account '{Account}'",
            job.PickmeJobId, accountName);

        // Parse items and determine types
        var deliveryItems = new List<DeliveryOrderItem>();
        var restaurantItems = new List<(int localId, string name, int qty, decimal price)>();
        var bakeryItems = new List<(int localId, string name, int qty, decimal price)>();

        if (job.Order?.Items != null)
        {
            foreach (var item in job.Order.Items)
            {
                var refId = item.RefId ?? "";
                var itemType = "U";
                int? localItemId = null;

                // Parse ref_id format: "B-123" or "R-45" (preferred)
                // Also supports legacy format "44383_B123" or "44383_R45" (prefix_TypeId)
                if (refId.StartsWith("B-", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(refId[2..], out int bakeryId))
                {
                    itemType = "B";
                    localItemId = bakeryId;
                    bakeryItems.Add((bakeryId, item.Name ?? "", item.Qty, item.Total / Math.Max(item.Qty, 1)));
                }
                else if (refId.StartsWith("R-", StringComparison.OrdinalIgnoreCase) &&
                         int.TryParse(refId[2..], out int restId))
                {
                    itemType = "R";
                    localItemId = restId;
                    restaurantItems.Add((restId, item.Name ?? "", item.Qty, item.Total / Math.Max(item.Qty, 1)));
                }
                else if (refId.Contains('_'))
                {
                    // Legacy format: "44383_B123" or "44383_R45"
                    var afterUnderscore = refId.Split('_').Last();
                    if (afterUnderscore.StartsWith("B", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(afterUnderscore[1..], out int legacyBakeryId))
                    {
                        itemType = "B";
                        localItemId = legacyBakeryId;
                        bakeryItems.Add((legacyBakeryId, item.Name ?? "", item.Qty, item.Total / Math.Max(item.Qty, 1)));
                    }
                    else if (afterUnderscore.StartsWith("R", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(afterUnderscore[1..], out int legacyRestId))
                    {
                        itemType = "R";
                        localItemId = legacyRestId;
                        restaurantItems.Add((legacyRestId, item.Name ?? "", item.Qty, item.Total / Math.Max(item.Qty, 1)));
                    }
                }

                // Build options string from nested options
                var optionsStr = "";
                if (item.Options != null)
                {
                    var parts = new List<string>();
                    foreach (var opt in item.Options)
                    {
                        if (opt.Items != null)
                        {
                            foreach (var sub in opt.Items)
                                parts.Add($"{opt.Name}: {sub.Name}");
                        }
                    }
                    optionsStr = string.Join(", ", parts);
                }

                deliveryItems.Add(new DeliveryOrderItem
                {
                    PlatformItemId = item.Id,
                    PlatformRefId = refId,
                    ItemName = item.Name ?? "Unknown",
                    Quantity = item.Qty,
                    PricePerItem = item.Qty > 0 ? item.Total / item.Qty : item.Total,
                    TotalPrice = item.Total,
                    SpecialInstructions = item.SpIns,
                    Options = optionsStr,
                    ItemType = itemType,
                    LocalItemId = localItemId
                });
            }
        }

        // Create RestaurantSale if there are restaurant items
        int? restaurantSaleId = null;
        if (restaurantItems.Count > 0 && hasRestaurant)
        {
            var nextNum = db.RestaurantSales.Any()
                ? db.RestaurantSales.Max(s => s.Id) + 1 : 1;

            var sale = new RestaurantSale
            {
                InvoiceNumber = $"RES-{nextNum:D5}",
                DateTime = DateTime.Now,
                TotalAmount = restaurantItems.Sum(i => i.price * i.qty),
                CashAmount = 0,
                CardAmount = 0,
                ChangeGiven = 0,
                CashierName = "PickMe",
                OrderSource = "Pickme Food",
                Items = restaurantItems.Select(i => new RestaurantSaleItem
                {
                    RestaurantItemId = i.localId,
                    ItemName = i.name,
                    Quantity = i.qty,
                    PricePerItem = i.price,
                    TotalPrice = i.qty * i.price
                }).ToList()
            };
            db.RestaurantSales.Add(sale);
            await db.SaveChangesAsync(ct);
            restaurantSaleId = sale.Id;
        }

        // Create bakery Sale + deduct stock if there are bakery items
        // Online orders (PickMe/UberEats) can go negative — shortages tracked in PendingStocks
        int? bakerySaleId = null;
        var pendingStockItems = new List<(int menuItemId, int shortage)>();

        if (bakeryItems.Count > 0 && hasBakery)
        {
            var nextNum = db.Sales.Any() ? db.Sales.Max(s => s.Id) + 1 : 1;

            var sale = new Sale
            {
                DateTime = DateTime.Now,
                TotalAmount = bakeryItems.Sum(i => i.price * i.qty),
                CashAmount = 0,
                CardAmount = 0,
                ChangeGiven = 0,
                CashierName = "PickMe",
                InvoiceNumber = $"PM-{nextNum:D5}",
                Items = bakeryItems.Select(i => new SaleItem
                {
                    MenuItemId = i.localId,
                    ItemName = i.name,
                    Quantity = i.qty,
                    PricePerItem = i.price,
                    TotalPrice = i.qty * i.price
                }).ToList()
            };
            db.Sales.Add(sale);

            // Deduct stock + GRNItems CurrentQuantity (FIFO) for each bakery item
            // Allow stock to go negative for online orders — track shortage in PendingStocks
            foreach (var bi in bakeryItems)
            {
                var stock = await db.Stocks.FirstOrDefaultAsync(s => s.MenuItemId == bi.localId, ct);
                int availableStock = stock?.Quantity ?? 0;

                if (stock != null)
                {
                    stock.Quantity -= bi.qty; // Can go negative for online orders
                }

                // FIFO GRN deduction: deduct from oldest GRN items first
                var grnItems = await db.GRNItems
                    .Where(g => g.MenuItemId == bi.localId && g.CurrentQuantity > 0)
                    .OrderBy(g => g.Id)
                    .ToListAsync(ct);

                int remaining = bi.qty;
                foreach (var gi in grnItems)
                {
                    if (remaining <= 0) break;
                    int deduct = Math.Min(gi.CurrentQuantity, remaining);
                    gi.CurrentQuantity -= deduct;
                    remaining -= deduct;
                }

                // Track shortage if stock was insufficient
                if (availableStock < bi.qty)
                {
                    int shortage = availableStock - bi.qty; // negative value
                    pendingStockItems.Add((bi.localId, shortage));
                }
            }

            await db.SaveChangesAsync(ct);
            bakerySaleId = sale.Id;
        }

        // Create DeliveryOrder record
        var deliveryOrder = new DeliveryOrder
        {
            PlatformName = "PickMe",
            PlatformOrderId = job.PickmeJobId ?? "",
            AccountName = accountName,
            RestaurantSaleId = restaurantSaleId,
            BakerySaleId = bakerySaleId,
            CustomerPhone = job.Customer?.ContactNumber,
            CustomerAddress = job.Customer?.Location?.Address,
            DeliveryMode = job.DeliveryMode ?? "Delivery",
            PlatformStatus = status,
            OrderTotal = job.Payment?.Total ?? 0,
            PaymentMethod = job.Payment?.Method,
            DeliveryNote = job.Order?.DeliveryNote,
            ReceivedAt = DateTime.Now,
            KotStatus = restaurantItems.Count > 0 ? 0 : 2, // PendingKOT if restaurant items, else already done
            RawOrderJson = rawJson.Length > 4000 ? rawJson[..4000] : rawJson,
            Items = deliveryItems
        };

        db.DeliveryOrders.Add(deliveryOrder);
        await db.SaveChangesAsync(ct);

        // Create PendingStock records for any stock shortages
        if (pendingStockItems.Count > 0)
        {
            foreach (var (menuItemId, shortage) in pendingStockItems)
            {
                db.PendingStocks.Add(new PendingStock
                {
                    DeliveryOrderId = deliveryOrder.Id,
                    MenuItemId = menuItemId,
                    PendingQuantity = shortage,
                    CurrentPendingQuantity = shortage,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.Now
                });
            }
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("PickMe order {JobId}: Created {Count} pending stock records",
                job.PickmeJobId, pendingStockItems.Count);
        }

        _logger.LogInformation("PickMe order {JobId} saved. Restaurant items: {RCount}, Bakery items: {BCount}",
            job.PickmeJobId, restaurantItems.Count, bakeryItems.Count);
    }
}

// ===== JSON Converters for flexible PickMe API fields =====

/// <summary>
/// Handles JSON fields that can be either a number or a string.
/// PickMe sends pickme_job_id as a number, but we store it as string.
/// </summary>
public class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(),
            JsonTokenType.Null => null,
            _ => reader.GetString()
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

/// <summary>
/// Handles JSON fields that can be either a number or a string but should deserialize to decimal.
/// PickMe sends item.total as a string (e.g. "550.00") but we need it as decimal.
/// </summary>
public class FlexibleDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String => decimal.TryParse(reader.GetString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m,
            _ => 0m
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

/// <summary>
/// Handles JSON fields that can be either a number or a string but should deserialize to int.
/// PickMe may send qty/id as strings.
/// </summary>
public class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => int.TryParse(reader.GetString(), out var i) ? i : 0,
            _ => 0
        };
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

// ===== PickMe API Response Models =====

public class PickMeJobListResponse
{
    [JsonPropertyName("params")]
    public PickMeParams? Params { get; set; }

    [JsonPropertyName("data")]
    public List<PickMeJob>? Data { get; set; }
}

public class PickMeParams
{
    [JsonPropertyName("pagination")]
    public PickMePagination? Pagination { get; set; }
}

public class PickMePagination
{
    [JsonPropertyName("page")]
    public int Page { get; set; }
    [JsonPropertyName("size")]
    public int Size { get; set; }
    [JsonPropertyName("total_records")]
    public int TotalRecords { get; set; }
}

public class PickMeJob
{
    [JsonPropertyName("pickme_job_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? PickmeJobId { get; set; }

    [JsonPropertyName("customer")]
    public PickMeCustomer? Customer { get; set; }

    [JsonPropertyName("outlet")]
    public PickMeOutlet? Outlet { get; set; }

    [JsonPropertyName("order")]
    public PickMeOrder? Order { get; set; }

    [JsonPropertyName("payment")]
    public PickMePayment? Payment { get; set; }

    [JsonPropertyName("status")]
    public PickMeStatus? Status { get; set; }

    [JsonPropertyName("delivery_mode")]
    public string? DeliveryMode { get; set; }

    [JsonPropertyName("created_timestamp")]
    public string? CreatedTimestamp { get; set; }
}

public class PickMeCustomer
{
    [JsonPropertyName("contact_number")]
    public string? ContactNumber { get; set; }

    [JsonPropertyName("location")]
    public PickMeLocation? Location { get; set; }
}

public class PickMeLocation
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public class PickMeOutlet
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("contact_number")]
    public string? ContactNumber { get; set; }

    [JsonPropertyName("location")]
    public PickMeLocation? Location { get; set; }
}

public class PickMeOrder
{
    [JsonPropertyName("items")]
    public List<PickMeOrderItem>? Items { get; set; }

    [JsonPropertyName("delivery_note")]
    public string? DeliveryNote { get; set; }
}

public class PickMeOrderItem
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Id { get; set; }

    [JsonPropertyName("ref_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? RefId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("qty")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Qty { get; set; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal Total { get; set; }

    [JsonPropertyName("sp_ins")]
    public string? SpIns { get; set; }

    [JsonPropertyName("options")]
    public List<PickMeOption>? Options { get; set; }
}

public class PickMeOption
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("items")]
    public List<PickMeSubOption>? Items { get; set; }
}

public class PickMeSubOption
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("qty")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Qty { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal Price { get; set; }

    [JsonPropertyName("ref_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? RefId { get; set; }
}

public class PickMePayment
{
    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal Total { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }
}

public class PickMeStatus
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
