using RoyalBakeryCashier.Data;

namespace RoyalBakeryAdmin.Pages;

public partial class LoginPage : ContentPage
{
    private readonly StockDbContext _db;

    public LoginPage()
    {
        InitializeComponent();
        _db = new StockDbContext();
    }

    private void Password_Completed(object sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

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
            // Ensure DB schema is up to date
            // Skip EnsureCreated when using SQL auth (remote connection) —
            // the database already exists, and the SQL user may lack CREATE DATABASE permission.
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(App.DbUser))
                    _db.Database.EnsureCreated();
                try { _db.ApplyMigrations(); } catch { }
            });

            var user = _db.Users.FirstOrDefault(u =>
                u.Username == username && u.IsActive);

            if (user == null || user.PasswordHash != password)
            {
                ShowError("Invalid username or password.");
                return;
            }

            // Only Admin role can access Admin Panel
            if (user.Role != "Admin")
            {
                ShowError("This account does not have admin access.");
                return;
            }

            // Navigate to Admin Shell
            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            ShowError($"Connection error: {msg}");
        }
        finally
        {
            LoginBtn.IsEnabled = true;
            LoginBtn.Text = "Login";
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
