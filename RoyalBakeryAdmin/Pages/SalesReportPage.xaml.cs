using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class SalesReportPage : ContentPage
{
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

            SalesView.ItemsSource = new ObservableCollection<RoyalBakeryCashier.Data.Entities.Sale>(sales);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
