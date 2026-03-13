using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class GRNReportPage : ContentPage
{
    private List<GRNViewModel> _allGRNs = new();

    public GRNReportPage()
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

            var grns = await Task.Run(() =>
                db.GRNs.Include(g => g.Items).ThenInclude(i => i.MenuItem)
                    .Where(g => g.CreatedAt >= from && g.CreatedAt < to)
                    .OrderByDescending(g => g.CreatedAt)
                    .ToList());

            int totalItems = grns.Sum(g => g.Items.Count);
            int totalQty = grns.Sum(g => g.Items.Sum(i => i.Quantity));

            TotalGRNLabel.Text = grns.Count.ToString();
            TotalItemsLabel.Text = totalItems.ToString();
            TotalQtyLabel.Text = totalQty.ToString();

            var viewModels = grns.Select(g => new GRNViewModel
            {
                GRNNumber = g.GRNNumber,
                DateStr = g.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                ItemsSummary = string.Join(", ",
                    g.Items.Select(i => $"{i.MenuItem?.Name ?? "?"} x{i.Quantity}")),
                TotalQty = g.Items.Sum(i => i.Quantity)
            }).ToList();

            _allGRNs = viewModels;
            GRNView.ItemsSource = new ObservableCollection<GRNViewModel>(_allGRNs);
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
            GRNView.ItemsSource = new ObservableCollection<GRNViewModel>(_allGRNs);
            return;
        }

        var filtered = _allGRNs
            .Where(g => g.GRNNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || g.ItemsSummary.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        GRNView.ItemsSource = new ObservableCollection<GRNViewModel>(filtered);
    }

    public class GRNViewModel
    {
        public string GRNNumber { get; set; } = "";
        public string DateStr { get; set; } = "";
        public string ItemsSummary { get; set; } = "";
        public int TotalQty { get; set; }
    }
}
