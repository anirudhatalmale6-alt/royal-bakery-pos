using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using RoyalBakeryCashier.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace RoyalBakeryCashier.Pages;

public partial class PaymentPage : ContentPage
{
    private readonly StockDbContext _db;
    private readonly decimal _total;
    private readonly List<PaymentCartItem> _cartItems;
    private readonly int? _salesOrderId;

    private Entry _activeEntry;

    public PaymentPage(decimal total, List<PaymentCartItem> cartItems, int? salesOrderId = null)
    {
        InitializeComponent();
        _db = new StockDbContext();
        _total = total;
        _cartItems = cartItems;
        _salesOrderId = salesOrderId;

        TotalLabel.Text = $"Rs. {_total:N2}";
        CashEntry.Text = ((int)_total).ToString();
        CardEntry.Text = "0";

        CashEntry.Focused += (s, e) => SetActiveEntry(CashEntry);
        CardEntry.Focused += (s, e) => SetActiveEntry(CardEntry);
        _activeEntry = CashEntry;

        UpdateBalance();
    }

    private void SetActiveEntry(Entry entry)
    {
        _activeEntry = entry;
        CashTabBtn.BackgroundColor = entry == CashEntry
            ? Color.FromArgb("#2196F3")
            : Color.FromArgb("#404040");
        CardTabBtn.BackgroundColor = entry == CardEntry
            ? Color.FromArgb("#2196F3")
            : Color.FromArgb("#404040");
    }

    private void CashTab_Clicked(object sender, EventArgs e)
    {
        SetActiveEntry(CashEntry);
        CashEntry.Focus();
    }

    private void CardTab_Clicked(object sender, EventArgs e)
    {
        SetActiveEntry(CardEntry);
        CardEntry.Focus();
    }

    private void AmountChanged(object sender, TextChangedEventArgs e)
    {
        UpdateBalance();
    }

    private void UpdateBalance()
    {
        decimal cash = decimal.TryParse(CashEntry.Text, out var c) ? c : 0;
        decimal card = decimal.TryParse(CardEntry.Text, out var d) ? d : 0;
        decimal paid = cash + card;
        decimal remaining = _total - paid;

        if (remaining > 0)
        {
            BalanceFrame.IsVisible = true;
            ChangeFrame.IsVisible = false;
            BalanceLabel.Text = $"Rs. {remaining:N2}";
            BalanceLabel.TextColor = Colors.OrangeRed;
            BalanceTitle.Text = "Remaining";
            ConfirmBtn.IsEnabled = false;
            ConfirmBtn.BackgroundColor = Color.FromArgb("#404040");
        }
        else if (remaining == 0)
        {
            BalanceFrame.IsVisible = true;
            ChangeFrame.IsVisible = false;
            BalanceLabel.Text = "Rs. 0.00";
            BalanceLabel.TextColor = Color.FromArgb("#4CAF50");
            BalanceTitle.Text = "Balance";
            ConfirmBtn.IsEnabled = true;
            ConfirmBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            BalanceFrame.IsVisible = false;
            ChangeFrame.IsVisible = true;
            ChangeLabel.Text = $"Rs. {Math.Abs(remaining):N2}";
            ConfirmBtn.IsEnabled = true;
            ConfirmBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        }
    }

