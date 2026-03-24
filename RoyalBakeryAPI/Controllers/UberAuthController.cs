using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace RoyalBakeryAPI.Controllers;

/// <summary>
/// Handles Uber Eats OAuth Authorization Code flow.
/// Step 1: Open /api/uber/authorize in browser → redirects to Uber login
/// Step 2: User logs in and grants permission → Uber redirects to /api/uber/callback
/// Step 3: Callback exchanges code for token → stores linked to app
/// </summary>
[ApiController]
[Route("api/uber")]
public class UberAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UberAuthController> _logger;

    public UberAuthController(IConfiguration config, IHttpClientFactory httpClientFactory,
        ILogger<UberAuthController> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Open this URL in a browser to start the Uber authorization flow.
    /// GET /api/uber/authorize?account=0  (0 = first account, 1 = second account)
    /// </summary>
    [HttpGet("authorize")]
    public IActionResult Authorize([FromQuery] int account = 0)
    {
        var accounts = _config.GetSection("UberEats:Accounts").GetChildren().ToList();
        if (account < 0 || account >= accounts.Count)
            return BadRequest(new { message = $"Invalid account index. Valid: 0 to {accounts.Count - 1}" });

        var clientId = accounts[account]["ClientId"];
        var name = accounts[account]["Name"] ?? "Default";

        if (string.IsNullOrEmpty(clientId) || clientId.StartsWith("YOUR_"))
            return BadRequest(new { message = $"Account '{name}' has no ClientId configured in appsettings.json" });

        // Build the redirect URI (this API's callback endpoint)
        var request = HttpContext.Request;
        var redirectUri = $"{request.Scheme}://{request.Host}/api/uber/callback";

        // Build Uber authorization URL
        var sandbox = _config.GetValue<bool>("UberEats:SandboxMode");
        var authBase = sandbox ? "https://sandbox-login.uber.com" : "https://login.uber.com";

        // Use eats.store scope for production (eats.pos_provisioning is sandbox-only)
        var scope = sandbox ? "eats.pos_provisioning" : "eats.store+eats.order+eats.store.orders.read";

        var authUrl = $"{authBase}/oauth/v2/authorize" +
            $"?client_id={clientId}" +
            $"&response_type=code" +
            $"&scope={scope}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={account}";

        _logger.LogInformation("Uber OAuth: Redirecting to authorize for account '{Name}'. Redirect URI: {RedirectUri}",
            name, redirectUri);

        return Redirect(authUrl);
    }

    /// <summary>
    /// OAuth callback endpoint — Uber redirects here after user authorizes.
    /// Exchanges the authorization code for an access token.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code = null,
        [FromQuery] string state = "0",
        [FromQuery] string? error = null,
        [FromQuery(Name = "error_description")] string? errorDescription = null)
    {
        // Handle Uber error redirects
        if (!string.IsNullOrEmpty(error))
        {
            return Content($@"
<html><body style='font-family:Arial; padding:40px; background:#1a1a1a; color:white;'>
<h1 style='color:#ff4444;'>Authorization Error</h1>
<p>Error: {error}</p>
<p>Description: {errorDescription}</p>
<p>Please check that the Redirect URI <code>http://localhost:5000/api/uber/callback</code> is added in your Uber Developer Dashboard app settings.</p>
</body></html>", "text/html");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Content($@"
<html><body style='font-family:Arial; padding:40px; background:#1a1a1a; color:white;'>
<h1 style='color:#ff4444;'>No Authorization Code</h1>
<p>Uber did not send an authorization code. Please try again from: <a href='/api/uber/authorize?account=0' style='color:#4CAF50;'>Authorize Account 0</a></p>
</body></html>", "text/html");
        }

        int accountIndex = int.TryParse(state, out var idx) ? idx : 0;
        var accounts = _config.GetSection("UberEats:Accounts").GetChildren().ToList();

        if (accountIndex < 0 || accountIndex >= accounts.Count)
            return BadRequest(new { message = "Invalid account in state parameter" });

        var clientId = accounts[accountIndex]["ClientId"];
        var clientSecret = accounts[accountIndex]["ClientSecret"];
        var name = accounts[accountIndex]["Name"] ?? "Default";

        var request = HttpContext.Request;
        var redirectUri = $"{request.Scheme}://{request.Host}/api/uber/callback";

        var sandbox = _config.GetValue<bool>("UberEats:SandboxMode");
        var tokenUrl = sandbox
            ? (_config["UberEats:SandboxTokenUrl"] ?? "https://sandbox-login.uber.com/oauth/v2/token")
            : (_config["UberEats:LiveTokenUrl"] ?? "https://auth.uber.com/oauth/v2/token");

        // Exchange authorization code for access token
        var client = _httpClientFactory.CreateClient("UberEats");
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId!,
            ["client_secret"] = clientSecret!,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        });

        var response = await client.PostAsync(tokenUrl, tokenRequest);
        var json = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Uber OAuth token exchange for '{Name}': {Status}", name, response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Uber OAuth token exchange failed: {Response}", json);
            return Content($@"
<html><body style='font-family:Arial; padding:40px; background:#1a1a1a; color:white;'>
<h1 style='color:#ff4444;'>Authorization Failed</h1>
<p>Account: {name}</p>
<p>Error: {json}</p>
<p>Please check your ClientId and ClientSecret in appsettings.json and try again.</p>
</body></html>", "text/html");
        }

        var tokenData = JsonSerializer.Deserialize<UberAuthTokenResponse>(json);

        // Now use this token to list stores
        string storeInfo = "";
        if (tokenData?.AccessToken != null)
        {
            try
            {
                var baseUrl = sandbox
                    ? (_config["UberEats:SandboxBaseUrl"] ?? "https://test-api.uber.com/v1")
                    : (_config["UberEats:LiveBaseUrl"] ?? "https://api.uber.com/v1");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenData.AccessToken}");

                var storesResponse = await client.GetAsync($"{baseUrl}/eats/stores");
                var storesJson = await storesResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Uber stores response: {Json}", storesJson);

                var stores = JsonSerializer.Deserialize<UberStoresResponse>(storesJson);
                if (stores?.Stores != null && stores.Stores.Count > 0)
                {
                    var storeLines = stores.Stores.Select(s =>
                        $"<tr><td style='padding:8px; border:1px solid #444;'>{s.Name}</td>" +
                        $"<td style='padding:8px; border:1px solid #444; font-family:monospace;'>{s.StoreId}</td></tr>");
                    storeInfo = $@"
<h2 style='color:#4CAF50;'>Linked Stores Found!</h2>
<table style='border-collapse:collapse; width:100%;'>
<tr style='background:#333;'><th style='padding:8px; border:1px solid #444;'>Store Name</th><th style='padding:8px; border:1px solid #444;'>Store ID</th></tr>
{string.Join("", storeLines)}
</table>
<p>Copy the Store ID(s) above and paste them into appsettings.json under the corresponding account.</p>";
                }
                else
                {
                    storeInfo = $"<p>Stores response: {storesJson}</p>";
                }
            }
            catch (Exception ex)
            {
                storeInfo = $"<p>Could not fetch stores: {ex.Message}</p>";
            }
        }

        return Content($@"
<html><body style='font-family:Arial; padding:40px; background:#1a1a1a; color:white;'>
<h1 style='color:#4CAF50;'>Authorization Successful!</h1>
<p>Account: <strong>{name}</strong></p>
<p>Scopes: {tokenData?.Scope}</p>
<p>Token valid for: {(tokenData?.ExpiresIn ?? 0) / 3600} hours</p>
{storeInfo}
<hr style='border-color:#444; margin:20px 0;'>
<p style='color:#888;'>You can close this page now. The API will use client_credentials for ongoing polling.</p>
</body></html>", "text/html");
    }

    /// <summary>
    /// Shows current authorization status and provides authorize links.
    /// GET /api/uber/status
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        var accounts = _config.GetSection("UberEats:Accounts").GetChildren().ToList();
        var sandbox = _config.GetValue<bool>("UberEats:SandboxMode");
        var enabled = _config.GetValue<bool>("UberEats:Enabled");

        var accountInfo = accounts.Select((a, i) => new
        {
            Index = i,
            Name = a["Name"] ?? "Default",
            HasClientId = !string.IsNullOrEmpty(a["ClientId"]) && !a["ClientId"]!.StartsWith("YOUR_"),
            HasStoreId = !string.IsNullOrEmpty(a["StoreId"]) && !a["StoreId"]!.StartsWith("YOUR_"),
            StoreId = a["StoreId"],
            AuthorizeUrl = $"/api/uber/authorize?account={i}"
        });

        return Ok(new
        {
            enabled,
            sandboxMode = sandbox,
            accounts = accountInfo
        });
    }
}

public class UberAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}

public class UberStoresResponse
{
    [JsonPropertyName("stores")]
    public List<UberStoreInfo>? Stores { get; set; }
}

public class UberStoreInfo
{
    [JsonPropertyName("store_id")]
    public string? StoreId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
