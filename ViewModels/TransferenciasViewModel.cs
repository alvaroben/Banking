using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.ViewModels;

public partial class TransferenciasViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;

    public TransferenciasViewModel(BankingDataService dataService)
    {
        _dataService = dataService;
        Cuentas = _dataService.Cuentas;
        Beneficiarios = _dataService.Beneficiarios;
        Transferencias = _dataService.Transferencias;
    }

    public ObservableCollection<Cuenta> Cuentas { get; }
    public ObservableCollection<Beneficiario> Beneficiarios { get; }
    public ObservableCollection<Transferencia> Transferencias { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    private bool isFormVisible;

    public string ToggleButtonText => IsFormVisible ? "Cancelar" : "+ Agregar transferencia";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(GuardarButtonText))]
    private Transferencia? transferenciaEnEdicion;

    public bool IsEditMode => TransferenciaEnEdicion is not null;

    public string FormTitle => IsEditMode ? "Editar transferencia" : "Nueva transferencia";

    public string GuardarButtonText => IsEditMode ? "Guardar cambios" : "Transferir";

    [ObservableProperty]
    private bool puedeTransferir;

    [ObservableProperty]
    private bool requisitoVisible;

    [ObservableProperty]
    private Cuenta? cuentaOrigenSeleccionada;

    [ObservableProperty]
    private Beneficiario? beneficiarioDestinoSeleccionado;

    [ObservableProperty]
    private string concepto = string.Empty;

    [ObservableProperty]
    private string monto = string.Empty;

    [ObservableProperty]
    private string? cuentaOrigenError;

    [ObservableProperty]
    private string? beneficiarioDestinoError;

    [ObservableProperty]
    private string? conceptoError;

    [ObservableProperty]
    private string? montoError;

    public void ActualizarEstado()
    {
        PuedeTransferir = Cuentas.Count > 0 && Beneficiarios.Count > 0;
        RequisitoVisible = !PuedeTransferir;

        if (!PuedeTransferir)
        {
            IsFormVisible = false;
            LimpiarCampos();
        }
    }

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
    private void Editar(Transferencia transferencia)
    {
        TransferenciaEnEdicion = transferencia;
        CuentaOrigenSeleccionada = Cuentas.FirstOrDefault(c => c.NumeroCuenta == transferencia.CuentaOrigen);
        BeneficiarioDestinoSeleccionado = Beneficiarios.FirstOrDefault(b => b.Nombre == transferencia.BeneficiarioDestino);
        Concepto = transferencia.Concepto;
        Monto = transferencia.Monto.ToString(CultureInfo.InvariantCulture);
        CuentaOrigenError = null;
        BeneficiarioDestinoError = null;
        ConceptoError = null;
        MontoError = null;
        IsFormVisible = true;
    }

    [RelayCommand]
    private async Task EliminarAsync(Transferencia transferencia)
    {
        var confirmar = await Shell.Current.DisplayAlert(
            "Eliminar transferencia",
            $"¿Seguro que deseas eliminar esta transferencia de {transferencia.Monto:C2}? El monto será devuelto a la cuenta origen.",
            "Eliminar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        _dataService.EliminarTransferencia(transferencia);

        if (TransferenciaEnEdicion == transferencia)
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

        var monto = decimal.Parse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture);

        if (IsEditMode)
        {
            var transferencia = TransferenciaEnEdicion!;

            if (!_dataService.ActualizarTransferencia(transferencia, CuentaOrigenSeleccionada!, BeneficiarioDestinoSeleccionado!, Concepto.Trim(), monto))
            {
                MontoError = "Fondos insuficientes en la cuenta seleccionada.";
                return;
            }

            var beneficiarioActualizado = BeneficiarioDestinoSeleccionado!.Nombre;

            IsFormVisible = false;
            LimpiarCampos();

            await Shell.Current.DisplayAlert(
                "Transferencia actualizada",
                $"La transferencia a {beneficiarioActualizado} fue actualizada.",
                "Aceptar");
            return;
        }

        var beneficiarioNombre = BeneficiarioDestinoSeleccionado!.Nombre;

        if (!_dataService.RegistrarTransferencia(CuentaOrigenSeleccionada!, BeneficiarioDestinoSeleccionado!, Concepto.Trim(), monto))
        {
            MontoError = "Fondos insuficientes en la cuenta seleccionada.";
            return;
        }

        IsFormVisible = false;
        LimpiarCampos();

        await Shell.Current.DisplayAlert(
            "Transferencia realizada",
            $"Se transfirieron {monto:C2} a {beneficiarioNombre}.",
            "Aceptar");
    }

    private bool Validar()
    {
        var esValido = true;

        CuentaOrigenError = null;
        if (CuentaOrigenSeleccionada is null)
        {
            CuentaOrigenError = "Selecciona una cuenta de origen.";
            esValido = false;
        }

        BeneficiarioDestinoError = null;
        if (BeneficiarioDestinoSeleccionado is null)
        {
            BeneficiarioDestinoError = "Selecciona un beneficiario destino.";
            esValido = false;
        }

        ConceptoError = null;
        if (string.IsNullOrWhiteSpace(Concepto))
        {
            ConceptoError = "El concepto es obligatorio.";
            esValido = false;
        }
        else if (Concepto.Trim().Length < 3)
        {
            ConceptoError = "El concepto debe tener al menos 3 caracteres.";
            esValido = false;
        }

        MontoError = null;
        if (string.IsNullOrWhiteSpace(Monto))
        {
            MontoError = "El monto es obligatorio.";
            esValido = false;
        }
        else if (!decimal.TryParse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto))
        {
            MontoError = "El monto debe ser un número válido.";
            esValido = false;
        }
        else if (monto <= 0)
        {
            MontoError = "El monto debe ser mayor a cero.";
            esValido = false;
        }
        else if (CuentaOrigenSeleccionada is not null)
        {
            var saldoDisponible = CuentaOrigenSeleccionada.Saldo;
            if (IsEditMode && TransferenciaEnEdicion!.CuentaOrigen == CuentaOrigenSeleccionada.NumeroCuenta)
            {
                saldoDisponible += TransferenciaEnEdicion.Monto;
            }

            if (monto > saldoDisponible)
            {
                MontoError = "Fondos insuficientes en la cuenta seleccionada.";
                esValido = false;
            }
        }

        return esValido;
    }

    private void LimpiarCampos()
    {
        TransferenciaEnEdicion = null;
        CuentaOrigenSeleccionada = null;
        BeneficiarioDestinoSeleccionado = null;
        Concepto = string.Empty;
        Monto = string.Empty;
        CuentaOrigenError = null;
        BeneficiarioDestinoError = null;
        ConceptoError = null;
        MontoError = null;
    }
}
