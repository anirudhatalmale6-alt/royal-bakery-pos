using Microsoft.EntityFrameworkCore;
using RoyalBakeryAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Support running as a Windows Service (auto-start with Windows)
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "RoyalBakeryAPI";
});

// Add services
builder.Services.AddControllers();
builder.Services.AddDbContext<BakeryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=.\\SQLEXPRESS;Database=RoyalBakery;Trusted_Connection=True;TrustServerCertificate=True;"));

// Allow all origins (for local network Android access)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto-create any missing tables and seed default admin user
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BakeryDbContext>();
    try
    {
        // Ensure database exists first (creates DB + all tables if brand new)
        db.Database.EnsureCreated();
        Console.WriteLine("Database ensured.");

        // Then add any individual missing tables (for existing DBs missing newer tables)
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MenuCategories' AND xtype='U')
            CREATE TABLE MenuCategories (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Name NVARCHAR(100) NOT NULL
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MenuItems' AND xtype='U')
            CREATE TABLE MenuItems (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Name NVARCHAR(MAX) NOT NULL,
                Price DECIMAL(18,2) NOT NULL,
                MenuCategoryId INT NOT NULL,
                IsQuick BIT NOT NULL DEFAULT 0
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Stocks' AND xtype='U')
            CREATE TABLE Stocks (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                MenuItemId INT NOT NULL,
                Quantity INT NOT NULL,
                FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GRNs' AND xtype='U')
            CREATE TABLE GRNs (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                GRNNumber NVARCHAR(MAX) NOT NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GRNItems' AND xtype='U')
            CREATE TABLE GRNItems (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                GRNId INT NOT NULL,
                MenuItemId INT NOT NULL,
                Quantity INT NOT NULL,
                Price DECIMAL(18,2) NOT NULL,
                CurrentQuantity INT NOT NULL,
                FOREIGN KEY (GRNId) REFERENCES GRNs(Id),
                FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GRNAdjustmentRequests' AND xtype='U')
            CREATE TABLE GRNAdjustmentRequests (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                GRNId INT NOT NULL,
                Reason NVARCHAR(MAX) NOT NULL,
                AdminCode NVARCHAR(MAX) NOT NULL,
                IsApproved BIT NOT NULL DEFAULT 0,
                RequestedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                FOREIGN KEY (GRNId) REFERENCES GRNs(Id)
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GRNAdjustmentRequestItems' AND xtype='U')
            CREATE TABLE GRNAdjustmentRequestItems (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                GRNAdjustmentRequestId INT NOT NULL,
                MenuItemId INT NOT NULL,
                ItemName NVARCHAR(MAX) NOT NULL,
                RequestedQuantity INT NOT NULL,
                Price DECIMAL(18,2) NOT NULL,
                FOREIGN KEY (GRNAdjustmentRequestId) REFERENCES GRNAdjustmentRequests(Id) ON DELETE CASCADE
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Clearances' AND xtype='U')
            CREATE TABLE Clearances (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                DateTime DATETIME2 NOT NULL DEFAULT GETDATE(),
                MenuItemId INT NOT NULL,
                Quantity INT NOT NULL,
                Reason NVARCHAR(MAX) NOT NULL,
                Note NVARCHAR(MAX) NULL,
                FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
            CREATE TABLE Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Username NVARCHAR(50) NOT NULL,
                PasswordHash NVARCHAR(200) NOT NULL,
                DisplayName NVARCHAR(100) NOT NULL,
                Role NVARCHAR(30) NOT NULL DEFAULT 'Cashier',
                IsActive BIT NOT NULL DEFAULT 1,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
            );

            -- Cashier/Salesman tables (needed so DB is complete)
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
            CREATE TABLE Orders (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                DateTime DATETIME2 NOT NULL,
                Status INT NOT NULL DEFAULT 1,
                TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
            CREATE TABLE OrderItems (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                OrderId INT NOT NULL,
                MenuItemId INT NOT NULL,
                Quantity INT NOT NULL,
                PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderPayments' AND xtype='U')
            CREATE TABLE OrderPayments (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                OrderId INT NOT NULL,
                PaymentType INT NOT NULL DEFAULT 0,
                TenderAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                DateTime DATETIME2 NOT NULL
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Sales' AND xtype='U')
            CREATE TABLE Sales (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                DateTime DATETIME2 NOT NULL,
                TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                CardAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                ChangeGiven DECIMAL(18,2) NOT NULL DEFAULT 0,
                CashierName NVARCHAR(MAX) NULL,
                InvoiceNumber NVARCHAR(MAX) NOT NULL DEFAULT ''
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SaleItems' AND xtype='U')
            CREATE TABLE SaleItems (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                SaleId INT NOT NULL,
                MenuItemId INT NOT NULL,
                ItemName NVARCHAR(MAX) NOT NULL DEFAULT '',
                Quantity INT NOT NULL,
                PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
                FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SalesOrders' AND xtype='U')
            CREATE TABLE SalesOrders (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                SalesOrderNumber NVARCHAR(20) NOT NULL,
                CreatedAt DATETIME2 NOT NULL,
                TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                Status INT NOT NULL DEFAULT 0,
                TerminalName NVARCHAR(MAX) NULL,
                CustomerName NVARCHAR(MAX) NULL
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SalesOrderItems' AND xtype='U')
            CREATE TABLE SalesOrderItems (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                SalesOrderId INT NOT NULL,
                MenuItemId INT NOT NULL,
                Quantity INT NOT NULL,
                PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id) ON DELETE CASCADE,
                FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GRNEditLogs' AND xtype='U')
            CREATE TABLE GRNEditLogs (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                GRNId INT NOT NULL,
                Reason NVARCHAR(MAX) NOT NULL,
                ChangeSummary NVARCHAR(MAX) NOT NULL DEFAULT '',
                EditedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                FOREIGN KEY (GRNId) REFERENCES GRNs(Id)
            );

            -- Add QuickCategory column if missing
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MenuItems') AND name = 'QuickCategory')
              ALTER TABLE MenuItems ADD QuickCategory INT NOT NULL DEFAULT 0;
        ");
        Console.WriteLine("Database tables verified/created.");

        // Seed default admin user if no users exist
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = "admin123",
                DisplayName = "Admin",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            Console.WriteLine("Default admin user created (admin/admin123)");
        }

        // Seed menu data + initial GRN (qty 10) if no categories exist yet
        bool hasMenuData = false;
        try { hasMenuData = db.MenuCategories.Any(); } catch { }
        if (!hasMenuData)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("RoyalBakeryAPI.SeedData.sql");
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    string seedSql = reader.ReadToEnd();
                    db.Database.ExecuteSqlRaw(seedSql);
                    Console.WriteLine("Menu data seeded: 20 categories, 195 items, stocks (qty 10), initial GRN created.");
                }
                else
                {
                    Console.WriteLine("WARNING: SeedData.sql embedded resource not found!");
                }
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"Seed data error: {seedEx.Message}");
            }
        }
        else
        {
            Console.WriteLine("Menu data already exists, skipping seed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"========================================");
        Console.WriteLine($"DB INIT ERROR: {ex.Message}");
        Console.WriteLine($"Inner: {ex.InnerException?.Message}");
        Console.WriteLine($"========================================");
        Console.WriteLine("API will continue but database may not be ready!");
    }
}

// Show detailed errors so we can debug 500s
app.UseDeveloperExceptionPage();

app.UseCors();
app.MapControllers();

// Listen on all interfaces so Android devices can reach it
app.Urls.Add("http://0.0.0.0:5000");

Console.WriteLine("Royal Bakery API running on http://0.0.0.0:5000");
Console.WriteLine("Endpoints:");
Console.WriteLine("  GET  /api/menu/categories");
Console.WriteLine("  GET  /api/menu/items");
Console.WriteLine("  GET  /api/stock");
Console.WriteLine("  GET  /api/stock/{menuItemId}");
Console.WriteLine("  GET  /api/grn");
Console.WriteLine("  GET  /api/grn/{id}");
Console.WriteLine("  POST /api/grn");
Console.WriteLine("  GET  /api/adjustment");
Console.WriteLine("  POST /api/adjustment");
Console.WriteLine("  POST /api/adjustment/{id}/approve");
Console.WriteLine("  PUT  /api/grn/{id}          (direct edit)");
Console.WriteLine("  GET  /api/grn/{id}/edits    (edit history)");
Console.WriteLine("  GET  /api/clearance");
Console.WriteLine("  POST /api/clearance");

app.Run();
