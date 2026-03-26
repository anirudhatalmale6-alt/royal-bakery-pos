using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class ProductsPage : ContentPage
{
    private List<ProductViewModel> _allProducts = new();

    public ProductsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProducts();
    }

    private async Task LoadProducts()
    {
        try
        {
            var db = new StockDbContext();
            var categories = await Task.Run(() => db.MenuCategories.ToList());
            var catMap = categories.ToDictionary(c => c.Id, c => c.Name);

            var items = await Task.Run(() => db.MenuItems.ToList());
            var stocks = await Task.Run(() => db.Stocks.ToList());
            var stockMap = stocks.ToDictionary(s => s.MenuItemId, s => s.Quantity);

            _allProducts = items.Select(i => new ProductViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Price = i.Price,
                MenuCategoryId = i.MenuCategoryId,
                CategoryName = catMap.TryGetValue(i.MenuCategoryId, out var cn) ? cn : "—",
                StockQty = stockMap.TryGetValue(i.Id, out var sq) ? sq : 0,
                IsQuick = i.IsQuick,
                QuickCategory = i.QuickCategory,
                QuickLabel = i.QuickCategory > 0 ? $"Q{i.QuickCategory}" : (i.IsQuick ? "Q1" : "—")
            }).OrderBy(p => p.CategoryName).ThenBy(p => p.Name).ToList();

            ProductsView.ItemsSource = new ObservableCollection<ProductViewModel>(_allProducts);
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
            ProductsView.ItemsSource = new ObservableCollection<ProductViewModel>(_allProducts);
            return;
        }

        var filtered = _allProducts
            .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || p.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ProductsView.ItemsSource = new ObservableCollection<ProductViewModel>(filtered);
    }

    private async void AddProduct_Clicked(object sender, EventArgs e)
    {
        var db = new StockDbContext();
        var categories = db.MenuCategories.OrderBy(c => c.Name).ToList();

        string name = await DisplayPromptAsync("Add Product", "Product Name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string priceStr = await DisplayPromptAsync("Add Product", "Price:", keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(priceStr, out decimal price) || price < 0) return;

        // Category selection
        string catChoice = await DisplayActionSheet("Select Category",
            "Cancel", null,
            categories.Select(c => c.Name).ToArray());
        if (string.IsNullOrEmpty(catChoice) || catChoice == "Cancel") return;
        var selectedCat = categories.FirstOrDefault(c => c.Name == catChoice);
        if (selectedCat == null) return;

        string quickChoice = await DisplayActionSheet("Quick Category", "Cancel", null, "None", "Quick 1", "Quick 2");
        int quickCat = quickChoice switch
        {
            "Quick 1" => 1,
            "Quick 2" => 2,
            _ => 0
        };

        try
        {
            var menuItem = new RoyalBakeryCashier.Data.Entities.MenuItem
            {
                Name = name.Trim(),
                Price = price,
                MenuCategoryId = selectedCat.Id,
                IsQuick = quickCat > 0,
                QuickCategory = quickCat
            };

            db.MenuItems.Add(menuItem);
            await db.SaveChangesAsync();

            // Create stock entry
            db.Stocks.Add(new Stock { MenuItemId = menuItem.Id, Quantity = 0 });
            await db.SaveChangesAsync();

            await DisplayAlert("Success", $"Product '{name}' added.", "OK");
            await LoadProducts();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void EditProduct_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ProductViewModel product)
        {
            var db = new StockDbContext();
            var item = db.MenuItems.Find(product.Id);
            if (item == null) return;

            var categories = db.MenuCategories.OrderBy(c => c.Name).ToList();

            string name = await DisplayPromptAsync("Edit Product", "Name:", initialValue: item.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            string priceStr = await DisplayPromptAsync("Edit Product", "Price:",
                initialValue: item.Price.ToString("F2"), keyboard: Keyboard.Numeric);
            if (!decimal.TryParse(priceStr, out decimal price) || price < 0) return;

            string catChoice = await DisplayActionSheet("Select Category",
                "Cancel", null,
                categories.Select(c => c.Name).ToArray());
            if (string.IsNullOrEmpty(catChoice) || catChoice == "Cancel") return;
            var selectedCat = categories.FirstOrDefault(c => c.Name == catChoice);
            if (selectedCat == null) return;

            string quickChoice = await DisplayActionSheet("Quick Category", "Cancel", null, "None", "Quick 1", "Quick 2");
            int quickCat = quickChoice switch
            {
                "Quick 1" => 1,
                "Quick 2" => 2,
                _ => 0
            };

            try
            {
                item.Name = name.Trim();
                item.Price = price;
                item.MenuCategoryId = selectedCat.Id;
                item.QuickCategory = quickCat;
                item.IsQuick = quickCat > 0;

                await db.SaveChangesAsync();
                await DisplayAlert("Success", $"Product updated.", "OK");
                await LoadProducts();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    private async void DeleteProduct_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ProductViewModel product)
        {
            bool confirm = await DisplayAlert("Delete Product",
                $"Are you sure you want to delete '{product.Name}'?\n\nThis will also remove its stock entry.",
                "Delete", "Cancel");
            if (!confirm) return;

            try
            {
                var db = new StockDbContext();
                var stock = db.Stocks.FirstOrDefault(s => s.MenuItemId == product.Id);
                if (stock != null) db.Stocks.Remove(stock);

                var item = db.MenuItems.Find(product.Id);
                if (item != null) db.MenuItems.Remove(item);

                await db.SaveChangesAsync();
                await LoadProducts();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Cannot delete: {ex.InnerException?.Message ?? ex.Message}\n\nItem may be referenced by orders or GRNs.", "OK");
            }
        }
    }

    private async void ExportToExcel_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_allProducts == null || _allProducts.Count == 0)
            {
                await DisplayAlert("No Data", "No products to export.", "OK");
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"BakeryItems_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(desktopPath, fileName);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Bakery Items");

            // Headers
            ws.Cell(1, 1).Value = "Item ID";
            ws.Cell(1, 2).Value = "Item Name";
            ws.Cell(1, 3).Value = "Price";

            // Style headers
            var headerRange = ws.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1A237E);
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            for (int i = 0; i < _allProducts.Count; i++)
            {
                var p = _allProducts[i];
                ws.Cell(i + 2, 1).Value = p.Id;
                ws.Cell(i + 2, 2).Value = p.Name;
                ws.Cell(i + 2, 3).Value = p.Price;
            }

            // Format price column
            ws.Column(3).Style.NumberFormat.Format = "#,##0.00";

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            workbook.SaveAs(filePath);

            await DisplayAlert("Export Complete",
                $"Bakery items exported to Desktop:\n\n{fileName}\n\n{_allProducts.Count} items exported.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Export Error", ex.Message, "OK");
        }
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int MenuCategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public int StockQty { get; set; }
        public bool IsQuick { get; set; }
        public int QuickCategory { get; set; }
        public string QuickLabel { get; set; } = "—";
    }
}
