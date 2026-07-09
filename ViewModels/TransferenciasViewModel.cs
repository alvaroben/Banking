using System.Collections.ObjectModel;
using System.Globalization;
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
        }
    }

    [RelayCommand]
    private void ToggleForm() => IsFormVisible = !IsFormVisible;

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!Validar())
        {
            return;
        }

        var monto = decimal.Parse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture);
        var beneficiarioNombre = BeneficiarioDestinoSeleccionado!.Nombre;

        if (!_dataService.RegistrarTransferencia(CuentaOrigenSeleccionada!, BeneficiarioDestinoSeleccionado!, Concepto.Trim(), monto))
        {
            MontoError = "Fondos insuficientes en la cuenta seleccionada.";
            return;
        }

        LimpiarFormulario();

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
        else if (CuentaOrigenSeleccionada is not null && monto > CuentaOrigenSeleccionada.Saldo)
        {
            MontoError = "Fondos insuficientes en la cuenta seleccionada.";
            esValido = false;
        }

        return esValido;
    }

    private void LimpiarFormulario()
    {
        CuentaOrigenSeleccionada = null;
        BeneficiarioDestinoSeleccionado = null;
        Concepto = string.Empty;
        Monto = string.Empty;
        CuentaOrigenError = null;
        BeneficiarioDestinoError = null;
        ConceptoError = null;
        MontoError = null;
        IsFormVisible = false;
    }
}
