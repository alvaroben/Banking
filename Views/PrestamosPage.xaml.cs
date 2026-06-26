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
        ToggleFormButton.Text = FormBorder.IsVisible ? "Cancelar" : "+ Solicitar préstamo";
    }

    private async void OnSolicitarClicked(object? sender, EventArgs e)
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

        ErrorLabel.IsVisible = false;

        FormFieldsLayout.IsEnabled = false;
        SolicitarButton.IsVisible = false;
        LoadingPanel.IsVisible = true;
        LoadingIndicator.Start();

        await Task.Delay(3000);

        LoadingIndicator.Stop();
        LoadingPanel.IsVisible = false;
        SolicitarButton.IsVisible = true;
        FormFieldsLayout.IsEnabled = true;

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
        FormBorder.IsVisible = false;
        ToggleFormButton.Text = "+ Solicitar préstamo";
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
