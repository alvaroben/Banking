using System.Globalization;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.Views;

public partial class PrestamosPage : ContentPage
{
    private readonly BankingDataService _dataService;

    public PrestamosPage(BankingDataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        PrestamosList.ItemsSource = _dataService.Prestamos;
    }

    private void OnToggleFormClicked(object? sender, EventArgs e)
    {
        FormBorder.IsVisible = !FormBorder.IsVisible;
        ToggleFormButton.Text = FormBorder.IsVisible ? "Cancelar" : "+ Agregar préstamo";
    }

    private void OnGuardarClicked(object? sender, EventArgs e)
    {
        var producto = ProductoEntry.Text?.Trim() ?? string.Empty;
        var montoTexto = MontoEntry.Text?.Trim() ?? string.Empty;
        var plazoTexto = PlazoEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(producto) || string.IsNullOrEmpty(montoTexto) || string.IsNullOrEmpty(plazoTexto))
        {
            MostrarError("Todos los campos son obligatorios.");
            return;
        }

        if (!decimal.TryParse(montoTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto) || monto <= 0)
        {
            MostrarError("El monto solicitado debe ser un número válido.");
            return;
        }

        if (!int.TryParse(plazoTexto, out var plazo) || plazo <= 0)
        {
            MostrarError("El plazo debe ser un número entero válido.");
            return;
        }

        var prestamo = new Prestamo
        {
            Producto = producto,
            MontoSolicitado = monto,
            PlazoMeses = plazo
        };

        _dataService.AgregarPrestamo(prestamo);

        ProductoEntry.Text = string.Empty;
        MontoEntry.Text = string.Empty;
        PlazoEntry.Text = string.Empty;
        ErrorLabel.IsVisible = false;
        FormBorder.IsVisible = false;
        ToggleFormButton.Text = "+ Agregar préstamo";
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
