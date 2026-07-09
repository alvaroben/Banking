using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.ViewModels;

public partial class CuentasViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;

    public CuentasViewModel(BankingDataService dataService)
    {
        _dataService = dataService;
        Cuentas = _dataService.Cuentas;
    }

    public ObservableCollection<Cuenta> Cuentas { get; }

    public IReadOnlyList<string> TiposCuenta { get; } = Enum.GetNames<TipoCuenta>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    private bool isFormVisible;

    public string ToggleButtonText => IsFormVisible ? "Cancelar" : "+ Solicitar cuenta";

    [ObservableProperty]
    private string? tipoSeleccionado;

    [ObservableProperty]
    private string saldo = string.Empty;

    [ObservableProperty]
    private string? tipoError;

    [ObservableProperty]
    private string? saldoError;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private void ToggleForm() => IsFormVisible = !IsFormVisible;

    [RelayCommand]
    private async Task SolicitarAsync()
    {
        if (!Validar())
        {
            return;
        }

        IsBusy = true;
        await Task.Delay(3000);
        IsBusy = false;

        var cuenta = new Cuenta
        {
            NumeroCuenta = _dataService.GenerarNumeroCuenta(),
            Tipo = Enum.Parse<TipoCuenta>(TipoSeleccionado!),
            Saldo = decimal.Parse(Saldo, NumberStyles.Number, CultureInfo.InvariantCulture)
        };

        _dataService.AgregarCuenta(cuenta);

        LimpiarFormulario();

        await Shell.Current.DisplayAlert(
            "Cuenta aprobada",
            $"Tu nueva cuenta {cuenta.NumeroCuenta} ha sido creada.",
            "Aceptar");
    }

    private bool Validar()
    {
        var esValido = true;

        TipoError = null;
        if (string.IsNullOrEmpty(TipoSeleccionado))
        {
            TipoError = "Selecciona un tipo de cuenta.";
            esValido = false;
        }

        SaldoError = null;
        if (string.IsNullOrWhiteSpace(Saldo))
        {
            SaldoError = "El saldo inicial es obligatorio.";
            esValido = false;
        }
        else if (!decimal.TryParse(Saldo, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto))
        {
            SaldoError = "El saldo inicial debe ser un número válido.";
            esValido = false;
        }
        else if (monto < 0)
        {
            SaldoError = "El saldo inicial no puede ser negativo.";
            esValido = false;
        }
        else if (monto < 100)
        {
            SaldoError = "El saldo inicial debe ser de al menos RD$100.00.";
            esValido = false;
        }

        return esValido;
    }

    private void LimpiarFormulario()
    {
        TipoSeleccionado = null;
        Saldo = string.Empty;
        TipoError = null;
        SaldoError = null;
        IsFormVisible = false;
    }
}