    private void Keypad_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && _activeEntry != null && (_activeEntry.Text?.Length ?? 0) < 8)
            _activeEntry.Text = (_activeEntry.Text ?? "") + btn.Text;
    }

    private void ClearKeypad_Clicked(object sender, EventArgs e)
    {
        if (_activeEntry != null)
            _activeEntry.Text = string.Empty;
    }

    private void DeleteKeypad_Clicked(object sender, EventArgs e)
    {
        if (_activeEntry != null && !string.IsNullOrEmpty(_activeEntry.Text))
            _activeEntry.Text = _activeEntry.Text.Substring(0, _activeEntry.Text.Length - 1);
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void Confirm_Clicked(object sender, EventArgs e)
    {
        decimal cash = decimal.TryParse(CashEntry.Text, out var c) ? c : 0;
        decimal card = decimal.TryParse(CardEntry.Text, out var d) ? d : 0;

        if (cash + card < _total)
        {
            await DisplayAlert("Error", "Payment not enough. Balance must be zero before confirming.", "OK");
            return;
        }

        decimal change = (cash + card) - _total;
        if (change < 0) change = 0;

        Sale sale = null;

        var strategy = _db.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync();

                // 1. Deduct stock and GRN
                DeductStock();
                DeductGRN();

                // 2. Generate invoice number
                int nextNum = (_db.Sales.Any() ? _db.Sales.Max(s => s.Id) : 0) + 1;

                // 3. Create Sale record
                sale = new Sale
                {
                    DateTime = DateTime.Now,
                    TotalAmount = _total,
                    CashAmount = cash,
                    CardAmount = card,
                    ChangeGiven = change,
                    InvoiceNumber = $"INV-{nextNum:D5}",
                    CashierName = string.IsNullOrEmpty(App.LoggedInUserName) ? "Cashier" : App.LoggedInUserName,
                    Items = _cartItems.Select(i => new SaleItem
                    {
                        MenuItemId = i.MenuItemId,
                        ItemName = i.Name,
                        Quantity = i.Quantity,
                        PricePerItem = i.Price,
                        TotalPrice = i.Total
                    }).ToList()
                };
                _db.Sales.Add(sale);

                // 4. Mark SalesOrder as paid (if loaded from QR scan)
                if (_salesOrderId.HasValue)
                {
                    var salesOrder = _db.SalesOrders.Find(_salesOrderId.Value);
                    if (salesOrder != null)
                        salesOrder.Status = 1; // Paid
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            });

            // Print receipt (outside transaction)
            if (sale != null)
                await PrintToThermal(sale, cash, card, change);

            await Navigation.PopModalAsync();
        }
        catch (DbUpdateException dbEx)
        {
            string innerMsg = dbEx.InnerException != null ? dbEx.InnerException.Message : "No inner details";
            await DisplayAlert("Database Error", $"Message: {dbEx.Message}\nInner: {innerMsg}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void DeductStock()
    {
        foreach (var item in _cartItems)
        {
            var stock = _db.Stocks.First(s => s.MenuItemId == item.MenuItemId);
            stock.Quantity -= item.Quantity;
        }
    }

    private void DeductGRN()
    {
        foreach (var item in _cartItems)
            DeductFromGRN_FIFO(item.MenuItemId, item.Quantity);
    }

    private void DeductFromGRN_FIFO(int menuItemId, int qtyNeeded)
    {
        var grns = _db.GRNItems
            .Where(g => g.MenuItemId == menuItemId && g.CurrentQuantity > 0)
            .OrderBy(g => g.Id)
            .ToList();

        foreach (var grn in grns)
        {
            if (qtyNeeded <= 0) break;
            int deduct = Math.Min(grn.CurrentQuantity, qtyNeeded);
            grn.CurrentQuantity -= deduct;
            qtyNeeded -= deduct;
        }

        if (qtyNeeded > 0)
            throw new Exception("Insufficient GRN stock");
    }

    private async Task PrintToThermal(Sale sale, decimal cash, decimal card, decimal change)
    {
        const int W = 48;
        string Separator(char c = '-') => new string(c, W);
        string Row(string left, string right) => left + right.PadLeft(W - left.Length);

        try
        {
            string receiptDir = Path.Combine(FileSystem.AppDataDirectory, "receipts");
            Directory.CreateDirectory(receiptDir);
            await File.WriteAllTextAsync(
                Path.Combine(receiptDir, $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt"),
                $"Invoice: {sale.InvoiceNumber} | Total: Rs.{sale.TotalAmount:N2}");
        }
        catch { }

        try
        {
            string printerName = Preferences.Get("ThermalPrinterName", "");
            if (string.IsNullOrEmpty(printerName))
            {
                printerName = RawPrinterHelper.FindThermalPrinter() ?? "";
                if (!string.IsNullOrEmpty(printerName))
                    Preferences.Set("ThermalPrinterName", printerName);
            }

            if (string.IsNullOrEmpty(printerName))
            {
                var printers = RawPrinterHelper.GetInstalledPrinters();
                string msg = printers.Count > 0
                    ? $"No thermal printer detected.\n\nInstalled printers:\n{string.Join("\n", printers)}\n\nPlease set printer name in Settings."
                    : "No printers found. Please install the Epson TM-T82 driver.";
                await DisplayAlert("Printer Not Found", msg, "OK");
                return;
            }

            var enc = Encoding.GetEncoding("IBM437");
            byte[] init = { 0x1B, 0x40 };
            byte[] center = { 0x1B, 0x61, 0x01 };
            byte[] left = { 0x1B, 0x61, 0x00 };
            byte[] feedCut = { 0x0A, 0x0A, 0x0A, 0x1D, 0x56, 0x41, 0x03 };

            using var ms = new MemoryStream();
            void Emit(byte[] b) => ms.Write(b, 0, b.Length);

            Emit(init);

            Emit(center);
            Emit(enc.GetBytes("The Royal Bakery\n"));
            Emit(enc.GetBytes("202, Galle Road, Colombo-06\n"));
            Emit(enc.GetBytes("0112 500 991 / 0114 341 642\n"));
            Emit(enc.GetBytes("www.theroyalbakery.com\n"));
            Emit(enc.GetBytes(Separator('=') + "\n"));

            Emit(left);
            Emit(enc.GetBytes(Row("Invoice #:", sale.InvoiceNumber) + "\n"));
            Emit(enc.GetBytes(Row("Date:", sale.DateTime.ToString("dd/MM/yyyy HH:mm")) + "\n"));
            Emit(enc.GetBytes(Row("Cashier:", sale.CashierName ?? "Cashier") + "\n"));
            Emit(enc.GetBytes(Separator() + "\n"));

            foreach (var item in sale.Items)
            {
                Emit(enc.GetBytes(item.ItemName + "\n"));
                Emit(enc.GetBytes(Row($"  {item.Quantity} x {item.PricePerItem:N2}", $"{item.TotalPrice:N2}") + "\n"));
            }

            Emit(enc.GetBytes(Separator() + "\n"));
            Emit(enc.GetBytes(Row("Subtotal", $"Rs. {sale.TotalAmount:N2}") + "\n"));
            Emit(enc.GetBytes(Separator('=') + "\n"));
            Emit(enc.GetBytes(Row("TOTAL", $"Rs. {sale.TotalAmount:N2}") + "\n"));
            Emit(enc.GetBytes(Separator() + "\n"));

            if (cash > 0) Emit(enc.GetBytes(Row("Cash", $"Rs. {cash:N2}") + "\n"));
            if (card > 0) Emit(enc.GetBytes(Row("Card", $"Rs. {card:N2}") + "\n"));
            Emit(enc.GetBytes(Row("Change", $"Rs. {change:N2}") + "\n"));
            Emit(enc.GetBytes(Separator() + "\n"));

            Emit(center);
            Emit(enc.GetBytes("Thank you for your purchase!\n"));
            Emit(enc.GetBytes("Please come again\n"));
            Emit(enc.GetBytes(Separator('=') + "\n"));
            Emit(enc.GetBytes("Powered by EzyCode\n"));
            Emit(enc.GetBytes("www.ezycode.lk\n"));

            Emit(feedCut);

            bool printed = RawPrinterHelper.SendBytesToPrinter(printerName, ms.ToArray());
            if (!printed)
            {
                await DisplayAlert("Print Error",
                    $"Failed to send data to printer: {printerName}\nPlease check the printer is on and connected.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Print Error", $"Could not print: {ex.Message}", "OK");
        }
    }

    public class PaymentCartItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
