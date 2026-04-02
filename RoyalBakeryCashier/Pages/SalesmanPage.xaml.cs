using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using RoyalBakeryCashier.Helpers;
using System.Collections.ObjectModel;
using System.Text;

namespace RoyalBakeryCashier.Pages;

public partial class SalesmanPage : ContentPage
{
    private readonly StockDbContext _dbContext;
    private List<ItemViewModel> _allItems;
    private ObservableCollection<CartItem> _cartItems;

    private bool _loaded = false;
    private CancellationTokenSource? _searchDebounce;

    public SalesmanPage()
    {
        InitializeComponent();
        _dbContext = new StockDbContext();
        _cartItems = new ObservableCollection<CartItem>();
        CartCollectionView.ItemsSource = _cartItems;
        Title = $"{App.TerminalName} Terminal";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        try
        {
            // Salesman connects to remote DB — skip EnsureCreated/ApplyMigrations
            // (database is created by the API/Cashier on the server machine)
            await Task.Run(() =>
            {
                // Just ensure QuickCategory column exists (safe schema patch)
                try { _dbContext.Database.ExecuteSqlRaw(
                    @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MenuItems') AND name = 'QuickCategory')
                      ALTER TABLE MenuItems ADD QuickCategory INT NOT NULL DEFAULT 0;"); } catch { }
            });
            await LoadCategoriesAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Database Error",
                $"Could not connect to SQL Server.\n\nMake sure SQL Server is running and the database exists.\n\nError: {ex.Message}",
                "OK");
        }
    }

    // Category colors (same as cashier)
    private static readonly Color[] _categoryColors = new[]
    {
        Color.FromArgb("#607D8B"),
        Color.FromArgb("#2196F3"),
        Color.FromArgb("#9C27B0"),
        Color.FromArgb("#FF9800"),
        Color.FromArgb("#4CAF50"),
        Color.FromArgb("#F44336"),
    };

    private const int QUICK1_CATEGORY_ID = -1;
    private const int QUICK2_CATEGORY_ID = -2;

    private async Task LoadCategoriesAsync()
    {
        var categories = await Task.Run(() => _dbContext.MenuCategories.AsNoTracking().ToList());
        CategoryGrid.Children.Clear();
        CategoryGrid.RowDefinitions.Clear();

        var allButtons = new List<(string Name, int? CatId)>
        {
            ("Quicks 1", QUICK1_CATEGORY_ID),
            ("Quicks 2", QUICK2_CATEGORY_ID),
            ("All", null)
        };
        foreach (var cat in categories)
            allButtons.Add((cat.Name, cat.Id));

        int cols = 6;
        int rows = (int)Math.Ceiling(allButtons.Count / (double)cols);
        for (int r = 0; r < rows; r++)
            CategoryGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < allButtons.Count; i++)
        {
            var (name, catId) = allButtons[i];
            Color bgColor;
            if (catId == QUICK1_CATEGORY_ID) bgColor = Color.FromArgb("#E91E63");
            else if (catId == QUICK2_CATEGORY_ID) bgColor = Color.FromArgb("#4CAF50");
            else if (catId == null) bgColor = Color.FromArgb("#607D8B");
            else bgColor = _categoryColors[i % _categoryColors.Length];

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
            _dbContext.Stocks
                .AsNoTracking()
                .Include(s => s.MenuItem)
                .Select(s => new ItemViewModel
                {
                    MenuItemId = s.MenuItemId,
                    Name = s.MenuItem.Name,
                    Price = s.MenuItem.Price,
                    AvailableStock = s.Quantity,
                    MenuCategoryId = s.MenuItem.MenuCategoryId,
                    QuickCategory = s.MenuItem.QuickCategory,
                    IsQuick = s.MenuItem.IsQuick
                })
                .ToList());

        _allItems = items;
        ItemsCollectionView.ItemsSource = _allItems;
    }

    private int? _currentCategoryId = null;

    private void FilterItems(int? categoryId)
    {
        _currentCategoryId = categoryId;
        ItemSearchEntry.Text = string.Empty;

        IEnumerable<ItemViewModel> filtered = _allItems;
        if (categoryId == QUICK1_CATEGORY_ID)
            filtered = _allItems.Where(i => i.QuickCategory == 1 || i.IsQuick);
        else if (categoryId == QUICK2_CATEGORY_ID)
            filtered = _allItems.Where(i => i.QuickCategory == 2);
        else if (categoryId != null)
            filtered = _allItems.Where(i => i.MenuCategoryId == categoryId);

        ItemsCollectionView.ItemsSource = filtered.ToList();
    }

