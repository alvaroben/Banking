using System.Globalization;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.Views;

public partial class MovimientosPage : ContentPage
{
    private readonly BankingDataService _dataService;

    public MovimientosPage(BankingDataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        TipoPicker.ItemsSource = Enum.GetNames<TipoMovimiento>();
        MovimientosList.ItemsSource = _dataService.Movimientos;
    }

    private void OnToggleFormClicked(object? sender, EventArgs e)
    {
        FormBorder.IsVisible = !FormBorder.IsVisible;
        ToggleFormButton.Text = FormBorder.IsVisible ? "Cancelar" : "+ Agregar movimiento";
    }

    private void OnGuardarClicked(object? sender, EventArgs e)
    {
        var concepto = ConceptoEntry.Text?.Trim() ?? string.Empty;
        var montoTexto = MontoEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(concepto) || TipoPicker.SelectedIndex == -1 || string.IsNullOrEmpty(montoTexto))
        {
            MostrarError("Todos los campos son obligatorios.");
            return;
        }

        if (!decimal.TryParse(montoTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto) || monto <= 0)
        {
            MostrarError("El monto debe ser un número válido.");
            return;
        }

        var movimiento = new Movimiento
        {
            Concepto = concepto,
            Tipo = Enum.Parse<TipoMovimiento>((string)TipoPicker.SelectedItem),
            Monto = monto
        };

        _dataService.AgregarMovimiento(movimiento);

        ConceptoEntry.Text = string.Empty;
        MontoEntry.Text = string.Empty;
        TipoPicker.SelectedIndex = -1;
        ErrorLabel.IsVisible = false;
        FormBorder.IsVisible = false;
        ToggleFormButton.Text = "+ Agregar movimiento";
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
