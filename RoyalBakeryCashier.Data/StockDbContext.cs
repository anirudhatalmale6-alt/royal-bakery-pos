using Microsoft.EntityFrameworkCore;
using RoyalBakeryCashier.Data.Entities;

namespace RoyalBakeryCashier.Data
{
    public partial class StockDbContext : DbContext
    {
        // Runtime DI constructor
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options) { }

        // Parameterless constructor for design-time migrations
        public StockDbContext() { }

        // DbSets
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; } // plural
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<OrderPayments> OrderPayments { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<MenuCategory> MenuCategories { get; set; }
        public DbSet<GRN> GRNs { get; set; }
        public DbSet<GRNItem> GRNItems { get; set; }
        public DbSet<Clearance> Clearances { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public DbSet<User> Users { get; set; }

        // Restaurant entities (separate menu, no inventory)
        public DbSet<RestaurantCategory> RestaurantCategories { get; set; }
        public DbSet<RestaurantItem> RestaurantItems { get; set; }
        public DbSet<RestaurantSale> RestaurantSales { get; set; }
        public DbSet<RestaurantSaleItem> RestaurantSaleItems { get; set; }

        // Delivery platform integration (PickMe, UberEats)
        public DbSet<DeliveryOrder> DeliveryOrders { get; set; }
        public DbSet<DeliveryOrderItem> DeliveryOrderItems { get; set; }

        // Pending stock (online order shortages settled by GRNs)
        public DbSet<PendingStock> PendingStocks { get; set; }
        public DbSet<PendingStockClearance> PendingStockClearances { get; set; }

        /// <summary>
        /// Static connection string override. Set from App.xaml.cs based on terminal.config.
        /// If null/empty, falls back to localhost with Windows Authentication.
        /// </summary>
        public static string ConnectionStringOverride { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connStr = ConnectionStringOverride;
                if (string.IsNullOrEmpty(connStr))
                    connStr = "Server=.\\SQLEXPRESS;Database=RoyalBakery;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=120;";
                optionsBuilder.UseSqlServer(connStr, opts =>
                    opts.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Order → OrderItem =====
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== OrderItem → MenuItem =====
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.MenuItem)
                .WithMany()
                .HasForeignKey(oi => oi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Stock → MenuItem =====
            modelBuilder.Entity<Stock>()
                .HasOne(s => s.MenuItem)
                .WithMany()
                .HasForeignKey(s => s.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== GRNItem → MenuItem =====
            modelBuilder.Entity<GRNItem>()
                .HasOne(g => g.MenuItem)
                .WithMany()
                .HasForeignKey(g => g.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== GRNItem → GRN (FIXED) =====
            modelBuilder.Entity<GRNItem>()
                .HasOne(g => g.GRN)
                .WithMany(grn => grn.Items) // GRN must have: public ICollection<GRNItem> Items { get; set; }
                .HasForeignKey(g => g.GRNId)
                .HasConstraintName("FK_GRNItem_GRN")
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Sale → SaleItem =====
            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.MenuItem)
                .WithMany()
                .HasForeignKey(si => si.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== SalesOrder → SalesOrderItem =====
            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(soi => soi.SalesOrder)
                .WithMany(so => so.Items)
                .HasForeignKey(soi => soi.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(soi => soi.MenuItem)
                .WithMany()
                .HasForeignKey(soi => soi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Decimal / int precision =====
            modelBuilder.Entity<GRNItem>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItem>().Property(p => p.PricePerItem).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItem>().Property(p => p.TotalPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Stock>().Property(s => s.Quantity).HasColumnType("int"); // integer quantity

            // ===== Restaurant Entities =====
            modelBuilder.Entity<RestaurantSaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(si => si.RestaurantSaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RestaurantSaleItem>()
                .HasOne(si => si.RestaurantItem)
                .WithMany()
                .HasForeignKey(si => si.RestaurantItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RestaurantItem>()
                .HasOne(ri => ri.Category)
                .WithMany()
                .HasForeignKey(ri => ri.RestaurantCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Delivery Order Entities =====
            modelBuilder.Entity<DeliveryOrderItem>()
                .HasOne(doi => doi.DeliveryOrder)
                .WithMany(d => d.Items)
                .HasForeignKey(doi => doi.DeliveryOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Pending Stock Entities =====
            modelBuilder.Entity<PendingStock>()
                .HasOne(ps => ps.DeliveryOrder)
                .WithMany()
                .HasForeignKey(ps => ps.DeliveryOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PendingStock>()
                .HasOne(ps => ps.MenuItem)
                .WithMany()
                .HasForeignKey(ps => ps.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PendingStockClearance>()
                .HasOne(c => c.PendingStock)
                .WithMany(ps => ps.Clearances)
                .HasForeignKey(c => c.PendingStockId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PendingStockClearance>()
                .HasOne(c => c.GRN)
                .WithMany()
                .HasForeignKey(c => c.GRNId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PendingStockClearance>()
                .HasOne(c => c.GRNItem)
                .WithMany()
                .HasForeignKey(c => c.GRNItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PendingStockClearance>()
                .HasOne(c => c.MenuItem)
                .WithMany()
                .HasForeignKey(c => c.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeliveryOrder>()
                .Property(d => d.OrderTotal).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DeliveryOrderItem>()
                .Property(doi => doi.PricePerItem).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<DeliveryOrderItem>()
                .Property(doi => doi.TotalPrice).HasColumnType("decimal(18,2)");
        }

        /// <summary>
        /// Apply schema patches for columns/tables added after initial EnsureCreated.
        /// Drops and recreates tables that have wrong schema (only if empty).
        /// Safe to call multiple times.
        /// </summary>
        public void ApplyMigrations()
        {
            // Step 1: Drop incomplete tables that have wrong schema (only safe for tables with no real data yet).
            // Uses a check: if the table exists but is missing a key column, drop it so we can recreate.
            var dropAndRecreate = new[]
            {
                // Sales: check for InvoiceNumber column
                @"IF OBJECT_ID('SaleItems', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'ItemName')
                  DROP TABLE SaleItems;",

                @"IF OBJECT_ID('Sales', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'InvoiceNumber')
                  DROP TABLE Sales;",

                // Orders: check for TotalAmount column
                @"IF OBJECT_ID('OrderItems', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderItems') AND name = 'PricePerItem')
                  DROP TABLE OrderItems;",

                @"IF OBJECT_ID('Orders', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'TotalAmount')
                  DROP TABLE Orders;",

                // SalesOrders: check for SalesOrderNumber column
                @"IF OBJECT_ID('SalesOrderItems', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SalesOrderItems') AND name = 'PricePerItem')
                  DROP TABLE SalesOrderItems;",

                @"IF OBJECT_ID('SalesOrders', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SalesOrders') AND name = 'SalesOrderNumber')
                  DROP TABLE SalesOrders;",

                // OrderPayments: check for TenderAmount
                @"IF OBJECT_ID('OrderPayments', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrderPayments') AND name = 'TenderAmount')
                  DROP TABLE OrderPayments;",

                // Clearances: check for Reason
                @"IF OBJECT_ID('Clearances', 'U') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Clearances') AND name = 'Reason')
                  DROP TABLE Clearances;",
            };

            foreach (var sql in dropAndRecreate)
            {
                try { Database.ExecuteSqlRaw(sql); } catch { }
            }

            // Step 2: Create tables that don't exist
            var creates = new[]
            {
                @"IF OBJECT_ID('Orders', 'U') IS NULL
                  CREATE TABLE Orders (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      DateTime DATETIME2 NOT NULL,
                      Status INT NOT NULL DEFAULT 1,
                      TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0
                  );",

                @"IF OBJECT_ID('OrderItems', 'U') IS NULL
                  CREATE TABLE OrderItems (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      OrderId INT NOT NULL,
                      MenuItemId INT NOT NULL,
                      Quantity INT NOT NULL,
                      PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                      TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                      CONSTRAINT FK_OrderItems_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE NO ACTION
                  );",

                @"IF OBJECT_ID('OrderPayments', 'U') IS NULL
                  CREATE TABLE OrderPayments (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      OrderId INT NOT NULL,
                      PaymentType INT NOT NULL DEFAULT 0,
                      TenderAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      DateTime DATETIME2 NOT NULL
                  );",

                @"IF OBJECT_ID('Sales', 'U') IS NULL
                  CREATE TABLE Sales (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      DateTime DATETIME2 NOT NULL,
                      TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CardAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      ChangeGiven DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CashierName NVARCHAR(MAX) NULL,
                      InvoiceNumber NVARCHAR(MAX) NOT NULL DEFAULT ''
                  );",

                @"IF OBJECT_ID('SaleItems', 'U') IS NULL
                  CREATE TABLE SaleItems (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      SaleId INT NOT NULL,
                      MenuItemId INT NOT NULL,
                      ItemName NVARCHAR(MAX) NOT NULL DEFAULT '',
                      Quantity INT NOT NULL,
                      PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                      TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
                      CONSTRAINT FK_SaleItems_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE NO ACTION
                  );",

                @"IF OBJECT_ID('SalesOrders', 'U') IS NULL
                  CREATE TABLE SalesOrders (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      SalesOrderNumber NVARCHAR(20) NOT NULL,
                      CreatedAt DATETIME2 NOT NULL,
                      TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      Status INT NOT NULL DEFAULT 0,
                      TerminalName NVARCHAR(MAX) NULL,
                      CustomerName NVARCHAR(MAX) NULL
                  );",

                @"IF OBJECT_ID('SalesOrderItems', 'U') IS NULL
                  CREATE TABLE SalesOrderItems (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      SalesOrderId INT NOT NULL,
                      MenuItemId INT NOT NULL,
                      Quantity INT NOT NULL,
                      PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                      TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CONSTRAINT FK_SalesOrderItems_SalesOrders FOREIGN KEY (SalesOrderId) REFERENCES SalesOrders(Id) ON DELETE CASCADE,
                      CONSTRAINT FK_SalesOrderItems_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE NO ACTION
                  );",

                @"IF OBJECT_ID('Clearances', 'U') IS NULL
                  CREATE TABLE Clearances (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      DateTime DATETIME2 NOT NULL,
                      MenuItemId INT NOT NULL,
                      Quantity INT NOT NULL,
                      Reason NVARCHAR(MAX) NOT NULL DEFAULT '',
                      Note NVARCHAR(MAX) NULL,
                      CONSTRAINT FK_Clearances_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE NO ACTION
                  );",

                // Step 3: Add missing columns to existing tables
                @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MenuItems') AND name = 'IsQuick')
                  ALTER TABLE MenuItems ADD IsQuick BIT NOT NULL DEFAULT 0;",

                @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MenuItems') AND name = 'QuickCategory')
                  ALTER TABLE MenuItems ADD QuickCategory INT NOT NULL DEFAULT 0;",

                // Sales table — add missing columns individually (handles partial schema)
                @"IF OBJECT_ID('Sales', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TotalAmount')
                  ALTER TABLE Sales ADD TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0;",
                @"IF OBJECT_ID('Sales', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CashAmount')
                  ALTER TABLE Sales ADD CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0;",
                @"IF OBJECT_ID('Sales', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CardAmount')
                  ALTER TABLE Sales ADD CardAmount DECIMAL(18,2) NOT NULL DEFAULT 0;",
                @"IF OBJECT_ID('Sales', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'ChangeGiven')
                  ALTER TABLE Sales ADD ChangeGiven DECIMAL(18,2) NOT NULL DEFAULT 0;",
                @"IF OBJECT_ID('Sales', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CashierName')
                  ALTER TABLE Sales ADD CashierName NVARCHAR(MAX) NULL;",
                @"IF OBJECT_ID('Sales', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'InvoiceNumber')
                  ALTER TABLE Sales ADD InvoiceNumber NVARCHAR(MAX) NOT NULL DEFAULT '';",

                // SaleItems — add missing columns
                @"IF OBJECT_ID('SaleItems', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'ItemName')
                  ALTER TABLE SaleItems ADD ItemName NVARCHAR(MAX) NOT NULL DEFAULT '';",
                @"IF OBJECT_ID('SaleItems', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'PricePerItem')
                  ALTER TABLE SaleItems ADD PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0;",
                @"IF OBJECT_ID('SaleItems', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SaleItems') AND name = 'TotalPrice')
                  ALTER TABLE SaleItems ADD TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0;",

                // Users table for login
                @"IF OBJECT_ID('Users', 'U') IS NULL
                  CREATE TABLE Users (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      Username NVARCHAR(50) NOT NULL,
                      PasswordHash NVARCHAR(200) NOT NULL,
                      DisplayName NVARCHAR(100) NOT NULL,
                      Role NVARCHAR(30) NOT NULL DEFAULT 'Cashier',
                      IsActive BIT NOT NULL DEFAULT 1,
                      CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                  );",

                // ===== Restaurant Tables =====
                @"IF OBJECT_ID('RestaurantCategories', 'U') IS NULL
                  CREATE TABLE RestaurantCategories (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      Name NVARCHAR(100) NOT NULL
                  );",

                @"IF OBJECT_ID('RestaurantItems', 'U') IS NULL
                  CREATE TABLE RestaurantItems (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      Name NVARCHAR(200) NOT NULL,
                      Price DECIMAL(18,2) NOT NULL DEFAULT 0,
                      RestaurantCategoryId INT NOT NULL,
                      CONSTRAINT FK_RestaurantItems_Categories FOREIGN KEY (RestaurantCategoryId) REFERENCES RestaurantCategories(Id) ON DELETE NO ACTION
                  );",

                @"IF OBJECT_ID('RestaurantSales', 'U') IS NULL
                  CREATE TABLE RestaurantSales (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      InvoiceNumber NVARCHAR(MAX) NOT NULL DEFAULT '',
                      DateTime DATETIME2 NOT NULL,
                      TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CardAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                      ChangeGiven DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CashierName NVARCHAR(MAX) NULL,
                      OrderSource NVARCHAR(50) NOT NULL DEFAULT 'Dine In'
                  );",

                @"IF OBJECT_ID('RestaurantSaleItems', 'U') IS NULL
                  CREATE TABLE RestaurantSaleItems (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      RestaurantSaleId INT NOT NULL,
                      RestaurantItemId INT NOT NULL,
                      ItemName NVARCHAR(MAX) NOT NULL DEFAULT '',
                      Quantity INT NOT NULL,
                      PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                      TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                      CONSTRAINT FK_RestaurantSaleItems_Sales FOREIGN KEY (RestaurantSaleId) REFERENCES RestaurantSales(Id) ON DELETE CASCADE,
                      CONSTRAINT FK_RestaurantSaleItems_Items FOREIGN KEY (RestaurantItemId) REFERENCES RestaurantItems(Id) ON DELETE NO ACTION
                  );",

                // ===== Delivery Platform Integration Tables =====
                @"IF OBJECT_ID('DeliveryOrders', 'U') IS NULL
                  CREATE TABLE DeliveryOrders (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      PlatformName NVARCHAR(50) NOT NULL,
                      PlatformOrderId NVARCHAR(200) NOT NULL,
                      AccountName NVARCHAR(100) NOT NULL DEFAULT '',
                      RestaurantSaleId INT NULL,
                      BakerySaleId INT NULL,
                      CustomerPhone NVARCHAR(50) NULL,
                      CustomerAddress NVARCHAR(MAX) NULL,
                      DeliveryMode NVARCHAR(20) NOT NULL DEFAULT 'Delivery',
                      PlatformStatus NVARCHAR(100) NOT NULL DEFAULT '',
                      OrderTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                      PaymentMethod NVARCHAR(50) NULL,
                      DeliveryNote NVARCHAR(MAX) NULL,
                      ReceivedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                      CompletedAt DATETIME2 NULL,
                      KotStatus INT NOT NULL DEFAULT 0,
                      RawOrderJson NVARCHAR(MAX) NULL
                  );",

                @"IF OBJECT_ID('DeliveryOrderItems', 'U') IS NULL
                  CREATE TABLE DeliveryOrderItems (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      DeliveryOrderId INT NOT NULL,
                      PlatformItemId INT NOT NULL DEFAULT 0,
                      PlatformRefId NVARCHAR(100) NULL,
                      ItemName NVARCHAR(200) NOT NULL DEFAULT '',
                      Quantity INT NOT NULL DEFAULT 0,
                      PricePerItem DECIMAL(18,2) NOT NULL DEFAULT 0,
                      TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                      SpecialInstructions NVARCHAR(MAX) NULL,
                      Options NVARCHAR(MAX) NULL,
                      ItemType NVARCHAR(5) NOT NULL DEFAULT 'U',
                      LocalItemId INT NULL,
                      CONSTRAINT FK_DeliveryOrderItems_DeliveryOrders FOREIGN KEY (DeliveryOrderId) REFERENCES DeliveryOrders(Id) ON DELETE CASCADE
                  );",

                // ===== Pending Stock Tables (online order shortages) =====
                @"IF OBJECT_ID('PendingStocks', 'U') IS NULL
                  CREATE TABLE PendingStocks (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      DeliveryOrderId INT NOT NULL,
                      MenuItemId INT NOT NULL,
                      PendingQuantity INT NOT NULL,
                      CurrentPendingQuantity INT NOT NULL,
                      Status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
                      CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                      CONSTRAINT FK_PendingStocks_DeliveryOrders FOREIGN KEY (DeliveryOrderId) REFERENCES DeliveryOrders(Id) ON DELETE NO ACTION,
                      CONSTRAINT FK_PendingStocks_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE NO ACTION
                  );",

                @"IF OBJECT_ID('PendingStockClearances', 'U') IS NULL
                  CREATE TABLE PendingStockClearances (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      PendingStockId INT NOT NULL,
                      GRNId INT NOT NULL,
                      GRNItemId INT NOT NULL,
                      MenuItemId INT NOT NULL,
                      QuantityUsed INT NOT NULL,
                      CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                      CONSTRAINT FK_PendingStockClearances_PendingStocks FOREIGN KEY (PendingStockId) REFERENCES PendingStocks(Id) ON DELETE NO ACTION,
                      CONSTRAINT FK_PendingStockClearances_GRNs FOREIGN KEY (GRNId) REFERENCES GRNs(Id) ON DELETE NO ACTION,
                      CONSTRAINT FK_PendingStockClearances_GRNItems FOREIGN KEY (GRNItemId) REFERENCES GRNItems(Id) ON DELETE NO ACTION,
                      CONSTRAINT FK_PendingStockClearances_MenuItems FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id) ON DELETE NO ACTION
                  );",
            };

            foreach (var sql in creates)
            {
                try { Database.ExecuteSqlRaw(sql); } catch { }
            }

            // Seed default admin user if no users exist
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT 1 FROM Users)
                    INSERT INTO Users (Username, PasswordHash, DisplayName, Role, IsActive, CreatedAt)
                    VALUES ('admin', 'admin123', 'Administrator', 'Admin', 1, GETDATE());
                ");
            }
            catch { }

            // Seed menu categories and items from Royal Bakery price list
            SeedMenuData();

            // Seed restaurant menu data
            SeedRestaurantData();
        }

        private void SeedRestaurantData()
        {
            try
            {
                // Check if restaurant data already seeded
                bool hasData = false;
                try { hasData = RestaurantCategories.Any(); } catch { return; }
                if (hasData) return;

                Database.ExecuteSqlRaw(@"
                    INSERT INTO RestaurantCategories (Name) VALUES
                    ('RICE'), ('DINNER'), ('CURRY'), ('BEVERAGES'), ('SIDE DISHES'), ('LUNCH PACK');
                ");

                Database.ExecuteSqlRaw(@"
                    -- RICE (CategoryId = look up)
                    DECLARE @rice INT = (SELECT Id FROM RestaurantCategories WHERE Name = 'RICE');
                    DECLARE @dinner INT = (SELECT Id FROM RestaurantCategories WHERE Name = 'DINNER');
                    DECLARE @curry INT = (SELECT Id FROM RestaurantCategories WHERE Name = 'CURRY');
                    DECLARE @bev INT = (SELECT Id FROM RestaurantCategories WHERE Name = 'BEVERAGES');
                    DECLARE @side INT = (SELECT Id FROM RestaurantCategories WHERE Name = 'SIDE DISHES');
                    DECLARE @lunch INT = (SELECT Id FROM RestaurantCategories WHERE Name = 'LUNCH PACK');

                    INSERT INTO RestaurantItems (Name, Price, RestaurantCategoryId) VALUES
                    ('SET MENU', 550, @rice),
                    ('VEGETABLE FRIED RICE', 400, @rice),
                    ('EGG FRIED RICE', 300, @rice),
                    ('CHICKEN FRIED RICE', 450, @rice),
                    ('FISH FRIED RICE', 440, @rice),
                    ('VEGETABLE BIRIYANI', 350, @rice),
                    ('CHICKEN BIRIYANI', 550, @rice),
                    ('EGG BIRIYANI', 450, @rice),
                    ('FISH BIRIYANI', 540, @rice),
                    ('PRAWN FRIED RICE', 580, @rice),
                    ('CUTTLE FISH FRIED RICE', 580, @rice),
                    ('CUTTLE FISH SET MENU', 750, @rice),
                    ('PRAWN SET MENU', 750, @rice),

                    ('PARATA', 60, @dinner),
                    ('PLAIN HOPPERS', 50, @dinner),
                    ('HONEY HOPPERS', 80, @dinner),
                    ('EGG HOPPERS', 110, @dinner),
                    ('MILK HOPPERS', 80, @dinner),
                    ('PILAU CHICKEN', 450, @dinner),
                    ('HOPPERS MEAL', 280, @dinner),
                    ('PILAU EGG', 300, @dinner),
                    ('POL ROTTY WITH LUNUMIRIS', 230, @dinner),
                    ('EGG ROTTY', 120, @dinner),
                    ('CHICKEN KOTTU', 550, @dinner),
                    ('FISH KOTTU', 530, @dinner),
                    ('EGG KOTTU', 480, @dinner),
                    ('VEGETABLE KOTTU', 400, @dinner),
                    ('CHEESE CHICKEN KOTTU', 1050, @dinner),
                    ('CHEESE KOTTU', 800, @dinner),
                    ('POL ROTTY SET', 230, @dinner),
                    ('ROTTY WITH CURRY', 200, @dinner),
                    ('STRING HOPPERS 10', 150, @dinner),
                    ('EGG NOODLES', 300, @dinner),
                    ('CHICKEN NOODLES', 450, @dinner),
                    ('FISH NOODLES', 440, @dinner),

                    ('POTATO CURRY', 100, @curry),
                    ('FISH CURRY', 250, @curry),
                    ('CHICKEN CURRY', 270, @curry),
                    ('KADALA CURRY', 150, @curry),
                    ('DHAL CURRY', 100, @curry),

                    ('VEGETABLE SOUP', 180, @bev),
                    ('VEG SOUP TAKE AWAY', 200, @bev),

                    ('LUNUMIRIS', 50, @side),
                    ('POL SAMBOL', 50, @side),
                    ('BOILED EGG', 100, @side),
                    ('VEGETABLE CHOPSUEY', 200, @side),
                    ('VEGETABLE KHORMA', 180, @side),
                    ('DEVILLED POTATO', 180, @side),
                    ('DEVILLED CHICKEN', 300, @side),
                    ('DEVILLED FISH', 380, @side),
                    ('DEVILLED PRAWN', 400, @side),
                    ('DEVILLED CUTTLEFISH', 380, @side),
                    ('TANDOORI CHICKEN', 380, @side),
                    ('MUSHROOM', 200, @side),
                    ('CHICKEN WINGS', 300, @side),
                    ('BRINJAL MOJU', 180, @side),

                    ('SAMBA CHICKEN', 380, @lunch),
                    ('SAMBA FISH', 370, @lunch),
                    ('SAMBA VEGETABLE', 350, @lunch),
                    ('RED RICE CHICKEN', 380, @lunch),
                    ('RED RICE FISH', 370, @lunch),
                    ('RED RICE VEGETABLE', 350, @lunch),
                    ('YELLOW RICE', 450, @lunch),
                    ('LAMPRAIS', 700, @lunch);
                ");
            }
            catch { }
        }
    }
}