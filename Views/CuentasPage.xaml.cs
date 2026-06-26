using System.Globalization;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.Views;

public partial class CuentasPage : ContentPage
{
    private readonly BankingDataService _dataService;

    public CuentasPage(BankingDataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        TipoPicker.ItemsSource = Enum.GetNames<TipoCuenta>();
        CuentasList.ItemsSource = _dataService.Cuentas;
    }

    private void OnToggleFormClicked(object? sender, EventArgs e)
    {
        FormBorder.IsVisible = !FormBorder.IsVisible;
        ToggleFormButton.Text = FormBorder.IsVisible ? "Cancelar" : "+ Solicitar cuenta";
    }

    private async void OnSolicitarClicked(object? sender, EventArgs e)
    {
        var numeroCuenta = NumeroCuentaEntry.Text?.Trim() ?? string.Empty;
        var saldoTexto = SaldoEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(numeroCuenta) || TipoPicker.SelectedIndex == -1 || string.IsNullOrEmpty(saldoTexto))
        {
            MostrarError("Todos los campos son obligatorios.");
            return;
        }

        if (!decimal.TryParse(saldoTexto, NumberStyles.Number, CultureInfo.InvariantCulture, out var saldo) || saldo < 0)
        {
            MostrarError("El saldo inicial debe ser un número válido.");
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

        var cuenta = new Cuenta
        {
            NumeroCuenta = numeroCuenta,
            Tipo = Enum.Parse<TipoCuenta>((string)TipoPicker.SelectedItem),
            Saldo = saldo
        };

        _dataService.AgregarCuenta(cuenta);

        NumeroCuentaEntry.Text = string.Empty;
        SaldoEntry.Text = string.Empty;
        TipoPicker.SelectedIndex = -1;
        FormBorder.IsVisible = false;
        ToggleFormButton.Text = "+ Solicitar cuenta";
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
