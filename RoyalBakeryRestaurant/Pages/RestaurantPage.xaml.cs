using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using RoyalBakeryCashier.Helpers;

namespace RoyalBakeryRestaurant.Pages;

public partial class RestaurantPage : ContentPage
{
    private readonly StockDbContext _dbContext;
    private List<ItemViewModel> _allItems = new();
    private ObservableCollection<CartItem> _cartItems;
    private bool _loaded = false;
    private CancellationTokenSource? _searchDebounce;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private CancellationTokenSource? _kotPollCts;

    public RestaurantPage()
    {
        InitializeComponent();
        _dbContext = new StockDbContext();
        _cartItems = new ObservableCollection<CartItem>();
        CartCollectionView.ItemsSource = _cartItems;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        try
        {
            await LoadCategoriesAsync();
            await LoadItemsAsync();
            StartDeliveryKotPolling(); // Start polling API for delivery KOT orders
        }
        catch (Exception ex)
        {
            await DisplayAlert("Database Error",
                $"Could not load restaurant data.\n\n{ex.Message}", "OK");
        }
    }

    private static readonly Color[] _categoryColors = new[]
    {
        Color.FromArgb("#E91E63"),
        Color.FromArgb("#2196F3"),
        Color.FromArgb("#FF9800"),
        Color.FromArgb("#4CAF50"),
        Color.FromArgb("#9C27B0"),
        Color.FromArgb("#F44336"),
    };