    private void ItemSearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce?.Cancel();
        var keyword = (e.NewTextValue ?? "").Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            FilterItemsWithoutClearingSearch(_currentCategoryId);
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
                    ItemsCollectionView.ItemsSource = filtered.ToList();
                });
        });
    }

    private void FilterItemsWithoutClearingSearch(int? categoryId)
    {
        IEnumerable<ItemViewModel> filtered = _allItems;
        if (categoryId == QUICK1_CATEGORY_ID)
            filtered = _allItems.Where(i => i.QuickCategory == 1 || i.IsQuick);
        else if (categoryId == QUICK2_CATEGORY_ID)
            filtered = _allItems.Where(i => i.QuickCategory == 2);
        else if (categoryId != null)
            filtered = _allItems.Where(i => i.MenuCategoryId == categoryId);

        ItemsCollectionView.ItemsSource = filtered.ToList();
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

    private async void AddToCart(ItemViewModel menuItem, int qty)
    {
        if (qty <= 0) return;

        int available = menuItem.AvailableStock;
        if (available <= 0)
        {
            await DisplayAlert("Out of Stock",
                $"\"{menuItem.Name}\" has no stock available.", "OK");
            return;
        }
        if (qty > available) qty = available;

        var existing = _cartItems.FirstOrDefault(c => c.MenuItemId == menuItem.MenuItemId);
        if (existing != null)
        {
            int canAdd = Math.Min(qty, available);
            int newQty = existing.Quantity + canAdd;
            menuItem.AvailableStock -= canAdd;
            int idx = _cartItems.IndexOf(existing);
            _cartItems[idx] = new CartItem
            {
                MenuItemId = existing.MenuItemId,
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
                MenuItemId = menuItem.MenuItemId,
                Name = menuItem.Name,
                Quantity = qty,
                Price = menuItem.Price,
                Total = qty * menuItem.Price
            });
            menuItem.AvailableStock -= qty;
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
                var menuItem = _allItems.FirstOrDefault(x => x.MenuItemId == item.MenuItemId);
                if (menuItem != null) menuItem.AvailableStock += item.Quantity;
                _cartItems.Remove(item); // ObservableCollection auto-notifies UI
                UpdateTotal();
            }
        }
    }

    private void DecreaseQuantity_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CartItem item)
        {
            var menuItem = _allItems.FirstOrDefault(x => x.MenuItemId == item.MenuItemId);
            if (menuItem != null) menuItem.AvailableStock++;

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
                        MenuItemId = item.MenuItemId,
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
            var menuItem = _allItems.FirstOrDefault(x => x.MenuItemId == item.MenuItemId);
            if (menuItem != null && menuItem.AvailableStock > 0)
            {
                int newQty = item.Quantity + 1;
                menuItem.AvailableStock--;
                int idx = _cartItems.IndexOf(item);
                if (idx >= 0)
                    _cartItems[idx] = new CartItem
                    {
                        MenuItemId = item.MenuItemId,
                        Name = item.Name,
                        Quantity = newQty,
                        Price = item.Price,
                        Total = newQty * item.Price
                    };
            }
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
        // Restore stock for all cart items in-memory (no DB hit)
        foreach (var ci in _cartItems)
        {
            var item = _allItems.FirstOrDefault(x => x.MenuItemId == ci.MenuItemId);
            if (item != null) item.AvailableStock += ci.Quantity;
        }
        _cartItems.Clear(); // ObservableCollection auto-notifies UI
        UpdateTotal();
    }

    // ===== CREATE SALES ORDER =====
    private async void CreateOrder_Clicked(object sender, EventArgs e)
    {
        if (!_cartItems.Any()) return; // silent — no popup for empty cart

        // If editing an existing order, cancel the old one first
        if (_editingSalesOrderId.HasValue)
        {
            await Task.Run(() =>
            {
                var oldOrder = _dbContext.SalesOrders.FirstOrDefault(so => so.Id == _editingSalesOrderId.Value);
                if (oldOrder != null && oldOrder.Status == 0) // Only cancel if still Pending
                {
                    oldOrder.Status = 2; // Cancelled
                    _dbContext.SaveChanges();
                }
            });
            _editingSalesOrderId = null;
        }

        // Generate sales order number (async to avoid UI freeze on Celeron)
        var lastOrder = await Task.Run(() =>
            _dbContext.SalesOrders.OrderByDescending(so => so.Id).FirstOrDefault());
        int nextNum = (lastOrder?.Id ?? 0) + 1;
        string orderNumber = $"SO-{nextNum:D5}";

        var salesOrder = new SalesOrder
        {
            SalesOrderNumber = orderNumber,
            CreatedAt = DateTime.Now,
            TotalAmount = _cartItems.Sum(c => c.Total),
            Status = 0, // Pending
            TerminalName = App.TerminalName,
            CustomerName = string.IsNullOrWhiteSpace(CustomerNameEntry.Text)
                ? null : CustomerNameEntry.Text.Trim(),
            Items = _cartItems.Select(c => new SalesOrderItem
            {
                MenuItemId = c.MenuItemId,
                Quantity = c.Quantity,
                PricePerItem = c.Price,
                TotalPrice = c.Total
            }).ToList()
        };

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            _dbContext.SalesOrders.Add(salesOrder);
            await _dbContext.SaveChangesAsync();
        });

        // Build item name lookup from cart (avoids per-item DB queries during print)
        var itemNames = _cartItems.ToDictionary(c => c.MenuItemId, c => c.Name);

        // Print sales order slip with QR code
        await PrintOrderSlip(salesOrder, itemNames);

        // Silent after order — no popup, just clear and ready for next order
        _cartItems.Clear(); // ObservableCollection auto-notifies UI
        CustomerNameEntry.Text = string.Empty;
        UpdateTotal();
    }

    private async Task PrintOrderSlip(SalesOrder order, Dictionary<int, string> itemNames = null)
    {
        const int W = 48;
        string Separator(char c = '-') => new string(c, W);
        string Row(string l, string r) => l + r.PadLeft(W - l.Length);

        // Save locally as backup
        try
        {
            string dir = Path.Combine(FileSystem.AppDataDirectory, "orders");
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(
                Path.Combine(dir, $"order_{DateTime.Now:yyyyMMdd_HHmmss}.txt"),
                $"Order: {order.SalesOrderNumber} | Total: Rs. {order.TotalAmount:N2}");
        }
        catch { }

        try
        {
            // Find printer: saved preference or auto-detect
            string printerName = Preferences.Get("ThermalPrinterName", "");
            if (string.IsNullOrEmpty(printerName))
            {
                printerName = RawPrinterHelper.FindThermalPrinter() ?? "";
                if (!string.IsNullOrEmpty(printerName))
                    Preferences.Set("ThermalPrinterName", printerName);
            }

            if (string.IsNullOrEmpty(printerName))
            {
                // Printer not found — silent (popup commented out as requested)
                // var printers = RawPrinterHelper.GetInstalledPrinters();
                // string msg = printers.Count > 0
                //     ? $"No thermal printer detected.\n\nInstalled printers:\n{string.Join("\n", printers)}"
                //     : "No printers found. Please install the Epson TM-T82 driver.";
                // await DisplayAlert("Printer Not Found", msg, "OK");
                return;
            }

            var enc = Encoding.GetEncoding("IBM437");
            byte[] init = { 0x1B, 0x40 };
            byte[] center = { 0x1B, 0x61, 0x01 };
            byte[] left = { 0x1B, 0x61, 0x00 };
            byte[] feedCut = { 0x0A, 0x0A, 0x0A, 0x1D, 0x56, 0x41, 0x03 };

            using var ms = new MemoryStream();
            void Emit(byte[] b) => ms.Write(b, 0, b.Length);

            Emit(init);

            // Header — centered
            Emit(center);
            Emit(enc.GetBytes("The Royal Bakery\n"));
            Emit(enc.GetBytes("202, Galle Road, Colombo-06\n"));
            Emit(enc.GetBytes("0112 500 991 / 0114 341 642\n"));
            Emit(enc.GetBytes("www.theroyalbakery.com\n"));
            Emit(enc.GetBytes(Separator('=') + "\n"));
            Emit(enc.GetBytes("*** SALES ORDER ***\n"));
            Emit(enc.GetBytes(Separator() + "\n"));

            // Body — left aligned
            Emit(left);
            Emit(enc.GetBytes(Row("Order #:", order.SalesOrderNumber) + "\n"));
            Emit(enc.GetBytes(Row("Date:", order.CreatedAt.ToString("dd/MM/yyyy HH:mm")) + "\n"));
            Emit(enc.GetBytes(Row("Terminal:", App.TerminalName) + "\n"));
            if (!string.IsNullOrEmpty(order.CustomerName))
                Emit(enc.GetBytes(Row("Customer:", order.CustomerName) + "\n"));
            Emit(enc.GetBytes(Separator() + "\n"));

            foreach (var item in order.Items)
            {
                string itemName = (itemNames != null && itemNames.TryGetValue(item.MenuItemId, out var n)) ? n : "Item";
                Emit(enc.GetBytes(itemName + "\n"));
                Emit(enc.GetBytes(Row($" {item.Quantity} x {item.PricePerItem:N2}", $"{item.TotalPrice:N2}") + "\n"));
            }

            Emit(enc.GetBytes(Separator() + "\n"));
            Emit(enc.GetBytes(Row("TOTAL", $"Rs. {order.TotalAmount:N2}") + "\n"));
            Emit(enc.GetBytes(Separator('=') + "\n"));

            // QR code + footer — centered
            Emit(center);
            byte[] qrBytes = BuildEscPosQR(order.SalesOrderNumber);
            Emit(qrBytes);
            Emit(enc.GetBytes("\n"));
            Emit(enc.GetBytes($"[ {order.SalesOrderNumber} ]\n"));
            Emit(enc.GetBytes("Present at cashier for payment\n"));
            Emit(enc.GetBytes(Separator() + "\n"));
            Emit(enc.GetBytes("Powered by EzyCode\n"));
            Emit(enc.GetBytes("www.ezycode.lk\n"));

            Emit(feedCut);

            // Send raw bytes — silent on failure (popup commented out as requested)
            RawPrinterHelper.SendBytesToPrinter(printerName, ms.ToArray());
        }
        catch { /* silent print error */ }
    }

    /// <summary>
    /// Build native ESC/POS QR code command bytes for Epson thermal printers.
    /// Uses GS ( k commands supported by TM-T82, TM-T88, etc.
    /// </summary>
    private static byte[] BuildEscPosQR(string data)
    {
        byte[] dataBytes = Encoding.ASCII.GetBytes(data);
        int storeLen = dataBytes.Length + 3; // pL pH 31 50 30 + data
        byte pL = (byte)(storeLen & 0xFF);
        byte pH = (byte)((storeLen >> 8) & 0xFF);

        using var ms = new MemoryStream();

        // 1. Select model (Model 2)
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 });

        // 2. Set module size (6 = medium, good for scanning)
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x06 });

        // 3. Set error correction level (L = 48, M = 49, Q = 50, H = 51)
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31 });

        // 4. Store QR data
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30 });
        ms.Write(dataBytes);

        // 5. Print QR code
        ms.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });

        return ms.ToArray();
    }

    private async void OrderHistory_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NavigationPage(new OrderHistoryPage(LoadOrderForEdit))
        {
            BarBackgroundColor = Color.FromArgb("#1A1A1A"),
            BarTextColor = Colors.White
        }, false);
    }

    private int? _editingSalesOrderId = null;

    private void LoadOrderForEdit(SalesOrder order)
    {
        // Clear current cart (restore stock)
        foreach (var ci in _cartItems)
        {
            var mi = _allItems.FirstOrDefault(x => x.MenuItemId == ci.MenuItemId);
            if (mi != null) mi.AvailableStock += ci.Quantity;
        }
        _cartItems.Clear();

        // Load order items into cart
        foreach (var item in order.Items)
        {
            var menuItem = _allItems.FirstOrDefault(x => x.MenuItemId == item.MenuItemId);
            if (menuItem == null) continue;

            _cartItems.Add(new CartItem
            {
                MenuItemId = item.MenuItemId,
                Name = menuItem.Name,
                Quantity = item.Quantity,
                Price = item.PricePerItem,
                Total = item.TotalPrice
            });
            menuItem.AvailableStock -= item.Quantity;
        }

        _editingSalesOrderId = order.Id;
        CustomerNameEntry.Text = order.CustomerName ?? "";
        UpdateTotal();
    }

    private async void RefreshItems_Clicked(object sender, EventArgs e)
    {
        _dbContext.ChangeTracker.Clear();
        await LoadItemsAsync();
    }

    private void UpdateTotal() => TotalLabel.Text = $"Total: Rs. {_cartItems.Sum(c => c.Total):F2}";

    // ===== View Models =====
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    public class ItemViewModel
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableStock { get; set; }
        public int MenuCategoryId { get; set; }
        public int QuickCategory { get; set; }
        public bool IsQuick { get; set; }
    }

    // ===== ORDER HISTORY PAGE =====
    public class OrderHistoryPage : ContentPage
    {
        private readonly Action<SalesOrder>? _onEditOrder;
        private CollectionView _listView;

        public OrderHistoryPage(Action<SalesOrder>? onEditOrder = null)
        {
            _onEditOrder = onEditOrder;
            Title = "Order History";
            BackgroundColor = Color.FromArgb("#1A1A1A");

            // Load orders on background thread — don't block UI
            var orders = Task.Run(() =>
            {
                using var db = new StockDbContext();
                return db.SalesOrders
                    .AsNoTracking()
                    .Include(so => so.Items)
                    .OrderByDescending(so => so.CreatedAt)
                    .Take(50)
                    .ToList();
            }).GetAwaiter().GetResult();

            var listView = new CollectionView
            {
                ItemsSource = orders,
                ItemTemplate = new DataTemplate(() =>
                {
                    var frame = new Frame
                    {
                        Padding = 12,
                        Margin = new Thickness(0, 4),
                        BackgroundColor = Color.FromArgb("#2A2A2A"),
                        CornerRadius = 10,
                        BorderColor = Color.FromArgb("#404040")
                    };

                    var grid = new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
                        RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                        RowSpacing = 2
                    };

                    var orderNum = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
                    orderNum.SetBinding(Label.TextProperty, "SalesOrderNumber");

                    var date = new Label { FontSize = 12, TextColor = Color.FromArgb("#AAAAAA") };
                    date.SetBinding(Label.TextProperty, new Binding("CreatedAt", stringFormat: "{0:dd MMM yyyy, hh:mm tt}"));

                    var customer = new Label { FontSize = 12, TextColor = Color.FromArgb("#888888") };
                    customer.SetBinding(Label.TextProperty, new Binding("CustomerName", stringFormat: "Customer: {0}"));

                    var total = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFEB3B"), HorizontalTextAlignment = TextAlignment.End };
                    total.SetBinding(Label.TextProperty, new Binding("TotalAmount", stringFormat: "Rs. {0:N2}"));

                    var statusLabel = new Label { FontSize = 11, HorizontalTextAlignment = TextAlignment.End };
                    statusLabel.SetBinding(Label.TextProperty, new Binding("Status", converter: new StatusConverter()));
                    statusLabel.SetBinding(Label.TextColorProperty, new Binding("Status", converter: new StatusColorConverter()));

                    // Edit button — only visible for Pending orders
                    var editBtn = new Button
                    {
                        Text = "Edit",
                        FontSize = 12,
                        HeightRequest = 32,
                        WidthRequest = 60,
                        Padding = 0,
                        CornerRadius = 6,
                        BackgroundColor = Color.FromArgb("#2196F3"),
                        TextColor = Colors.White,
                        VerticalOptions = LayoutOptions.Center
                    };
                    editBtn.SetBinding(Button.CommandParameterProperty, ".");
                    editBtn.SetBinding(Button.IsVisibleProperty, new Binding("Status", converter: new PendingVisibilityConverter()));
                    editBtn.Clicked += EditOrder_Clicked;

                    grid.Add(orderNum, 0, 0);
                    grid.Add(date, 0, 1);
                    grid.Add(customer, 0, 2);
                    grid.Add(total, 1, 0);
                    grid.Add(statusLabel, 1, 1);
                    grid.Add(editBtn, 2, 0);
                    Grid.SetRowSpan(editBtn, 2);

                    frame.Content = grid;
                    return frame;
                })
            };

            var closeBtn = new Button
            {
                Text = "Close",
                BackgroundColor = Color.FromArgb("#757575"),
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 48,
                CornerRadius = 10
            };
            closeBtn.Clicked += async (s, e) => await Navigation.PopModalAsync();

            Content = new Grid
            {
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
                Padding = 20,
                RowSpacing = 12,
                Children =
                {
                    new Label { Text = "Sales Order History", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                }
            };

            var mainGrid = (Grid)Content;
            Grid.SetRow(listView, 1);
            Grid.SetRow(closeBtn, 2);
            mainGrid.Children.Add(listView);
            mainGrid.Children.Add(closeBtn);
        }

        private async void EditOrder_Clicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is SalesOrder order)
            {
                if (order.Status != 0)
                {
                    await DisplayAlert("Cannot Edit", "Only pending orders can be edited.", "OK");
                    return;
                }

                _onEditOrder?.Invoke(order);
                await Navigation.PopModalAsync(false);
            }
        }

        private class PendingVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                => (int)value == 0; // Only visible for Pending (status 0)
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => 0;
        }

        private class StatusConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (int)value switch { 0 => "Pending", 1 => "Paid", 2 => "Cancelled", _ => "Unknown" };
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => 0;
        }

        private class StatusColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (int)value switch
                {
                    0 => Color.FromArgb("#FF9800"), // Pending - orange
                    1 => Color.FromArgb("#4CAF50"), // Paid - green
                    2 => Color.FromArgb("#F44336"), // Cancelled - red
                    _ => Colors.White
                };
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => Colors.White;
        }
    }
}
