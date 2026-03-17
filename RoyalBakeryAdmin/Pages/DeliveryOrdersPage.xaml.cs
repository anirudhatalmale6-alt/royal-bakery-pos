using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;

namespace RoyalBakeryAdmin.Pages;

public partial class DeliveryOrdersPage : ContentPage
{
    private List<DeliveryOrderViewModel> _allOrders = new();

    public DeliveryOrdersPage()
    {
        InitializeComponent();
        PlatformFilter.SelectedIndex = 0; // "All"
        FilterDate.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadOrders();
    }

    private async Task LoadOrders(string? platformFilter = null, DateTime? dateFilter = null)
    {
        try
        {
            var orders = await Task.Run(() =>
            {
                using var db = new StockDbContext();
                IQueryable<DeliveryOrder> query = db.DeliveryOrders.Include(d => d.Items);

                if (!string.IsNullOrEmpty(platformFilter) && platformFilter != "All")
                    query = query.Where(d => d.PlatformName == platformFilter);

                if (dateFilter.HasValue)
                {
                    var start = dateFilter.Value.Date;
                    var end = start.AddDays(1);
                    query = query.Where(d => d.ReceivedAt >= start && d.ReceivedAt < end);
                }

                return query
                    .OrderByDescending(d => d.ReceivedAt)
                    .Take(200)
                    .ToList();
            });

            _allOrders = orders.Select(d => new DeliveryOrderViewModel
            {
                Id = d.Id,
                PlatformName = d.PlatformName,
                PlatformOrderId = d.PlatformOrderId,
                ShortOrderId = d.PlatformOrderId.Length > 12
                    ? d.PlatformOrderId[..12] + "..."
                    : d.PlatformOrderId,
                AccountName = d.AccountName,
                DeliveryMode = d.DeliveryMode,
                PlatformStatus = d.PlatformStatus,
                OrderTotal = d.OrderTotal,
                PaymentMethod = d.PaymentMethod ?? "",
                CustomerPhone = d.CustomerPhone ?? "",
                CustomerAddress = d.CustomerAddress ?? "",
                DeliveryNote = d.DeliveryNote ?? "",
                ReceivedAt = d.ReceivedAt,
                CompletedAt = d.CompletedAt,
                KotStatus = d.KotStatus,
                KotStatusText = d.KotStatus switch { 0 => "Pending", 1 => "Printed", 2 => "Done", _ => "?" },
                RestaurantSaleId = d.RestaurantSaleId,
                BakerySaleId = d.BakerySaleId,
                ItemSummary = string.Join(", ", d.Items.Select(i => $"{i.Quantity}x {i.ItemName}")),
                Items = d.Items.ToList()
            }).ToList();

            OrdersView.ItemsSource = _allOrders;

            // Update summaries
            TotalOrdersLabel.Text = _allOrders.Count.ToString();
            TotalRevenueLabel.Text = $"Rs. {_allOrders.Sum(o => o.OrderTotal):N2}";
            PickMeCountLabel.Text = _allOrders.Count(o => o.PlatformName == "PickMe").ToString();
            UberCountLabel.Text = _allOrders.Count(o => o.PlatformName == "UberEats").ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load delivery orders: {ex.Message}", "OK");
        }
    }

    private async void Filter_Clicked(object sender, EventArgs e)
    {
        var platform = PlatformFilter.SelectedItem?.ToString();
        await LoadOrders(platform, FilterDate.Date);
    }

    private async void Today_Clicked(object sender, EventArgs e)
    {
        FilterDate.Date = DateTime.Today;
        PlatformFilter.SelectedIndex = 0;
        await LoadOrders(null, DateTime.Today);
    }

    private async void Refresh_Clicked(object sender, EventArgs e)
    {
        var platform = PlatformFilter.SelectedItem?.ToString();
        await LoadOrders(platform, FilterDate.Date);
    }

    private void OrdersView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is DeliveryOrderViewModel order)
        {
            DetailPanel.IsVisible = true;
            DetailTitle.Text = $"{order.PlatformName} Order - {order.PlatformOrderId}";

            var info = $"Date: {order.ReceivedAt:dd MMM yyyy HH:mm}\n" +
                       $"Mode: {order.DeliveryMode}    Payment: {order.PaymentMethod}\n" +
                       $"Status: {order.PlatformStatus}    KOT: {order.KotStatusText}\n" +
                       $"Account: {order.AccountName}\n";

            if (!string.IsNullOrEmpty(order.CustomerPhone))
                info += $"Phone: {order.CustomerPhone}\n";
            if (!string.IsNullOrEmpty(order.CustomerAddress))
                info += $"Address: {order.CustomerAddress}\n";
            if (!string.IsNullOrEmpty(order.DeliveryNote))
                info += $"Note: {order.DeliveryNote}\n";
            if (order.RestaurantSaleId.HasValue)
                info += $"Restaurant Sale ID: {order.RestaurantSaleId}\n";
            if (order.BakerySaleId.HasValue)
                info += $"Bakery Sale ID: {order.BakerySaleId}\n";

            DetailInfo.Text = info;

            var items = "";
            foreach (var item in order.Items)
            {
                var typeLabel = item.ItemType switch { "B" => "[Bakery]", "R" => "[Restaurant]", _ => "[Unmapped]" };
                items += $"  {item.Quantity}x {item.ItemName}  Rs.{item.TotalPrice:N2}  {typeLabel}\n";
                if (!string.IsNullOrEmpty(item.Options))
                    items += $"       Options: {item.Options}\n";
                if (!string.IsNullOrEmpty(item.SpecialInstructions))
                    items += $"       Note: {item.SpecialInstructions}\n";
            }
            items += $"\n  TOTAL: Rs. {order.OrderTotal:N2}";
            DetailItems.Text = items;

            OrdersView.SelectedItem = null;
        }
    }

    public class DeliveryOrderViewModel
    {
        public int Id { get; set; }
        public string PlatformName { get; set; } = "";
        public string PlatformOrderId { get; set; } = "";
        public string ShortOrderId { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string DeliveryMode { get; set; } = "";
        public string PlatformStatus { get; set; } = "";
        public decimal OrderTotal { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string DeliveryNote { get; set; } = "";
        public DateTime ReceivedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int KotStatus { get; set; }
        public string KotStatusText { get; set; } = "";
        public int? RestaurantSaleId { get; set; }
        public int? BakerySaleId { get; set; }
        public string ItemSummary { get; set; } = "";
        public List<DeliveryOrderItem> Items { get; set; } = new();
    }
}
