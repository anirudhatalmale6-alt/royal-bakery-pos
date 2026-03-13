using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class SalesReportPage : ContentPage
{
    private List<RoyalBakeryCashier.Data.Entities.Sale> _allSales = new();

    public SalesReportPage()
    {
        InitializeComponent();
        FromDate.Date = DateTime.Today;
        ToDate.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReport();
    }

    private async void Generate_Clicked(object sender, EventArgs e)
    {
        await LoadReport();
    }

    private async Task LoadReport()
    {
        try
        {
            var db = new StockDbContext();
            var from = FromDate.Date;
            var to = ToDate.Date.AddDays(1);

            var sales = await Task.Run(() =>
                db.Sales.Include(s => s.Items)
                    .Where(s => s.DateTime >= from && s.DateTime < to)
                    .OrderByDescending(s => s.DateTime)
                    .ToList());

            RevenueLabel.Text = $"Rs. {sales.Sum(s => s.TotalAmount):N2}";
            InvoiceCountLabel.Text = sales.Count.ToString();
            CashLabel.Text = $"Rs. {sales.Sum(s => s.CashAmount):N2}";
            CardLabel.Text = $"Rs. {sales.Sum(s => s.CardAmount):N2}";

            _allSales = sales;
            SalesView.ItemsSource = new ObservableCollection<RoyalBakeryCashier.Data.Entities.Sale>(_allSales);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = (e.NewTextValue ?? "").Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            SalesView.ItemsSource = new ObservableCollection<RoyalBakeryCashier.Data.Entities.Sale>(_allSales);
            return;
        }

        var filtered = _allSales
            .Where(s => (s.InvoiceNumber ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || (s.CashierName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        SalesView.ItemsSource = new ObservableCollection<RoyalBakeryCashier.Data.Entities.Sale>(filtered);
    }
}