    private async Task LoadCategoriesAsync()
    {
        var categories = await Task.Run(() => _dbContext.RestaurantCategories.OrderBy(c => c.Id).ToList());
        CategoryGrid.Children.Clear();
        CategoryGrid.RowDefinitions.Clear();

        var allButtons = new List<(string Name, int? CatId)> { ("All", null) };
        foreach (var cat in categories)
            allButtons.Add((cat.Name, cat.Id));

        int cols = 6;
        int rows = (int)Math.Ceiling(allButtons.Count / (double)cols);
        for (int r = 0; r < rows; r++)
            CategoryGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < allButtons.Count; i++)
        {
            var (name, catId) = allButtons[i];
            Color bgColor = catId == null
                ? Color.FromArgb("#607D8B")
                : _categoryColors[i % _categoryColors.Length];

            var btn = new Button
            {
                Text = name,
                BackgroundColor = bgColor,
                TextColor = Colors.White,
                CornerRadius = 8,
                FontSize = 13,
                HeightRequest = 44,
                Margin = 0,
            };
            btn.Clicked += (s, e) => FilterItems(catId);
            Grid.SetRow(btn, i / cols);
            Grid.SetColumn(btn, i % cols);
            CategoryGrid.Children.Add(btn);
        }
    }

    private async Task LoadItemsAsync()
    {
        var items = await Task.Run(() =>
            _dbContext.RestaurantItems
                .Include(ri => ri.Category)
                .Select(ri => new ItemViewModel
                {
                    RestaurantItemId = ri.Id,
                    Name = ri.Name,
                    Price = ri.Price,
                    RestaurantCategoryId = ri.RestaurantCategoryId,
                })
                .ToList());

        _allItems = items;
        ItemsCollectionView.ItemsSource = new ObservableCollection<ItemViewModel>(_allItems);
    }

    private int? _currentCategoryId = null;

    private void FilterItems(int? categoryId)
    {
        _currentCategoryId = categoryId;
        ItemSearchEntry.Text = string.Empty;

        IEnumerable<ItemViewModel> filtered = _allItems;
        if (categoryId != null)
            filtered = _allItems.Where(i => i.RestaurantCategoryId == categoryId);

        ItemsCollectionView.ItemsSource = new ObservableCollection<ItemViewModel>(filtered);
    }

    private void ItemSearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce?.Cancel();
        var keyword = (e.NewTextValue ?? "").Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            FilterItems(_currentCategoryId);
            return;
        }

        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        Task.Delay(150, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var filtered = _allItems
                        .Where(i => i.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    ItemsCollectionView.ItemsSource = new ObservableCollection<ItemViewModel>(filtered);
                });
        });
    }

    private void ItemsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ItemViewModel selected)
        {
            int qty = 1;
            var entered = (QuantityEntry.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(entered) && int.TryParse(entered, out int parsed) && parsed > 0)
                qty = parsed;

            AddToCart(selected, qty);
            QuantityEntry.Text = string.Empty;
            ItemsCollectionView.SelectedItem = null;
        }
    }

    private void AddToCart(ItemViewModel item, int qty)
    {
        if (qty <= 0) return;

        var existing = _cartItems.FirstOrDefault(c => c.RestaurantItemId == item.RestaurantItemId);
        if (existing != null)
        {
            int newQty = existing.Quantity + qty;
            int idx = _cartItems.IndexOf(existing);
            _cartItems[idx] = new CartItem
            {
                RestaurantItemId = existing.RestaurantItemId,
                Name = existing.Name,
                Quantity = newQty,
                Price = existing.Price,
                Total = newQty * existing.Price
            };
        }
        else
        {
            _cartItems.Add(new CartItem
            {
                RestaurantItemId = item.RestaurantItemId,
                Name = item.Name,
                Quantity = qty,
                Price = item.Price,
                Total = qty * item.Price
            });
        }

        UpdateTotal();
    }

    private async void CartItemName_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is CartItem item)
        {
            bool confirm = await DisplayAlert("Remove Item",
                $"Remove \"{item.Name}\" from order?", "Delete", "Cancel");
            if (confirm)
            {
                _cartItems.Remove(item);
                UpdateTotal();
            }
        }
    }

    private void DecreaseQuantity_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartItem item)
        {
            int newQty = item.Quantity - 1;
            if (newQty <= 0)
            {
                _cartItems.Remove(item);
            }
            else
            {
                int idx = _cartItems.IndexOf(item);
                if (idx >= 0)
                    _cartItems[idx] = new CartItem
                    {
                        RestaurantItemId = item.RestaurantItemId,
                        Name = item.Name,
                        Quantity = newQty,
                        Price = item.Price,
                        Total = newQty * item.Price
                    };
            }
            UpdateTotal();
        }
    }

    private void IncreaseQuantity_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartItem item)
        {
            int newQty = item.Quantity + 1;
            int idx = _cartItems.IndexOf(item);
            if (idx >= 0)
                _cartItems[idx] = new CartItem
                {
                    RestaurantItemId = item.RestaurantItemId,
                    Name = item.Name,
                    Quantity = newQty,
                    Price = item.Price,
                    Total = newQty * item.Price
                };
            UpdateTotal();
        }
    }

    private void Keypad_Clicked(object sender, EventArgs e)
    {
        if (sender is Button b && (QuantityEntry.Text?.Length ?? 0) < 6)
            QuantityEntry.Text += b.Text;
    }

    private void ClearKeypad_Clicked(object sender, EventArgs e) => QuantityEntry.Text = string.Empty;

    private void ClearCart_Clicked(object sender, EventArgs e)
    {
        _cartItems.Clear();
        UpdateTotal();
    }

    // ===== COMPLETE SALE — open Payment popup =====
    private async void CompleteSale_Clicked(object sender, EventArgs e)
    {
        if (!_cartItems.Any()) return;

        string orderSource = OrderSourcePicker.SelectedItem?.ToString() ?? "Dine In";
        decimal total = _cartItems.Sum(c => c.Total);

        var cartData = _cartItems.Select(c => new RestaurantPaymentPage.CartItemData
        {
            RestaurantItemId = c.RestaurantItemId,
            Name = c.Name,
            Quantity = c.Quantity,
            Price = c.Price,
            Total = c.Total
        }).ToList();

        var paymentPage = new RestaurantPaymentPage(total, cartData, orderSource, App.LoggedInUserName);

        await Navigation.PushModalAsync(new NavigationPage(paymentPage)
        {
            BarBackgroundColor = Color.FromArgb("#1A1A1A"),
            BarTextColor = Colors.White
        }, false); // no animation = instant open

        // Only clear cart if payment was completed (not cancelled)
        if (RestaurantPaymentPage.LastPaymentCompleted)
        {
            _cartItems.Clear();
            UpdateTotal();
            RestaurantPaymentPage.LastPaymentCompleted = false;
        }
    }

    private async void SalesHistory_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NavigationPage(new SalesHistoryPage())
        {
            BarBackgroundColor = Color.FromArgb("#1A1A1A"),
            BarTextColor = Colors.White
        }, false);
    }

    private async void RefreshItems_Clicked(object sender, EventArgs e)
    {
        _dbContext.ChangeTracker.Clear();
        await LoadItemsAsync();
    }

    private void UpdateTotal() => TotalLabel.Text = $"Total: Rs. {_cartItems.Sum(c => c.Total):F2}";

    // ===== DELIVERY KOT AUTO-PRINT =====
    // Polls API every 5 seconds for pending PickMe/Uber orders and prints KOT automatically

    private void StartDeliveryKotPolling()
    {
        _kotPollCts?.Cancel();
        _kotPollCts = new CancellationTokenSource();
        var ct = _kotPollCts.Token;

        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await PollAndPrintDeliveryKot(ct);
                }
                catch { /* silent — API may not be running */ }

                try { await Task.Delay(5000, ct); } catch { break; }
            }
        }, ct);
    }

    private async Task PollAndPrintDeliveryKot(CancellationToken ct)
    {
        var url = $"{App.ApiBaseUrl}/api/delivery/pending-kot";
        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return;

        var json = await response.Content.ReadAsStringAsync(ct);
        var orders = JsonSerializer.Deserialize<List<DeliveryKotOrder>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (orders == null || orders.Count == 0) return;

        foreach (var order in orders)
        {
            try
            {
                PrintDeliveryKot(order);

                // Mark as printed
                await _httpClient.PostAsync(
                    $"{App.ApiBaseUrl}/api/delivery/{order.Id}/kot-done",
                    null, ct);
            }
            catch { }
        }
    }

    private void PrintDeliveryKot(DeliveryKotOrder order)
    {
        const int W = 48;
        string Separator(char c = '-') => new string(c, W);

        string printerName = App.KOTPrinterName;
        if (string.IsNullOrEmpty(printerName))
        {
            printerName = Preferences.Get("ThermalPrinterName", "");
            if (string.IsNullOrEmpty(printerName))
                printerName = RawPrinterHelper.FindThermalPrinter() ?? "";
        }
        if (string.IsNullOrEmpty(printerName)) return;

        var enc = Encoding.GetEncoding("IBM437");
        byte[] init = { 0x1B, 0x40 };
        byte[] center = { 0x1B, 0x61, 0x01 };
        byte[] left = { 0x1B, 0x61, 0x00 };
        byte[] bold_on = { 0x1B, 0x45, 0x01 };
        byte[] bold_off = { 0x1B, 0x45, 0x00 };
        byte[] dbl_on = { 0x1D, 0x21, 0x11 };
        byte[] dbl_off = { 0x1D, 0x21, 0x00 };
        byte[] feedCut = { 0x0A, 0x0A, 0x0A, 0x1D, 0x56, 0x41, 0x03 };

        using var ms = new MemoryStream();
        void Emit(byte[] b) => ms.Write(b, 0, b.Length);

        Emit(init);
        Emit(center);
        Emit(dbl_on);
        Emit(enc.GetBytes("** KOT **\n"));
        Emit(dbl_off);
        Emit(bold_on);
        Emit(enc.GetBytes($"[ {order.PlatformName?.ToUpper()} ORDER ]\n"));
        Emit(bold_off);
        Emit(enc.GetBytes(Separator('=') + "\n"));

        Emit(left);
        Emit(enc.GetBytes($"Order: {order.PlatformOrderId}\n"));
        Emit(enc.GetBytes($"Time:  {order.ReceivedAt:HH:mm dd/MM}\n"));
        if (!string.IsNullOrEmpty(order.DeliveryMode))
            Emit(enc.GetBytes($"Mode:  {order.DeliveryMode}\n"));
        if (!string.IsNullOrEmpty(order.CustomerPhone))
            Emit(enc.GetBytes($"Phone: {order.CustomerPhone}\n"));
        Emit(enc.GetBytes(Separator() + "\n\n"));

        Emit(bold_on);
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                Emit(enc.GetBytes($"{item.Quantity}x  {item.ItemName}\n"));
                if (!string.IsNullOrEmpty(item.Options))
                    Emit(enc.GetBytes($"       [{item.Options}]\n"));
                if (!string.IsNullOrEmpty(item.SpecialInstructions))
                    Emit(enc.GetBytes($"       *{item.SpecialInstructions}\n"));
            }
        }
        Emit(bold_off);

        if (!string.IsNullOrEmpty(order.DeliveryNote))
        {
            Emit(enc.GetBytes("\n"));
            Emit(bold_on);
            Emit(enc.GetBytes($"NOTE: {order.DeliveryNote}\n"));
            Emit(bold_off);
        }

        Emit(enc.GetBytes("\n" + Separator() + "\n"));
        Emit(center);
        int totalItems = order.Items?.Sum(i => i.Quantity) ?? 0;
        Emit(enc.GetBytes($"Items: {totalItems}\n"));
        Emit(feedCut);

        RawPrinterHelper.SendBytesToPrinter(printerName, ms.ToArray());
    }

    // DTO classes for delivery KOT API response
    private class DeliveryKotOrder
    {
        public int Id { get; set; }
        public string? PlatformName { get; set; }
        public string? PlatformOrderId { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? DeliveryMode { get; set; }
        public string? DeliveryNote { get; set; }
        public decimal OrderTotal { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime ReceivedAt { get; set; }
        public List<DeliveryKotItem>? Items { get; set; }
    }

    private class DeliveryKotItem
    {
        public string? ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerItem { get; set; }
        public decimal TotalPrice { get; set; }
        public string? SpecialInstructions { get; set; }
        public string? Options { get; set; }
        public string? ItemType { get; set; }
    }

    public class CartItem
    {
        public int RestaurantItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    public class ItemViewModel
    {
        public int RestaurantItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int RestaurantCategoryId { get; set; }
    }

    // ===== Sales History Page =====
    public class SalesHistoryPage : ContentPage
    {
        public SalesHistoryPage()
        {
            Title = "Restaurant Sales History";
            BackgroundColor = Color.FromArgb("#1A1A1A");

            var db = new StockDbContext();
            var sales = db.RestaurantSales
                .Include(s => s.Items)
                .OrderByDescending(s => s.DateTime)
                .Take(50)
                .ToList();

            var listView = new CollectionView
            {
                ItemsSource = sales,
                ItemTemplate = new DataTemplate(() =>
                {
                    var frame = new Frame
                    {
                        Padding = 12, Margin = new Thickness(0, 4),
                        BackgroundColor = Color.FromArgb("#2A2A2A"),
                        CornerRadius = 10, BorderColor = Color.FromArgb("#404040")
                    };

                    var grid = new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                        RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                        RowSpacing = 2
                    };

                    var inv = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
                    inv.SetBinding(Label.TextProperty, "InvoiceNumber");

                    var date = new Label { FontSize = 12, TextColor = Color.FromArgb("#AAAAAA") };
                    date.SetBinding(Label.TextProperty, new Binding("DateTime", stringFormat: "{0:dd MMM yyyy, hh:mm tt}"));

                    var source = new Label { FontSize = 12, TextColor = Color.FromArgb("#888888") };
                    source.SetBinding(Label.TextProperty, new Binding("OrderSource", stringFormat: "Source: {0}"));

                    var total = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFEB3B"), HorizontalTextAlignment = TextAlignment.End };
                    total.SetBinding(Label.TextProperty, new Binding("TotalAmount", stringFormat: "Rs. {0:N2}"));

                    grid.Add(inv, 0, 0);
                    grid.Add(date, 0, 1);
                    grid.Add(source, 0, 2);
                    grid.Add(total, 1, 0);

                    frame.Content = grid;
                    return frame;
                })
            };

            var closeBtn = new Button
            {
                Text = "Close", BackgroundColor = Color.FromArgb("#757575"),
                TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold,
                HeightRequest = 48, CornerRadius = 10
            };
            closeBtn.Clicked += async (s, e) => await Navigation.PopModalAsync();

            var mainGrid = new Grid
            {
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
                Padding = 20, RowSpacing = 12,
            };
            mainGrid.Add(new Label { Text = "Restaurant Sales History", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White }, 0, 0);
            Grid.SetRow(listView, 1);
            Grid.SetRow(closeBtn, 2);
            mainGrid.Children.Add(listView);
            mainGrid.Children.Add(closeBtn);

            Content = mainGrid;
        }
    }
}
