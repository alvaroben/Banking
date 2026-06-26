using System.Globalization;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.Views;

public partial class TransferenciasPage : ContentPage
{
    private readonly BankingDataService _dataService;

    public TransferenciasPage(BankingDataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;

        CuentaPicker.ItemsSource = _dataService.Cuentas;
        CuentaPicker.ItemDisplayBinding = new Binding(nameof(Cuenta.NumeroCuenta));

        BeneficiarioPicker.ItemsSource = _dataService.Beneficiarios;
        BeneficiarioPicker.ItemDisplayBinding = new Binding(nameof(Beneficiario.Nombre));

        TransferenciasList.ItemsSource = _dataService.Transferencias;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var puedeTransferir = _dataService.Cuentas.Count > 0 && _dataService.Beneficiarios.Count > 0;
        RequisitoLabel.IsVisible = !puedeTransferir;
        ToggleFormButton.IsEnabled = puedeTransferir;

        if (!puedeTransferir)
        {
            FormBorder.IsVisible = false;
            ToggleFormButton.Text = "+ Agregar transferencia";
        }
    }

    private void OnToggleFormClicked(object? sender, EventArgs e)
    {
        FormBorder.IsVisible = !FormBorder.IsVisible;
        ToggleFormButton.Text = FormBorder.IsVisible ? "Cancelar" : "+ Agregar transferencia";
    }

    private void OnGuardarClicked(object? sender, EventArgs e)
    {
        var concepto = ConceptoEntry.Text?.Trim() ?? string.Empty;
        var montoTexto = MontoEntry.Text?.Trim() ?? string.Empty;
        var cuentaOrigen = CuentaPicker.SelectedItem as Cuenta;
        var beneficiarioDestino = BeneficiarioPicker.SelectedItem as Beneficiario;

        if (cuentaOrigen is null || beneficiarioDestino is null || string.IsNullOrEmpty(concepto) || string.IsNullOrEmpty(montoTexto))
        {
            MostrarError("Todos los campos son obligatorios.");
            return;
        }

        if (!decimal.TryParse(montoTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto) || monto <= 0)
        {
            MostrarError("El monto debe ser un número válido.");
            return;
        }

        if (!_dataService.RegistrarTransferencia(cuentaOrigen, beneficiarioDestino, concepto, monto))
        {
            MostrarError("Fondos insuficientes en la cuenta seleccionada.");
            return;
        }

        ConceptoEntry.Text = string.Empty;
        MontoEntry.Text = string.Empty;
        CuentaPicker.SelectedItem = null;
        BeneficiarioPicker.SelectedItem = null;
        ErrorLabel.IsVisible = false;
        FormBorder.IsVisible = false;
        ToggleFormButton.Text = "+ Agregar transferencia";
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
