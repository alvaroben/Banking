using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.ViewModels;

public partial class BeneficiariosViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;

    public BeneficiariosViewModel(BankingDataService dataService)
    {
        _dataService = dataService;
        Beneficiarios = _dataService.Beneficiarios;
    }

    public ObservableCollection<Beneficiario> Beneficiarios { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    private bool isFormVisible;

    public string ToggleButtonText => IsFormVisible ? "Cancelar" : "+ Agregar beneficiario";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(GuardarButtonText))]
    private Beneficiario? beneficiarioEnEdicion;

    public bool IsEditMode => BeneficiarioEnEdicion is not null;

    public string FormTitle => IsEditMode ? "Editar beneficiario" : "Nuevo beneficiario";

    public string GuardarButtonText => IsEditMode ? "Guardar cambios" : "Guardar";

    [ObservableProperty]
    private string nombre = string.Empty;

    [ObservableProperty]
    private string numeroCuenta = string.Empty;

    [ObservableProperty]
    private string banco = string.Empty;

    [ObservableProperty]
    private string? nombreError;

    [ObservableProperty]
    private string? numeroCuentaError;

    [ObservableProperty]
    private string? bancoError;

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
    private void Editar(Beneficiario beneficiario)
    {
        BeneficiarioEnEdicion = beneficiario;
        Nombre = beneficiario.Nombre;
        NumeroCuenta = beneficiario.NumeroCuenta;
        Banco = beneficiario.Banco;
        NombreError = null;
        NumeroCuentaError = null;
        BancoError = null;
        IsFormVisible = true;
    }

    [RelayCommand]
    private async Task EliminarAsync(Beneficiario beneficiario)
    {
        var confirmar = await Shell.Current.DisplayAlert(
            "Eliminar beneficiario",
            $"¿Seguro que deseas eliminar a {beneficiario.Nombre} de tu lista de beneficiarios?",
            "Eliminar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        _dataService.EliminarBeneficiario(beneficiario);

        if (BeneficiarioEnEdicion == beneficiario)
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
            var beneficiario = BeneficiarioEnEdicion!;
            beneficiario.Nombre = Nombre.Trim();
            beneficiario.NumeroCuenta = NumeroCuenta.Trim();
            beneficiario.Banco = Banco.Trim();

            IsFormVisible = false;
            LimpiarCampos();

            await Shell.Current.DisplayAlert(
                "Beneficiario actualizado",
                $"Los datos de {beneficiario.Nombre} fueron actualizados.",
                "Aceptar");
            return;
        }

        var nuevoBeneficiario = new Beneficiario
        {
            Nombre = Nombre.Trim(),
            NumeroCuenta = NumeroCuenta.Trim(),
            Banco = Banco.Trim()
        };

        _dataService.AgregarBeneficiario(nuevoBeneficiario);

        IsFormVisible = false;
        LimpiarCampos();

        await Shell.Current.DisplayAlert(
            "Beneficiario agregado",
            $"{nuevoBeneficiario.Nombre} fue agregado a tu lista de beneficiarios.",
            "Aceptar");
    }

    private bool Validar()
    {
        var esValido = true;

        NombreError = null;
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            NombreError = "El nombre es obligatorio.";
            esValido = false;
        }
        else if (Nombre.Trim().Length < 3)
        {
            NombreError = "El nombre debe tener al menos 3 caracteres.";
            esValido = false;
        }
        else if (!Nombre.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
        {
            NombreError = "El nombre solo puede contener letras y espacios.";
            esValido = false;
        }

        NumeroCuentaError = null;
        if (string.IsNullOrWhiteSpace(NumeroCuenta))
        {
            NumeroCuentaError = "El número de cuenta es obligatorio.";
            esValido = false;
        }
        else if (!NumeroCuenta.Trim().All(char.IsDigit))
        {
            NumeroCuentaError = "El número de cuenta solo debe contener dígitos.";
            esValido = false;
        }
        else if (NumeroCuenta.Trim().Length < 8 || NumeroCuenta.Trim().Length > 12)
        {
            NumeroCuentaError = "El número de cuenta debe tener entre 8 y 12 dígitos.";
            esValido = false;
        }
        else if (_dataService.Beneficiarios.Any(b => b.NumeroCuenta == NumeroCuenta.Trim() && b != BeneficiarioEnEdicion))
        {
            NumeroCuentaError = "Ya existe un beneficiario con ese número de cuenta.";
            esValido = false;
        }

        BancoError = null;
        if (string.IsNullOrWhiteSpace(Banco))
        {
            BancoError = "El banco es obligatorio.";
            esValido = false;
        }
        else if (Banco.Trim().Length < 3)
        {
            BancoError = "El nombre del banco debe tener al menos 3 caracteres.";
            esValido = false;
        }

        return esValido;
    }

    private void LimpiarCampos()
    {
        BeneficiarioEnEdicion = null;
        Nombre = string.Empty;
        NumeroCuenta = string.Empty;
        Banco = string.Empty;
        NombreError = null;
        NumeroCuentaError = null;
        BancoError = null;
    }
}
