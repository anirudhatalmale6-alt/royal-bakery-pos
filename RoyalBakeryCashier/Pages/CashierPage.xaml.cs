using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace RoyalBakeryCashier.Pages
{
    public partial class CashierPage : ContentPage
    {
        private readonly StockDbContext _dbContext;
        private List<ItemViewModel> _allItems;
        private ObservableCollection<CartItem> _cartItems;

        private bool _loaded = false;
        private int? _loadedSalesOrderId = null; // Track loaded SalesOrder for completion

        public CashierPage()
        {
            InitializeComponent();
            _dbContext = new StockDbContext();
            _cartItems = new ObservableCollection<CartItem>();
            CartCollectionView.ItemsSource = _cartItems;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_loaded)
            {
                _loaded = true;
                try
                {
                    await Task.Run(() =>
                    {
                        _dbContext.Database.EnsureCreated();

                        // Only run full migrations + seed on first-ever startup
                        bool alreadySeeded = false;
                        try { alreadySeeded = _dbContext.MenuCategories.Any(); } catch { }

                        if (!alreadySeeded)
                            _dbContext.ApplyMigrations(); // creates tables + seeds 195 items
                        else
                        {
                            // Quick schema patch only (no seed)
                            try
                            {
                                _dbContext.Database.ExecuteSqlRaw(
                                    @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MenuItems') AND name = 'QuickCategory')
                                      ALTER TABLE MenuItems ADD QuickCategory INT NOT NULL DEFAULT 0;");
                            }
                            catch { }
                        }
                    });
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Database Error",
                        $"Could not connect to SQL Server.\n\nMake sure SQL Server is running and the database exists.\n\nError: {ex.Message}",
                        "OK");
                    return;
                }
            }

            // Always reload items and clear cart when page reappears (e.g., after payment modal closes)
            _cartItems.Clear(); // ObservableCollection auto-notifies UI
            LoadItems();
            UpdateTotal();

            // Keep focus on the sales order entry for scanner input
            // Delay focus slightly so MAUI layout is complete
            await Task.Delay(100);
            SalesOrderEntry.Text = string.Empty; // clear any previous scan
            SalesOrderEntry.Focus();
        }

        // Category color palette matching reference design
        private static readonly Color[] _categoryColors = new[]
        {
            Color.FromArgb("#607D8B"), // All - grey
            Color.FromArgb("#2196F3"), // Bread - blue
            Color.FromArgb("#9C27B0"), // Pastries - purple
            Color.FromArgb("#FF9800"), // Cakes - orange
            Color.FromArgb("#4CAF50"), // Drinks - green
            Color.FromArgb("#F44336"), // Specials - red
        };

        private const int QUICK1_CATEGORY_ID = -1;
        private const int QUICK2_CATEGORY_ID = -2;

        private void LoadCategories()
        {
            var categories = _dbContext.MenuCategories.ToList();
            CategoryGrid.Children.Clear();
            CategoryGrid.RowDefinitions.Clear();

            // Fixed first: Quicks 1, Quicks 2, All — then real categories
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
                if (catId == QUICK1_CATEGORY_ID) bgColor = Color.FromArgb("#E91E63"); // pink
                else if (catId == QUICK2_CATEGORY_ID) bgColor = Color.FromArgb("#4CAF50"); // green
                else if (catId == null) bgColor = Color.FromArgb("#607D8B"); // grey (All)
                else bgColor = _categoryColors[i % _categoryColors.Length];

                var btn = CreateCategoryButton(name, catId, bgColor);
                Grid.SetRow(btn, i / cols);
                Grid.SetColumn(btn, i % cols);
                CategoryGrid.Children.Add(btn);
            }
        }

        private Button CreateCategoryButton(string name, int? categoryId, Color bgColor)
        {
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
            btn.Clicked += (s, e) => FilterItems(categoryId);
            return btn;
        }

        private async void GoToClearance_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("ClearStock");
        }

        // ===== DAILY SALES REPORT =====
        private async void DailyReport_Clicked(object sender, EventArgs e)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var todaySales = _dbContext.Sales
                    .Include(s => s.Items)
                    .Where(s => s.DateTime >= today && s.DateTime < tomorrow)
                    .ToList();

                int invoiceCount = todaySales.Count;
                decimal totalRevenue = todaySales.Sum(s => s.TotalAmount);
                decimal totalCash = todaySales.Sum(s => s.CashAmount);
                decimal totalCard = todaySales.Sum(s => s.CardAmount);
                decimal totalChange = todaySales.Sum(s => s.ChangeGiven);

                // Items sold breakdown
                var itemsSold = todaySales
                    .SelectMany(s => s.Items)
                    .GroupBy(si => si.ItemName)
                    .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity), Total = g.Sum(x => x.TotalPrice) })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine($"Date: {today:dd MMM yyyy}");
                sb.AppendLine($"Invoices: {invoiceCount}");
                sb.AppendLine($"Total Revenue: Rs. {totalRevenue:N2}");
                sb.AppendLine();
                sb.AppendLine("--- Payment Summary ---");
                sb.AppendLine($"Cash Received: Rs. {totalCash:N2}");
                sb.AppendLine($"Card Payments: Rs. {totalCard:N2}");
                sb.AppendLine($"Change Given: Rs. {totalChange:N2}");
                sb.AppendLine($"Net Collected: Rs. {(totalCash + totalCard - totalChange):N2}");
                sb.AppendLine();
                sb.AppendLine("--- Items Sold ---");
                foreach (var item in itemsSold.Take(30))
                {
                    sb.AppendLine($"{item.Qty}x {item.Name} = Rs. {item.Total:N2}");
                }
                if (itemsSold.Count > 30)
                    sb.AppendLine($"... and {itemsSold.Count - 30} more items");
                sb.AppendLine();
                sb.AppendLine($"Total Items: {itemsSold.Sum(x => x.Qty)}");

                await DisplayAlert("Daily Sales Report", sb.ToString(), "Close");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not generate report: {ex.Message}", "OK");
            }
        }

        private async void SalesHistory_Clicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushModalAsync(new NavigationPage(new SalesHistoryPage())
                {
                    BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                    BarTextColor = Colors.White
                });
            }
            catch (Exception ex)
            {
                App.LogCrash("SalesHistory_Clicked", ex);
                await DisplayAlert("Error", $"Could not open Sales History.\n\n{ex.Message}", "OK");
            }
        }

        // ===== SALES ORDER LOADING =====

        // Timer for barcode scanner auto-load: scanners type fast then press Enter,
        // but we also auto-load after a short delay in case Enter isn't sent
        private CancellationTokenSource? _scanDebounce;

        private void SalesOrderEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Barcode scanners type characters rapidly then may or may not send Enter.
            // Debounce: if text stops changing for 400ms and length >= 3, auto-load.
            _scanDebounce?.Cancel();
            var text = (e.NewTextValue ?? "").Trim();
            if (text.Length < 3) return;

            _scanDebounce = new CancellationTokenSource();
            var token = _scanDebounce.Token;
            Task.Delay(400, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    MainThread.BeginInvokeOnMainThread(() => LoadSalesOrderFromSearch());
            });
        }

        private void SalesOrderEntry_Completed(object sender, EventArgs e)
        {
            _scanDebounce?.Cancel(); // Cancel debounce since Enter was pressed
            LoadSalesOrderFromSearch();
        }

        private void LoadSalesOrder_Clicked(object sender, EventArgs e)
        {
            _scanDebounce?.Cancel();
            LoadSalesOrderFromSearch();
        }

        private async void LoadSalesOrderFromSearch()
        {
            var searchText = (SalesOrderEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(searchText)) return; // silent — no popup for empty

            // Always clear the entry first (erase previous QR code)
            SalesOrderEntry.Text = string.Empty;

            // Clear EF cache so we always get fresh status from DB
            _dbContext.ChangeTracker.Clear();

            // Search by exact SalesOrderNumber or by Id
            var salesOrder = _dbContext.SalesOrders
                .Include(so => so.Items)
                .ThenInclude(i => i.MenuItem)
                .FirstOrDefault(so => so.SalesOrderNumber == searchText
                    || so.Id.ToString() == searchText);

            if (salesOrder == null)
            {
                // Silent — don't show popup for not found
                SalesOrderEntry.Focus();
                return;
            }

            if (salesOrder.Status == 1)
            {
                // KEEP this popup — client specifically requested it
                await DisplayAlert("Bill Already Completed",
                    $"Sales Order {salesOrder.SalesOrderNumber} has already been billed and completed.", "OK");
                SalesOrderEntry.Focus();
                return;
            }

            if (salesOrder.Status == 2)
            {
                await DisplayAlert("Cancelled", $"Sales Order {salesOrder.SalesOrderNumber} was cancelled.", "OK");
                SalesOrderEntry.Focus();
                return;
            }

            // Clear current cart and load sales order items
            _cartItems.Clear();
            LoadItems(); // refresh stock

            foreach (var item in salesOrder.Items)
            {
                var menuItem = _allItems.FirstOrDefault(x => x.MenuItemId == item.MenuItemId);
                if (menuItem == null) continue;

                int qtyToAdd = item.Quantity;
                if (qtyToAdd > menuItem.AvailableStock)
                    qtyToAdd = menuItem.AvailableStock; // silently cap to available
                if (qtyToAdd <= 0) continue;

                _cartItems.Add(new CartItem
                {
                    MenuItemId = item.MenuItemId,
                    Name = menuItem.Name,
                    Quantity = qtyToAdd,
                    Price = item.PricePerItem,
                    Total = qtyToAdd * item.PricePerItem
                });
                menuItem.AvailableStock -= qtyToAdd;
            }

            _loadedSalesOrderId = salesOrder.Id;
            UpdateTotal();
            SalesOrderEntry.Focus(); // Keep focus on scanner entry
        }

        private void LoadItems()
        {
            _allItems = _dbContext.Stocks
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
                .ToList();

            ItemsCollectionView.ItemsSource = new ObservableCollection<ItemViewModel>(_allItems);
        }

        private int? _currentCategoryId = null;

        private void FilterItems(int? categoryId)
        {
            _currentCategoryId = categoryId;
            ItemSearchEntry.Text = string.Empty; // clear search when switching categories

            // In-memory filtering — no DB query
            IEnumerable<ItemViewModel> filtered = _allItems;
            if (categoryId == QUICK1_CATEGORY_ID)
                filtered = _allItems.Where(i => i.QuickCategory == 1 || i.IsQuick);
            else if (categoryId == QUICK2_CATEGORY_ID)
                filtered = _allItems.Where(i => i.QuickCategory == 2);
            else if (categoryId != null)
                filtered = _allItems.Where(i => i.MenuCategoryId == categoryId);

            ItemsCollectionView.ItemsSource = new ObservableCollection<ItemViewModel>(filtered);
        }

        private void ItemSearchEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = (e.NewTextValue ?? "").Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                // Show current category when search is cleared
                FilterItemsWithoutClearingSearch(_currentCategoryId);
                return;
            }

            // Search across ALL items regardless of category
            var filtered = _allItems
                .Where(i => i.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ItemsCollectionView.ItemsSource = new ObservableCollection<ItemViewModel>(filtered);
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

            ItemsCollectionView.ItemsSource = new ObservableCollection<ItemViewModel>(filtered);
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
                SalesOrderEntry.Focus();
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

        private void Keypad_Clicked(object sender, EventArgs e)
        {
            if (sender is Button b && (QuantityEntry.Text?.Length ?? 0) < 6)
                QuantityEntry.Text += b.Text;
        }

        private void ClearKeypad_Clicked(object sender, EventArgs e) => QuantityEntry.Text = string.Empty;

        private void ClearCart_Clicked(object sender, EventArgs e)
        {
            foreach (var ci in _cartItems)
            {
                var item = _allItems.FirstOrDefault(x => x.MenuItemId == ci.MenuItemId);
                if (item != null) item.AvailableStock += ci.Quantity;
            }
            _cartItems.Clear(); // ObservableCollection auto-notifies UI
            _loadedSalesOrderId = null;
            UpdateTotal();
            SalesOrderEntry.Focus();
        }

        private async void PlaceOrder_Clicked(object sender, EventArgs e)
        {
            if (!_cartItems.Any()) return; // silent — no popup for empty cart

            try
            {
                var order = new Order
                {
                    DateTime = DateTime.Now,
                    Status = 0,
                    TotalAmount = _cartItems.Sum(c => c.Total),
                    Items = _cartItems.Select(c => new OrderItem
                    {
                        MenuItemId = c.MenuItemId,
                        Quantity = c.Quantity,
                        PricePerItem = c.Price,
                        TotalPrice = c.Total,
                        MenuItem = null
                    }).ToList()
                };

                _dbContext.Orders.Add(order);
                await _dbContext.SaveChangesAsync();

                await Navigation.PushModalAsync(new NavigationPage(new PaymentPage(order.Id, _loadedSalesOrderId))
                {
                    BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                    BarTextColor = Colors.White
                });
                _loadedSalesOrderId = null;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not place order.\n\n{ex.InnerException?.Message ?? ex.Message}", "OK");
            }
        }

        private async void CartItemName_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is CartItem item)
            {
                bool confirm = await DisplayAlert("Remove Item",
                    $"Remove \"{item.Name}\" from cart?", "Delete", "Cancel");

                if (confirm)
                {
                    var menuItem = _allItems.FirstOrDefault(x => x.MenuItemId == item.MenuItemId);
                    if (menuItem != null)
                        menuItem.AvailableStock += item.Quantity;

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

        private void UpdateTotal() => TotalLabel.Text = $"Total: Rs. {_cartItems.Sum(c => c.Total):F2}";

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
    }
}