using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RoyalBakeryAPI.Models;

namespace RoyalBakeryAPI.Services;

/// <summary>
/// Background service that polls Uber Eats API for new orders.
/// Flow: Get OAuth token → Poll created-orders → Get order details → Accept → Save to DB
/// Uses same DeliveryOrder/DeliveryOrderItem tables as PickMe.
/// </summary>
public class UberEatsPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<UberEatsPollingService> _logger;

    // Token cache per account (ClientId → token + expiry)
    private readonly Dictionary<string, (string Token, DateTime ExpiresAt)> _tokenCache = new();

    public UberEatsPollingService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<UberEatsPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _config.GetValue<bool>("UberEats:Enabled");
        if (!enabled)
        {
            _logger.LogInformation("Uber Eats polling is disabled in config.");
            return;
        }

        // Wait 15 seconds for DB init
        await Task.Delay(15_000, stoppingToken);

        var pollInterval = _config.GetValue<int>("UberEats:PollIntervalSeconds", 30);

        var sandboxMode = _config.GetValue<bool>("UberEats:SandboxMode");
        _logger.LogInformation("Uber Eats polling started. Mode: {Mode}, Interval: {Interval}s",
            sandboxMode ? "SANDBOX" : "LIVE", pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllAccounts(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uber Eats polling error");
            }

            await Task.Delay(pollInterval * 1000, stoppingToken);
        }
    }

    private async Task PollAllAccounts(CancellationToken ct)
    {
        var sandbox = _config.GetValue<bool>("UberEats:SandboxMode");
        var baseUrl = sandbox
            ? (_config["UberEats:SandboxBaseUrl"] ?? "https://test-api.uber.com/v1")
            : (_config["UberEats:LiveBaseUrl"] ?? "https://api.uber.com/v1");
        var accounts = _config.GetSection("UberEats:Accounts").GetChildren();

        foreach (var account in accounts)
        {
            var clientId = account["ClientId"];
            var clientSecret = account["ClientSecret"];
            var storeId = account["StoreId"];
            var name = account["Name"] ?? "Default";
            var hasBakery = account.GetValue<bool>("HasBakeryItems");
            var hasRestaurant = account.GetValue<bool>("HasRestaurantItems");

            if (string.IsNullOrEmpty(clientId) || clientId.StartsWith("YOUR_") ||
                string.IsNullOrEmpty(storeId) || storeId.StartsWith("YOUR_"))
            {
                _logger.LogDebug("Skipping Uber Eats account '{Name}' — not configured", name);
                continue;
            }

            try
            {
                var token = await GetAccessToken(clientId!, clientSecret!, ct);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Uber Eats: Could not get access token for account '{Name}'", name);
                    continue;
                }

                await PollAccount(baseUrl, token, storeId!, name, hasBakery, hasRestaurant, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling Uber Eats account '{Name}'", name);
            }
        }
    }

    private async Task<string?> GetAccessToken(string clientId, string clientSecret, CancellationToken ct)
    {
        // Check cache — refresh if expiring within 5 minutes
        if (_tokenCache.TryGetValue(clientId, out var cached) &&
            cached.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return cached.Token;
        }

        var sandbox = _config.GetValue<bool>("UberEats:SandboxMode");
        var tokenUrl = sandbox
            ? (_config["UberEats:SandboxTokenUrl"] ?? "https://sandbox-login.uber.com/oauth/v2/token")
            : (_config["UberEats:LiveTokenUrl"] ?? "https://auth.uber.com/oauth/v2/token");
        var client = _httpClientFactory.CreateClient("UberEats");

        // Try multiple scope combinations — production and sandbox apps accept different scopes
        string[] scopeSets = new[]
        {
            "eats.store.orders.read eats.order eats.store",  // sandbox scopes
            "eats.deliveries eats.store eats.store.orders.read",  // production scopes v1
            "eats.deliveries",  // minimal production scope
        };

        HttpResponseMessage response = null!;
        string err = "";

        foreach (var scope in scopeSets)
        {
            var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "client_credentials",
                ["scope"] = scope
            });

            response = await client.PostAsync(tokenUrl, requestBody, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Uber Eats: Token obtained with scope: {Scope}", scope);
                break;
            }

            err = await response.Content.ReadAsStringAsync(ct);

            // If error is not scope-related, stop trying
            if (!err.Contains("invalid_scope"))
                break;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Uber Eats token request failed: {Status} — {Error}", response.StatusCode, err);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var tokenResponse = JsonSerializer.Deserialize<UberTokenResponse>(json);

        if (tokenResponse?.AccessToken == null) return null;

        // Cache token (expires_in is in seconds, default 30 days)
        var expiresIn = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 2592000;
        _tokenCache[clientId] = (tokenResponse.AccessToken, DateTime.UtcNow.AddSeconds(expiresIn));

        _logger.LogInformation("Uber Eats: Access token obtained, expires in {Hours}h", expiresIn / 3600);
        return tokenResponse.AccessToken;
    }

    private async Task PollAccount(string baseUrl, string token, string storeId,
        string accountName, bool hasBakery, bool hasRestaurant, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("UberEats");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Poll for created orders (new orders waiting to be accepted)
        var url = $"{baseUrl}/eats/stores/{storeId}/created-orders";
        var response = await client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Uber Eats created-orders returned {Status} for account '{Account}'",
                response.StatusCode, accountName);
            return;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<UberCreatedOrdersResponse>(json);

        if (result?.Orders == null || result.Orders.Count == 0)
            return;

        _logger.LogInformation("Uber Eats [{Account}]: Found {Count} created orders", accountName, result.Orders.Count);

        foreach (var orderSummary in result.Orders)
        {
            try
            {
                await ProcessOrder(baseUrl, token, orderSummary.Id!, accountName,
                    hasBakery, hasRestaurant, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Uber Eats order {OrderId}", orderSummary.Id);
            }
        }
    }

    private async Task ProcessOrder(string baseUrl, string token, string orderId,
        string accountName, bool hasBakery, bool hasRestaurant, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BakeryDbContext>();

        // Check if already processed
        var exists = await db.DeliveryOrders
            .AnyAsync(d => d.PlatformOrderId == orderId && d.PlatformName == "UberEats", ct);
        if (exists) return;

        // Get order details
        var client = _httpClientFactory.CreateClient("UberEats");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var detailUrl = $"{baseUrl}/eats/order/{orderId}";
        var response = await client.GetAsync(detailUrl, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Uber Eats order detail failed for {OrderId}: {Status}", orderId, response.StatusCode);
            return;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var order = JsonSerializer.Deserialize<UberOrderDetail>(json);
        if (order == null) return;

        _logger.LogInformation("New Uber Eats order: {OrderId} from account '{Account}'", orderId, accountName);

        // Accept the order
        await AcceptOrder(baseUrl, token, orderId, ct);

        // Parse items
        var deliveryItems = new List<DeliveryOrderItem>();
        var restaurantItems = new List<(int localId, string name, int qty, decimal price)>();
        var bakeryItems = new List<(int localId, string name, int qty, decimal price)>();

        var allCartItems = order.Cart?.Items ?? new List<UberCartItem>();
        foreach (var item in allCartItems)
        {
            var refId = item.ExternalData ?? "";
            var itemType = "U";
            int? localItemId = null;
            int qty = item.Quantity > 0 ? item.Quantity : 1;
            decimal price = item.Price?.UnitPrice?.Amount != null
                ? (decimal)item.Price.UnitPrice.Amount / 100m  // Uber prices are in cents
                : 0;
            decimal total = price * qty;

            // Parse ref_id: same format as PickMe (B-123, R-45, or legacy 44383_B123)
            if (refId.StartsWith("B-", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(refId[2..], out int bakeryId))
            {
                itemType = "B";
                localItemId = bakeryId;
                bakeryItems.Add((bakeryId, item.Title ?? "", qty, price));
            }
            else if (refId.StartsWith("R-", StringComparison.OrdinalIgnoreCase) &&
                     int.TryParse(refId[2..], out int restId))
            {
                itemType = "R";
                localItemId = restId;
                restaurantItems.Add((restId, item.Title ?? "", qty, price));
            }
            else if (refId.Contains('_'))
            {
                var afterUnderscore = refId.Split('_').Last();
                if (afterUnderscore.StartsWith("B", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(afterUnderscore[1..], out int legacyBakeryId))
                {
                    itemType = "B";
                    localItemId = legacyBakeryId;
                    bakeryItems.Add((legacyBakeryId, item.Title ?? "", qty, price));
                }
                else if (afterUnderscore.StartsWith("R", StringComparison.OrdinalIgnoreCase) &&
                         int.TryParse(afterUnderscore[1..], out int legacyRestId))
                {
                    itemType = "R";
                    localItemId = legacyRestId;
                    restaurantItems.Add((legacyRestId, item.Title ?? "", qty, price));
                }
            }

            // Build customizations string
            var customizations = "";
            if (item.SelectedModifierGroups != null)
            {
                var parts = new List<string>();
                foreach (var group in item.SelectedModifierGroups)
                {
                    if (group.SelectedItems != null)
                    {
                        foreach (var mod in group.SelectedItems)
                            parts.Add($"{group.Title}: {mod.Title}");
                    }
                }
                customizations = string.Join(", ", parts);
            }

            deliveryItems.Add(new DeliveryOrderItem
            {
                PlatformItemId = 0,
                PlatformRefId = refId,
                ItemName = item.Title ?? "Unknown",
                Quantity = qty,
                PricePerItem = price,
                TotalPrice = total,
                SpecialInstructions = item.SpecialInstructions,
                Options = customizations,
                ItemType = itemType,
                LocalItemId = localItemId
            });
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
                CashierName = "UberEats",
                OrderSource = "Ubereats",
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
        int? bakerySaleId = null;
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
                CashierName = "UberEats",
                InvoiceNumber = $"UE-{nextNum:D5}",
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
            foreach (var bi in bakeryItems)
            {
                var stock = await db.Stocks.FirstOrDefaultAsync(s => s.MenuItemId == bi.localId, ct);
                if (stock != null)
                {
                    stock.Quantity = Math.Max(0, stock.Quantity - bi.qty);
                }

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
            }

            await db.SaveChangesAsync(ct);
            bakerySaleId = sale.Id;
        }

        // Compute order total from items or from Uber payment
        decimal orderTotal = deliveryItems.Sum(i => i.TotalPrice);
        if (order.Payment?.Charges?.Total?.Amount != null)
            orderTotal = (decimal)order.Payment.Charges.Total.Amount / 100m;

        // Create DeliveryOrder record
        var deliveryOrder = new DeliveryOrder
        {
            PlatformName = "UberEats",
            PlatformOrderId = orderId,
            AccountName = accountName,
            RestaurantSaleId = restaurantSaleId,
            BakerySaleId = bakerySaleId,
            CustomerPhone = order.Eater?.Phone?.Number,
            CustomerAddress = order.Eater?.DeliveryAddress,
            DeliveryMode = order.Type ?? "Delivery",
            PlatformStatus = "Accepted",
            OrderTotal = orderTotal,
            PaymentMethod = "UberEats",
            DeliveryNote = order.Cart?.SpecialInstructions,
            ReceivedAt = DateTime.Now,
            KotStatus = restaurantItems.Count > 0 ? 0 : 2,
            RawOrderJson = json.Length > 4000 ? json[..4000] : json,
            Items = deliveryItems
        };

        db.DeliveryOrders.Add(deliveryOrder);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Uber Eats order {OrderId} saved. Restaurant items: {RCount}, Bakery items: {BCount}",
            orderId, restaurantItems.Count, bakeryItems.Count);
    }

    private async Task AcceptOrder(string baseUrl, string token, string orderId, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UberEats");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var body = new StringContent(
                JsonSerializer.Serialize(new { reason = "POS auto-accept" }),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"{baseUrl}/eats/orders/{orderId}/accept_pos_order", body, ct);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Uber Eats order {OrderId} accepted", orderId);
            else
                _logger.LogWarning("Uber Eats accept failed for {OrderId}: {Status}", orderId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to accept Uber Eats order {OrderId}", orderId);
        }
    }
}

