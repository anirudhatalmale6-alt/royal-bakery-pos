using Microsoft.EntityFrameworkCore;

namespace RoyalBakeryCashier.Data
{
    public partial class StockDbContext
    {
        /// <summary>
        /// Seeds menu categories and items from the Restaurant Items list.
        /// Clears and reloads all items on every run.
        /// </summary>
        public void SeedMenuData()
        {
            try
            {
                // Check if already seeded with new data
                var hasNewData = false;
                try { hasNewData = MenuCategories.Any(c => c.Name == "RICE"); } catch { }
                if (hasNewData) return; // Already seeded with restaurant items

                Database.ExecuteSqlRaw(@"
-- Restaurant Items - Auto-generated from RES Items.xlsx
-- Clears all existing items and reloads
DELETE FROM Stocks;
DELETE FROM GRNItems;
DELETE FROM GRNs;
DELETE FROM MenuItems;
DELETE FROM MenuCategories;

INSERT INTO MenuCategories (Name) VALUES (N'RICE');
INSERT INTO MenuCategories (Name) VALUES (N'DINNER');
INSERT INTO MenuCategories (Name) VALUES (N'CURRY');
INSERT INTO MenuCategories (Name) VALUES (N'BEVERAGES');
INSERT INTO MenuCategories (Name) VALUES (N'SIDE DISHES');
INSERT INTO MenuCategories (Name) VALUES (N'LUNCH PACK');

INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'SET MENU', 550.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'VEGETABLE FRIED RICE', 400.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'EGG FRIED RICE', 300.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHICKEN FRIED RICE', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'FISH FRIED RICE', 440.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'VEGETABLE BIRIYANI', 350.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHICKEN BIRIYANI', 550.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'EGG BIRIYANI', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'FISH BIRIYANI', 540.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'PRAWN FRIED RICE', 580.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CUTTLE FISH FRIED RICE', 580.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CUTTLE FISH SET MENU', 750.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'PRAWN SET MENU', 750.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'RICE'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'PARATA', 60.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'PLAIN HOPPERS', 50.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'HONEY HOPPERS', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'EGG HOPPERS', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'MILK HOPPERS', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'PILAU CHICKEN', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'HOPPERS MEAL', 280.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'PILAU EGG', 300.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'POL ROTTY WITH LUNUMIRIS', 230.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'EGG ROTTY', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHICKEN KOTTU', 550.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'FISH KOTTU', 530.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'EGG KOTTU', 480.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'VEGETABLE KOTTU', 400.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHEESE CHICKEN KOTTU', 1050.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHEESE KOTTU', 800.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'POL ROTTY SET', 230.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'ROTTY WITH CURRY', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'STRING HOPPERS 10', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'EGG NOODLES', 300.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHICKEN NOODLES', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'FISH NOODLES', 440.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DINNER'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'POTATO CURRY', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CURRY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'FISH CURRY', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CURRY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHICKEN CURRY', 270.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CURRY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'KADALA CURRY', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CURRY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'DHAL CURRY', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CURRY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'VEGETABLE SOUP', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'VEG SOUP TAKE AWAY', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'LUNUMIRIS', 50.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'POL SAMBOL', 50.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'BOILED EGG', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'VEGETABLE CHOPSUEY', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'VEGETABLE KHORMA', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'DEVILLED POTATO', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'DEVILLED CHICKEN', 300.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'DEVILLED FISH', 380.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'DEVILLED PRAWN', 400.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'DEVILLED CUTTLEFISH', 380.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'TANDOORI CHICKEN', 380.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'MUSHROOM', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'CHICKEN WINGS', 300.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'BRINJAL MOJU', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SIDE DISHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'SAMBA CHICKEN', 380.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'SAMBA FISH', 370.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'SAMBA VEGETABLE', 350.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'RED RICE CHICKEN', 380.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'RED RICE FISH', 370.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'RED RICE VEGETABLE', 350.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'YELLOW RICE', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'LAMPRAIS', 700.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'LUNCH PACK'), 0, 0);

-- Stocks with qty 10 for all items
INSERT INTO Stocks (MenuItemId, Quantity) SELECT Id, 10 FROM MenuItems;

-- Initial GRN
INSERT INTO GRNs (GRNNumber, DateTime, TotalItems, TotalQuantity) SELECT 'GRN-INIT', GETDATE(), COUNT(*), COUNT(*) * 10 FROM MenuItems;
INSERT INTO GRNItems (GRNId, MenuItemId, Quantity, CurrentQuantity, Price) SELECT (SELECT TOP 1 Id FROM GRNs ORDER BY Id DESC), Id, 10, 10, Price FROM MenuItems;
");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SeedMenuData error: {ex.Message}");
            }
        }
    }
}