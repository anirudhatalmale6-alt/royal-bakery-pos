using Microsoft.EntityFrameworkCore;

namespace RoyalBakeryCashier.Data
{
    public partial class StockDbContext
    {
        /// <summary>
        /// Seeds menu categories and items from the Royal Bakery Shop price list.
        /// Clears and reloads all items.
        /// </summary>
        public void SeedMenuData()
        {
            try
            {
                // Check if already seeded with bakery data
                var hasNewData = false;
                try { hasNewData = MenuCategories.Any(c => c.Name == "CAKES"); } catch { }
                if (hasNewData) return;

                Database.ExecuteSqlRaw(@"
-- Royal Bakery Shop Items - Auto-generated from RB Shop.xlsx
-- Clears all existing items and reloads
DELETE FROM Stocks;
DELETE FROM GRNItems;
DELETE FROM GRNs;
DELETE FROM MenuItems;
DELETE FROM MenuCategories;

INSERT INTO MenuCategories (Name) VALUES (N'CAKES');
INSERT INTO MenuCategories (Name) VALUES (N'GATEAU''X');
INSERT INTO MenuCategories (Name) VALUES (N'SPECIALITY CAKES');
INSERT INTO MenuCategories (Name) VALUES (N'DESSERTS');
INSERT INTO MenuCategories (Name) VALUES (N'SWEET PATISSERIE / CAKE PCS');
INSERT INTO MenuCategories (Name) VALUES (N'BISCUITS');
INSERT INTO MenuCategories (Name) VALUES (N'COOKIES');
INSERT INTO MenuCategories (Name) VALUES (N'BEVERAGES');
INSERT INTO MenuCategories (Name) VALUES (N'BREAD');
INSERT INTO MenuCategories (Name) VALUES (N'BUNS');
INSERT INTO MenuCategories (Name) VALUES (N'CROISSANT');
INSERT INTO MenuCategories (Name) VALUES (N'FRIED ROLLS');
INSERT INTO MenuCategories (Name) VALUES (N'ROTTY');
INSERT INTO MenuCategories (Name) VALUES (N'PATTY');
INSERT INTO MenuCategories (Name) VALUES (N'CUTLET');
INSERT INTO MenuCategories (Name) VALUES (N'SAVOURY PASTRIES/WRAPS');
INSERT INTO MenuCategories (Name) VALUES (N'SRI LANKAN SWEETS');
INSERT INTO MenuCategories (Name) VALUES (N'SANDWICHES');

INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Butter Cake 325grms', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Butter Cake 01Kg', 1300.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Ribbon Cake 400grms', 900.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Ribbon Cake 1Kg', 1500.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Ribbon Cake 2Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Cake 400grms', 950.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Cake 1Kg', 1600.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Cake 2Kg', 3200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Coffee Cake 1Kg', 1700.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Eggless Date Cake 1Kg', 1750.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Eggless Chocolate Cake 500g', 600.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Eggless Icing Cake 1Kg', 1700.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Swiss Roll - Vanilla', 400.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Swiss Roll - Chocolate', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fudge Brownie Cake', 1200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Black Forest Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Pineapple Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Coffee Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Naugat Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Pyramid Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Swiss Roll Pc', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fudge Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Mousse Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Mixed Fruit Gateaux', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Black Forest 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Coffee Gateaux 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Pineapple Gateaux 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Nougat Gateaux 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Mixed Fruit Gateaux 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Caramel Gateaux 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Gateaux 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Mousse Gateaux 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Fudge 1Kg', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'GATEAU''X'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Rich Cake 500grms', 2000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Rich Cake 1Kg', 4000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Birthday Cake Willton Pan', 4250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Eggless Birthday Cake Willton Pan', 4250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Up Right Specialty Cake Willton Pan', 4250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Round Shape Cake (Small)', 1800.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Round Shape Cake (Large)', 3500.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Love Cake 500g', 1700.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Tall Cake', 3500.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Design Cake 1', 3000.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Design Cake 2', 4500.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SPECIALITY CAKES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cake Pudding', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cream Caramel', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Bread & Butter Pudding', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fruit Trifle', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Biscuit Pudding', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Mousse', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Lava Cake', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Watalappam', 220.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Lava Cake', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Brownie', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Jaggery pudding', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cheese Cake', 650.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Salted caramel', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Falooda Jelly Pudding', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'DESSERTS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Butter Cake', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Ribbon Cake', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Cake', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Coffee Cake', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vanilla Muffin', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Muffin', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Chip Muffin', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vanilla Cupcake', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Cupcake', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Mud Cake', 140.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Éclairs', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Mousse Éclairs (Jumbo)', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cream Doughnut', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Doughnut', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Mousse Doughnut', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Apple Cake', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Bakewell Tart', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cream Puff', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Rum Ball', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Mixed Fruit Tart', 220.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Wedding Cake', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Apple Pie', 350.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Carrot Cake', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Blueberry Chocolate Cake', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Red Velvet', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Rainbow Cake', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Red Velvet Swiss Roll Pcs', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Dodol', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Laddu', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Brownie Cake Pc', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Apple Crumble', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Lamingtons', 170.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SWEET PATISSERIE / CAKE PCS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Gnanakatha Packet', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Butter Rusk Packet', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Finger Biscuit (Ginger) Packet', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Rulan Biscuit Packet', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Wine Biscuit Packet', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Sawborrow Packet', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Baby Rusk Packet', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Bread Crumbs 500grms', 350.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BISCUITS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Coconut Macaroons', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'COOKIES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Kisses Packet', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'COOKIES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Chip Cookie', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'COOKIES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Butter Cookies Packet', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'COOKIES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Coconut Toffee', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'COOKIES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Milk Toffee', 300.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'COOKIES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Date & Cashewnut bar', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'COOKIES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'NesTea', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Nescafe', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Hot Milo', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Ice Milo', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Ice Coffee', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Ice Faluda', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BEVERAGES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Loaf of Bread', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Sandwich Bread (Small)', 180.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Jumbo Sandwich Bread', 350.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Roast Bread (Large)', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Brown Bread', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Butter Bread', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Currant Bread', 290.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Naan Bread', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Banana Bread', 400.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'French Bread (Baguettes)', 290.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BREAD'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Dinner Bun (½)', 50.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fancy Dinner Bun with Sesame Seed', 50.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Hot-Dog Bun/Burger Bun', 50.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Tea Bun', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Kimbula Bun', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fruit Bun', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cream Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable & Cheese Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Seeni Sambol Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg & Seeni Sambol Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Seeni Sambol Bread', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg Curry Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Omelet Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Bun', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Pathira', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Hot-Dogs', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Prawn Bun', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg Pizza', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cheese & Chicken Bun', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Sausage Bun', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Devilled Chicken Bun', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Burger', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'BUNS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Plain Croissant', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CROISSANT'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chocolate Croissant', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CROISSANT'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Croissant', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CROISSANT'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Sausage Croissant', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CROISSANT'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Chinese Roll', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg Chinese Roll', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Chinese Roll', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Chinese Roll', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cheese & Sausage Chinese Roll', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Samosa', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Samosa', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Spring Roll', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'FRIED ROLLS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Rotty', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'ROTTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg Rotty', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'ROTTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Rotty', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'ROTTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Rotty', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'ROTTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Naan Rotty', 140.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'ROTTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Naan Rotty', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'ROTTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Pol Rotty with Katta Sambol', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'ROTTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Patty', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'PATTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Patty', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'PATTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Patty', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'PATTY'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Cutlet', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CUTLET'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg Cutlet', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CUTLET'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Cutlet', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CUTLET'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Cutlet', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'CUTLET'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Muffin', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Pastry', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Pastry', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg & Seeni Sambol Pastry', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Pastry', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Pie', 110.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Pie', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Savoury Chicken Doughnut', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Pizza', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Pizza', 150.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Quiche Lorraine', 120.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Danish Pastry', 200.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Lasagna', 450.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SAVOURY PASTRIES/WRAPS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Konda Kavum', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Mung Kavum', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Kokis', 50.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Athiraha', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Aasmi', 140.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Halapa', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Aluwa', 80.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Kiribath with Lunumiris', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SRI LANKAN SWEETS'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Seeni Sambol Sandwich', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Fish Sandwich', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Vegetable Sandwich (3 Line)', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Egg Sandwich', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Sandwich', 90.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Focaccia Sandwich', 130.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Chicken Submarine', 250.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);
INSERT INTO MenuItems (Name, Price, MenuCategoryId, IsQuick, QuickCategory) VALUES (N'Cheese Sandwich', 100.00, (SELECT TOP 1 Id FROM MenuCategories WHERE Name = N'SANDWICHES'), 0, 0);

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