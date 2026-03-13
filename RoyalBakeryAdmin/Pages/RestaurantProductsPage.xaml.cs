using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class RestaurantProductsPage : ContentPage
{
    private List<RestaurantProductViewModel> _allProducts = new();

    public RestaurantProductsPage()
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
            var categories = await Task.Run(() => db.RestaurantCategories.ToList());
            var catMap = categories.ToDictionary(c => c.Id, c => c.Name);

            var items = await Task.Run(() => db.RestaurantItems.ToList());

            _allProducts = items.Select(i => new RestaurantProductViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Price = i.Price,
                RestaurantCategoryId = i.RestaurantCategoryId,
                CategoryName = catMap.TryGetValue(i.RestaurantCategoryId, out var cn) ? cn : "—"
            }).OrderBy(p => p.CategoryName).ThenBy(p => p.Name).ToList();

            ProductsView.ItemsSource = new ObservableCollection<RestaurantProductViewModel>(_allProducts);
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
            ProductsView.ItemsSource = new ObservableCollection<RestaurantProductViewModel>(_allProducts);
            return;
        }

        var filtered = _allProducts
            .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || p.CategoryName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ProductsView.ItemsSource = new ObservableCollection<RestaurantProductViewModel>(filtered);
    }

    private async void AddProduct_Clicked(object sender, EventArgs e)
    {
        var db = new StockDbContext();
        var categories = db.RestaurantCategories.OrderBy(c => c.Name).ToList();

        if (categories.Count == 0)
        {
            await DisplayAlert("No Categories", "Please add a restaurant category first.", "OK");
            return;
        }

        string name = await DisplayPromptAsync("Add Restaurant Product", "Product Name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string priceStr = await DisplayPromptAsync("Add Restaurant Product", "Price:", keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(priceStr, out decimal price) || price < 0) return;

        // Category selection
        string catChoice = await DisplayActionSheet("Select Category",
            "Cancel", null,
            categories.Select(c => c.Name).ToArray());
        if (string.IsNullOrEmpty(catChoice) || catChoice == "Cancel") return;
        var selectedCat = categories.FirstOrDefault(c => c.Name == catChoice);
        if (selectedCat == null) return;

        try
        {
            var item = new RestaurantItem
            {
                Name = name.Trim(),
                Price = price,
                RestaurantCategoryId = selectedCat.Id
            };

            db.RestaurantItems.Add(item);
            await db.SaveChangesAsync();

            await DisplayAlert("Success", $"Product '{name.Trim()}' added.", "OK");
            await LoadProducts();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void EditProduct_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is RestaurantProductViewModel product)
        {
            var db = new StockDbContext();
            var item = db.RestaurantItems.Find(product.Id);
            if (item == null) return;

            var categories = db.RestaurantCategories.OrderBy(c => c.Name).ToList();

            string name = await DisplayPromptAsync("Edit Restaurant Product", "Name:", initialValue: item.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            string priceStr = await DisplayPromptAsync("Edit Restaurant Product", "Price:",
                initialValue: item.Price.ToString("F2"), keyboard: Keyboard.Numeric);
            if (!decimal.TryParse(priceStr, out decimal price) || price < 0) return;

            string catChoice = await DisplayActionSheet("Select Category",
                "Cancel", null,
                categories.Select(c => c.Name).ToArray());
            if (string.IsNullOrEmpty(catChoice) || catChoice == "Cancel") return;
            var selectedCat = categories.FirstOrDefault(c => c.Name == catChoice);
            if (selectedCat == null) return;

            try
            {
                item.Name = name.Trim();
                item.Price = price;
                item.RestaurantCategoryId = selectedCat.Id;

                await db.SaveChangesAsync();
                await DisplayAlert("Success", "Product updated.", "OK");
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
        if (sender is Button btn && btn.BindingContext is RestaurantProductViewModel product)
        {
            bool confirm = await DisplayAlert("Delete Product",
                $"Are you sure you want to delete '{product.Name}'?",
                "Delete", "Cancel");
            if (!confirm) return;

            try
            {
                var db = new StockDbContext();
                var item = db.RestaurantItems.Find(product.Id);
                if (item != null) db.RestaurantItems.Remove(item);

                await db.SaveChangesAsync();
                await LoadProducts();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Cannot delete: {ex.InnerException?.Message ?? ex.Message}\n\nItem may be referenced by restaurant orders.", "OK");
            }
        }
    }

    public class RestaurantProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = "";
        public int RestaurantCategoryId { get; set; }
    }
}
