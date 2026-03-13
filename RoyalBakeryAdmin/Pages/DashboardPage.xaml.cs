using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class DashboardPage : ContentPage
{
    private bool _loaded = false;

    public DashboardPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboard();
    }

    private async Task LoadDashboard()
    {
        try
        {
            var db = new StockDbContext();

            if (!_loaded)
            {
                _loaded = true;
                await Task.Run(() =>
                {
                    db.Database.EnsureCreated();
                    try { db.ApplyMigrations(); } catch { }
                });
            }

            DateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Today's Sales
            var todaySales = await Task.Run(() =>
                db.Sales.Where(s => s.DateTime >= today && s.DateTime < tomorrow).ToList());
            TodaySalesLabel.Text = $"Rs. {todaySales.Sum(s => s.TotalAmount):N2}";
            TodaySalesCountLabel.Text = $"{todaySales.Count} invoices";

            // Products & Categories
            var productCount = await Task.Run(() => db.MenuItems.Count());
            var categoryCount = await Task.Run(() => db.MenuCategories.Count());
            TotalProductsLabel.Text = productCount.ToString();
            TotalCategoriesLabel.Text = $"{categoryCount} categories";

            // Stock
            var stocks = await Task.Run(() => db.Stocks.ToList());
            StockItemsLabel.Text = stocks.Count.ToString();
            var lowStock = stocks.Count(s => s.Quantity <= 5);
            LowStockLabel.Text = $"{lowStock} low stock";

            // Today's GRN
            var todayGrns = await Task.Run(() =>
                db.GRNs.Include(g => g.Items)
                    .Where(g => g.DateTime >= today && g.DateTime < tomorrow).ToList());
            TodayGRNLabel.Text = todayGrns.Count.ToString();
            TodayGRNItemsLabel.Text = $"{todayGrns.Sum(g => g.Items.Count)} items received";

            // Today's Clearance
            var todayClearances = await Task.Run(() =>
                db.Clearances.Where(c => c.DateTime >= today && c.DateTime < tomorrow).ToList());
            TodayClearanceLabel.Text = todayClearances.Count.ToString();
            TodayClearanceQtyLabel.Text = $"{todayClearances.Sum(c => c.Quantity)} items cleared";

            // Top selling items
            var topItems = await Task.Run(() =>
                db.Sales.Include(s => s.Items)
                    .Where(s => s.DateTime >= today && s.DateTime < tomorrow)
                    .SelectMany(s => s.Items)
                    .GroupBy(si => si.ItemName)
                    .Select(g => new TopItem
                    {
                        Name = g.Key,
                        Qty = g.Sum(x => x.Quantity),
                        Total = g.Sum(x => x.TotalPrice)
                    })
                    .OrderByDescending(x => x.Total)
                    .Take(15)
                    .ToList());

            TopItemsView.ItemsSource = new ObservableCollection<TopItem>(topItems);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load dashboard: {ex.Message}", "OK");
        }
    }

    public class TopItem
    {
        public string Name { get; set; } = "";
        public int Qty { get; set; }
        public decimal Total { get; set; }
    }
}