// ===== Uber Eats API Response Models =====

public class UberTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class UberCreatedOrdersResponse
{
    [JsonPropertyName("orders")]
    public List<UberOrderSummary>? Orders { get; set; }
}

public class UberOrderSummary
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("current_state")]
    public string? CurrentState { get; set; }

    [JsonPropertyName("placed_at")]
    public string? PlacedAt { get; set; }
}

public class UberOrderDetail
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("display_id")]
    public string? DisplayId { get; set; }

    [JsonPropertyName("current_state")]
    public string? CurrentState { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("placed_at")]
    public string? PlacedAt { get; set; }

    [JsonPropertyName("estimated_ready_for_pickup_at")]
    public string? EstimatedReadyAt { get; set; }

    [JsonPropertyName("cart")]
    public UberCart? Cart { get; set; }

    [JsonPropertyName("eater")]
    public UberEater? Eater { get; set; }

    [JsonPropertyName("payment")]
    public UberPayment? Payment { get; set; }

    [JsonPropertyName("store")]
    public UberStore? Store { get; set; }
}

public class UberCart
{
    [JsonPropertyName("items")]
    public List<UberCartItem>? Items { get; set; }

    [JsonPropertyName("special_instructions")]
    public string? SpecialInstructions { get; set; }
}

public class UberCartItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("external_data")]
    public string? ExternalData { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public UberItemPrice? Price { get; set; }

    [JsonPropertyName("special_instructions")]
    public string? SpecialInstructions { get; set; }

    [JsonPropertyName("selected_modifier_groups")]
    public List<UberModifierGroup>? SelectedModifierGroups { get; set; }
}

public class UberItemPrice
{
    [JsonPropertyName("unit_price")]
    public UberMoney? UnitPrice { get; set; }

    [JsonPropertyName("total_price")]
    public UberMoney? TotalPrice { get; set; }
}

public class UberMoney
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("formatted_amount")]
    public string? FormattedAmount { get; set; }
}

public class UberModifierGroup
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("selected_items")]
    public List<UberModifierItem>? SelectedItems { get; set; }
}

public class UberModifierItem
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("external_data")]
    public string? ExternalData { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public UberItemPrice? Price { get; set; }
}

public class UberEater
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("phone")]
    public UberPhone? Phone { get; set; }

    [JsonPropertyName("delivery_address")]
    public string? DeliveryAddress { get; set; }
}

public class UberPhone
{
    [JsonPropertyName("number")]
    public string? Number { get; set; }
}

public class UberPayment
{
    [JsonPropertyName("charges")]
    public UberCharges? Charges { get; set; }
}

public class UberCharges
{
    [JsonPropertyName("total")]
    public UberMoney? Total { get; set; }

    [JsonPropertyName("sub_total")]
    public UberMoney? SubTotal { get; set; }

    [JsonPropertyName("tax")]
    public UberMoney? Tax { get; set; }
}

public class UberStore
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
