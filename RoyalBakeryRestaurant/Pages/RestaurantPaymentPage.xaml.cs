using RoyalBakeryCashier.Data;
using RoyalBakeryCashier.Data.Entities;
using RoyalBakeryCashier.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace RoyalBakeryRestaurant.Pages;

public partial class RestaurantPaymentPage : ContentPage
{
    private readonly decimal _total;
    private readonly List<CartItemData> _cartItems;
    private readonly string _orderSource;
    private readonly string _cashierName;

    private Entry _activeEntry;

    public RestaurantPaymentPage(decimal total, List<CartItemData> cartItems, string orderSource, string cashierName)
    {
        InitializeComponent();
        _total = total;
        _cartItems = cartItems;
        _orderSource = orderSource;
        _cashierName = cashierName;

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

        RestaurantSale sale = null;

        var db = new StockDbContext();
        var strategy = db.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                // Generate invoice number
                var lastSale = db.RestaurantSales
                    .OrderByDescending(s => s.Id).FirstOrDefault();
                int nextNum = (lastSale?.Id ?? 0) + 1;
                string invoiceNumber = $"RES-{nextNum:D5}";

                sale = new RestaurantSale
                {
                    InvoiceNumber = invoiceNumber,
                    DateTime = DateTime.Now,
                    TotalAmount = _total,
                    CashAmount = cash,
                    CardAmount = card,
                    ChangeGiven = change,
                    CashierName = _cashierName,
                    OrderSource = _orderSource,
                    Items = _cartItems.Select(ci => new RestaurantSaleItem
                    {
                        RestaurantItemId = ci.RestaurantItemId,
                        ItemName = ci.Name,
                        Quantity = ci.Quantity,
                        PricePerItem = ci.Price,
                        TotalPrice = ci.Total
                    }).ToList()
                };

                db.RestaurantSales.Add(sale);
                await db.SaveChangesAsync();
            });

            // Print invoice (outside strategy)
            if (sale != null)
                await PrintInvoice(sale, cash, card, change);

            // Print KOT for Pickme/Ubereats orders
            if (sale != null && (_orderSource == "Pickme Food" || _orderSource == "Ubereats"))
                await PrintKOT(sale);

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

    private async Task PrintInvoice(RestaurantSale sale, decimal cash, decimal card, decimal change)
    {
        const int W = 48;
        string Separator(char c = '-') => new string(c, W);
        string Row(string l, string r) => l + r.PadLeft(W - l.Length);

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

            // Header — centered
            Emit(center);
            Emit(enc.GetBytes("The Royal Bakery\n"));
            Emit(enc.GetBytes("RESTAURANT\n"));
            Emit(enc.GetBytes("202, Galle Road, Colombo-06\n"));
            Emit(enc.GetBytes("0112 500 991 / 0114 341 642\n"));
            Emit(enc.GetBytes("www.theroyalbakery.com\n"));
            Emit(enc.GetBytes(Separator('=') + "\n"));

            // Body — left aligned
            Emit(left);
            Emit(enc.GetBytes(Row("Invoice #:", sale.InvoiceNumber) + "\n"));
            Emit(enc.GetBytes(Row("Date:", sale.DateTime.ToString("dd/MM/yyyy HH:mm")) + "\n"));
            Emit(enc.GetBytes(Row("Source:", sale.OrderSource) + "\n"));
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

            // Footer — centered
            Emit(center);
            Emit(enc.GetBytes("Thank you!\n"));
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

    /// <summary>
    /// Print Kitchen Order Ticket (KOT) for Pickme Food / Ubereats orders.
    /// Uses separate KOT printer if configured, otherwise same printer.
    /// </summary>
    private async Task PrintKOT(RestaurantSale sale)
    {
        const int W = 48;
        string Separator(char c = '-') => new string(c, W);

        try
        {
            // Use KOT printer if configured, otherwise fall back to invoice printer
            string printerName = App.KOTPrinterName;
            if (string.IsNullOrEmpty(printerName))
            {
                printerName = Preferences.Get("ThermalPrinterName", "");
                if (string.IsNullOrEmpty(printerName))
                    printerName = RawPrinterHelper.FindThermalPrinter() ?? "";
            }
            if (string.IsNullOrEmpty(printerName)) return;

            var enc = Encoding.GetEncoding("IBM437");
            byte[] init = { 0x1B, 0x40 };
            byte[] center = { 0x1B, 0x61, 0x01 };
            byte[] left = { 0x1B, 0x61, 0x00 };
            byte[] bold_on = { 0x1B, 0x45, 0x01 };
            byte[] bold_off = { 0x1B, 0x45, 0x00 };
            byte[] dbl_on = { 0x1D, 0x21, 0x11 }; // double height+width
            byte[] dbl_off = { 0x1D, 0x21, 0x00 };
            byte[] feedCut = { 0x0A, 0x0A, 0x0A, 0x1D, 0x56, 0x41, 0x03 };

            using var ms = new MemoryStream();
            void Emit(byte[] b) => ms.Write(b, 0, b.Length);

            Emit(init);
            Emit(center);
            Emit(dbl_on);
            Emit(enc.GetBytes("** KOT **\n"));
            Emit(dbl_off);
            Emit(bold_on);
            Emit(enc.GetBytes($"[ {sale.OrderSource.ToUpper()} ]\n"));
            Emit(bold_off);
            Emit(enc.GetBytes(Separator('=') + "\n"));

            Emit(left);
            Emit(enc.GetBytes($"Order: {sale.InvoiceNumber}\n"));
            Emit(enc.GetBytes($"Time:  {sale.DateTime:HH:mm dd/MM}\n"));
            Emit(enc.GetBytes(Separator() + "\n\n"));

            Emit(bold_on);
            foreach (var item in sale.Items)
            {
                string line = $"{item.Quantity}x  {item.ItemName}\n";
                Emit(enc.GetBytes(line));
            }
            Emit(bold_off);

            Emit(enc.GetBytes("\n" + Separator() + "\n"));
            Emit(center);
            Emit(enc.GetBytes($"Items: {sale.Items.Sum(i => i.Quantity)}\n"));
            Emit(feedCut);

            RawPrinterHelper.SendBytesToPrinter(printerName, ms.ToArray());
        }
        catch { /* silent */ }
    }

    /// <summary>
    /// Data transfer object for cart items passed from the Restaurant page.
    /// </summary>
    public class CartItemData
    {
        public int RestaurantItemId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
