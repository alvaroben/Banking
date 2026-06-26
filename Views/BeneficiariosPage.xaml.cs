using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.Views;

public partial class BeneficiariosPage : ContentPage
{
    private readonly BankingDataService _dataService;

    public BeneficiariosPage(BankingDataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BeneficiariosList.ItemsSource = _dataService.Beneficiarios;
    }

    private void OnToggleFormClicked(object? sender, EventArgs e)
    {
        FormBorder.IsVisible = !FormBorder.IsVisible;
        ToggleFormButton.Text = FormBorder.IsVisible ? "Cancelar" : "+ Agregar beneficiario";
    }

    private void OnGuardarClicked(object? sender, EventArgs e)
    {
        var nombre = NombreEntry.Text?.Trim() ?? string.Empty;
        var numeroCuenta = NumeroCuentaEntry.Text?.Trim() ?? string.Empty;
        var banco = BancoEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(numeroCuenta) || string.IsNullOrEmpty(banco))
        {
            MostrarError("Todos los campos son obligatorios.");
            return;
        }

        var beneficiario = new Beneficiario
        {
            Nombre = nombre,
            NumeroCuenta = numeroCuenta,
            Banco = banco
        };

        _dataService.AgregarBeneficiario(beneficiario);

        NombreEntry.Text = string.Empty;
        NumeroCuentaEntry.Text = string.Empty;
        BancoEntry.Text = string.Empty;
        ErrorLabel.IsVisible = false;
        FormBorder.IsVisible = false;
        ToggleFormButton.Text = "+ Agregar beneficiario";
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
