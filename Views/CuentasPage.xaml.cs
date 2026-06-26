using System.Globalization;
using System.Linq;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.Views;

public partial class CuentasPage : ContentPage
{
    private readonly BankingDataService _dataService;
    private static readonly Random Random = new();

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
        var saldoTexto = SaldoEntry.Text?.Trim() ?? string.Empty;

        if (TipoPicker.SelectedIndex == -1 || string.IsNullOrEmpty(saldoTexto))
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
            NumeroCuenta = GenerarNumeroCuenta(),
            Tipo = Enum.Parse<TipoCuenta>((string)TipoPicker.SelectedItem),
            Saldo = saldo
        };

        _dataService.AgregarCuenta(cuenta);

        SaldoEntry.Text = string.Empty;
        TipoPicker.SelectedIndex = -1;
        FormBorder.IsVisible = false;
        ToggleFormButton.Text = "+ Solicitar cuenta";

        await DisplayAlertAsync("Cuenta aprobada", $"Tu nueva cuenta {cuenta.NumeroCuenta} ha sido creada.", "Aceptar");
    }

    private string GenerarNumeroCuenta()
    {
        string numero;
        do
        {
            numero = $"10{Random.Next(0, 100_000_000):D8}";
        } while (_dataService.Cuentas.Any(c => c.NumeroCuenta == numero));

        return numero;
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
