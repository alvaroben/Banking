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
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(GuardarButtonText))]
    private Cuenta? cuentaEnEdicion;

    public bool IsEditMode => CuentaEnEdicion is not null;

    public string FormTitle => IsEditMode ? "Editar cuenta" : "Nueva cuenta";

    public string GuardarButtonText => IsEditMode ? "Guardar cambios" : "Solicitar cuenta";

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
    private void ToggleForm()
    {
        if (IsFormVisible)
        {
            IsFormVisible = false;
            LimpiarCampos();
        }
        else
        {
            LimpiarCampos();
            IsFormVisible = true;
        }
    }

    [RelayCommand]
    private void Editar(Cuenta cuenta)
    {
        CuentaEnEdicion = cuenta;
        TipoSeleccionado = cuenta.Tipo.ToString();
        Saldo = cuenta.Saldo.ToString(CultureInfo.InvariantCulture);
        TipoError = null;
        SaldoError = null;
        IsFormVisible = true;
    }

    [RelayCommand]
    private async Task EliminarAsync(Cuenta cuenta)
    {
        var confirmar = await Shell.Current.DisplayAlert(
            "Eliminar cuenta",
            $"¿Seguro que deseas eliminar la cuenta {cuenta.NumeroCuenta}? Esta acción no se puede deshacer.",
            "Eliminar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        _dataService.EliminarCuenta(cuenta);

        if (CuentaEnEdicion == cuenta)
        {
            IsFormVisible = false;
            LimpiarCampos();
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!Validar())
        {
            return;
        }

        if (IsEditMode)
        {
            var cuenta = CuentaEnEdicion!;
            cuenta.Tipo = Enum.Parse<TipoCuenta>(TipoSeleccionado!);
            cuenta.Saldo = decimal.Parse(Saldo, NumberStyles.Number, CultureInfo.InvariantCulture);

            IsFormVisible = false;
            LimpiarCampos();

            await Shell.Current.DisplayAlert(
                "Cuenta actualizada",
                $"Los datos de la cuenta {cuenta.NumeroCuenta} fueron actualizados.",
                "Aceptar");
            return;
        }

        IsBusy = true;
        await Task.Delay(3000);
        IsBusy = false;

        var nuevaCuenta = new Cuenta
        {
            NumeroCuenta = _dataService.GenerarNumeroCuenta(),
            Tipo = Enum.Parse<TipoCuenta>(TipoSeleccionado!),
            Saldo = decimal.Parse(Saldo, NumberStyles.Number, CultureInfo.InvariantCulture)
        };

        _dataService.AgregarCuenta(nuevaCuenta);

        IsFormVisible = false;
        LimpiarCampos();

        await Shell.Current.DisplayAlert(
            "Cuenta aprobada",
            $"Tu nueva cuenta {nuevaCuenta.NumeroCuenta} ha sido creada.",
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

    private void LimpiarCampos()
    {
        CuentaEnEdicion = null;
        TipoSeleccionado = null;
        Saldo = string.Empty;
        TipoError = null;
        SaldoError = null;
    }
}
