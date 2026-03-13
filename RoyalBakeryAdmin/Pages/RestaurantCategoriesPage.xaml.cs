using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class RestaurantCategoriesPage : ContentPage
{
    private List<RestaurantCategoryViewModel> _allCategories = new();

    public RestaurantCategoriesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        try
        {
            var db = new StockDbContext();
            var categories = await Task.Run(() => db.RestaurantCategories.OrderBy(c => c.Name).ToList());
            var items = await Task.Run(() => db.RestaurantItems.ToList());

            _allCategories = categories.Select(c => new RestaurantCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                ProductCount = items.Count(i => i.RestaurantCategoryId == c.Id)
            }).ToList();

            CategoriesView.ItemsSource = new ObservableCollection<RestaurantCategoryViewModel>(_allCategories);
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
            CategoriesView.ItemsSource = new ObservableCollection<RestaurantCategoryViewModel>(_allCategories);
            return;
        }

        var filtered = _allCategories
            .Where(c => c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        CategoriesView.ItemsSource = new ObservableCollection<RestaurantCategoryViewModel>(filtered);
    }

    private async void AddCategory_Clicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Add Restaurant Category", "Category Name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var db = new StockDbContext();
            db.RestaurantCategories.Add(new RestaurantCategory { Name = name.Trim() });
            await db.SaveChangesAsync();
            await DisplayAlert("Success", $"Category '{name.Trim()}' added.", "OK");
            await LoadCategories();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void EditCategory_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is RestaurantCategoryViewModel cat)
        {
            string name = await DisplayPromptAsync("Edit Restaurant Category", "Name:", initialValue: cat.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                var db = new StockDbContext();
                var entity = db.RestaurantCategories.Find(cat.Id);
                if (entity == null) return;

                entity.Name = name.Trim();
                await db.SaveChangesAsync();
                await LoadCategories();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    private async void DeleteCategory_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is RestaurantCategoryViewModel cat)
        {
            if (cat.ProductCount > 0)
            {
                await DisplayAlert("Cannot Delete",
                    $"Category '{cat.Name}' has {cat.ProductCount} product(s). Move or delete them first.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Delete Category",
                $"Delete category '{cat.Name}'?", "Delete", "Cancel");
            if (!confirm) return;

            try
            {
                var db = new StockDbContext();
                var entity = db.RestaurantCategories.Find(cat.Id);
                if (entity != null) db.RestaurantCategories.Remove(entity);
                await db.SaveChangesAsync();
                await LoadCategories();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    public class RestaurantCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ProductCount { get; set; }
    }
}
