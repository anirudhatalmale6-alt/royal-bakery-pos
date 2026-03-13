using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class ClearanceReportPage : ContentPage
{
    public ClearanceReportPage()
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

            var clearances = await Task.Run(() =>
                db.Clearances.Include(c => c.MenuItem)
                    .Where(c => c.DateTime >= from && c.DateTime < to)
                    .OrderByDescending(c => c.DateTime)
                    .ToList());

            TotalClearanceLabel.Text = clearances.Count.ToString();
            TotalQtyLabel.Text = clearances.Sum(c => c.Quantity).ToString();

            var viewModels = clearances.Select(c => new ClearanceViewModel
            {
                DateStr = c.DateTime.ToString("dd/MM/yyyy HH:mm"),
                ItemName = c.MenuItem?.Name ?? "Unknown",
                Quantity = c.Quantity,
                Reason = c.Reason,
                Note = c.Note ?? ""
            }).ToList();

            ClearanceView.ItemsSource = new ObservableCollection<ClearanceViewModel>(viewModels);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public class ClearanceViewModel
    {
        public string DateStr { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public string Reason { get; set; } = "";
        public string Note { get; set; } = "";
    }
}
