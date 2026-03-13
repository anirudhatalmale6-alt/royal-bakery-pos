namespace RoyalBakeryRestaurant;

public partial class App : Application
{
    public static string DatabaseServer { get; set; } = ".\\SQLEXPRESS";
    public static string DbUser { get; set; } = "";
    public static string DbPassword { get; set; } = "";
    public static string LoggedInUserName { get; set; } = "";
    public static int LoggedInUserId { get; set; }

    /// <summary>
    /// KOT printer name (separate thermal printer for kitchen orders).
    /// Set via terminal.config: KOTPrinter=EPSON TM-T82 Receipt
    /// </summary>
    public static string KOTPrinterName { get; set; } = "";

    public App()
    {
        InitializeComponent();

        LoadTerminalConfig();

        if (!string.IsNullOrEmpty(DbUser))
        {
            RoyalBakeryCashier.Data.StockDbContext.ConnectionStringOverride =
                $"Server={DatabaseServer};Database=RoyalBakery;User Id={DbUser};Password={DbPassword};TrustServerCertificate=True;Connect Timeout=120;";
        }
        else
        {
            RoyalBakeryCashier.Data.StockDbContext.ConnectionStringOverride =
                $"Server={DatabaseServer};Database=RoyalBakery;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=120;";
        }

        MainPage = new NavigationPage(new Pages.LoginPage())
        {
            BarBackgroundColor = Color.FromArgb("#1A1A1A"),
            BarTextColor = Colors.White
        };
    }

    private void LoadTerminalConfig()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "terminal.config");

            if (!File.Exists(configPath))
                configPath = Path.Combine(FileSystem.AppDataDirectory, "terminal.config");

            if (File.Exists(configPath))
            {
                var lines = File.ReadAllLines(configPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length != 2) continue;
                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key.Equals("Server", StringComparison.OrdinalIgnoreCase))
                        DatabaseServer = val;
                    else if (key.Equals("DbUser", StringComparison.OrdinalIgnoreCase))
                        DbUser = val;
                    else if (key.Equals("DbPassword", StringComparison.OrdinalIgnoreCase))
                        DbPassword = val;
                    else if (key.Equals("KOTPrinter", StringComparison.OrdinalIgnoreCase))
                        KOTPrinterName = val;
                }
            }
        }
        catch { }
    }
}
