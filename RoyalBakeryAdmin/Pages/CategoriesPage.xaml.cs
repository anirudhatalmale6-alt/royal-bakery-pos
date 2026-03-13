using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using System.Collections.ObjectModel;

namespace RoyalBakeryAdmin.Pages;

public partial class CategoriesPage : ContentPage
{
    public CategoriesPage()
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
            var categories = await Task.Run(() => db.MenuCategories.OrderBy(c => c.Name).ToList());
            var items = await Task.Run(() => db.MenuItems.ToList());

            var viewModels = categories.Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                ProductCount = items.Count(i => i.MenuCategoryId == c.Id)
            }).ToList();

            CategoriesView.ItemsSource = new ObservableCollection<CategoryViewModel>(viewModels);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void AddCategory_Clicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync("Add Category", "Category Name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var db = new StockDbContext();
            db.MenuCategories.Add(new MenuCategory { Name = name.Trim() });
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
        if (sender is Button btn && btn.BindingContext is CategoryViewModel cat)
        {
            string name = await DisplayPromptAsync("Edit Category", "Name:", initialValue: cat.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                var db = new StockDbContext();
                var entity = db.MenuCategories.Find(cat.Id);
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
        if (sender is Button btn && btn.BindingContext is CategoryViewModel cat)
        {
            if (cat.ProductCount > 0)
            {
                await DisplayAlert("Cannot Delete",
                    $"Category '{cat.Name}' has {cat.ProductCount} products. Move or delete them first.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Delete Category",
                $"Delete category '{cat.Name}'?", "Delete", "Cancel");
            if (!confirm) return;

            try
            {
                var db = new StockDbContext();
                var entity = db.MenuCategories.Find(cat.Id);
                if (entity != null) db.MenuCategories.Remove(entity);
                await db.SaveChangesAsync();
                await LoadCategories();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ProductCount { get; set; }
    }
}
