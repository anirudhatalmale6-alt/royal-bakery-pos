using RoyalBakeryCashier.Data;

namespace RoyalBakeryRestaurant.Pages;

public partial class LoginPage : ContentPage
{
    private readonly StockDbContext _db;

    public LoginPage()
    {
        InitializeComponent();
        _db = new StockDbContext();
    }

    private void Password_Completed(object sender, EventArgs e) => PasswordEntry.Focus();

    private async void Login_Clicked(object sender, EventArgs e)
    {
        string username = (UsernameEntry.Text ?? "").Trim();
        string password = (PasswordEntry.Text ?? "").Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Please enter username and password.");
            return;
        }

        LoginBtn.IsEnabled = false;
        LoginBtn.Text = "Logging in...";
        ErrorLabel.IsVisible = false;

        try
        {
            await Task.Run(() =>
            {
                _db.Database.EnsureCreated();
                try { _db.ApplyMigrations(); } catch { }
            });

            var user = _db.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
            if (user == null || user.PasswordHash != password)
            {
                ShowError("Invalid username or password.");
                return;
            }

            App.LoggedInUserName = user.DisplayName;
            App.LoggedInUserId = user.Id;

            Application.Current!.MainPage = new NavigationPage(new RestaurantPage())
            {
                BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                BarTextColor = Colors.White
            };
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            LoginBtn.IsEnabled = true;
            LoginBtn.Text = "Login";
        }
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }
}
