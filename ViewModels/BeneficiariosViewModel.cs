using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;
using SQLite;

namespace InternetBankingApp.ViewModels;

public partial class BeneficiariosViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;

    public BeneficiariosViewModel(BankingDataService dataService)
    {
        _dataService = dataService;
    }

    public ObservableCollection<Beneficiario> Beneficiarios { get; } = [];

    [ObservableProperty]
    private bool isCargando;

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

    /// <summary>
    /// Punto de entrada desde el OnAppearing de la página: inicializa la base de datos de forma
    /// perezosa (solo la primera vez cuesta algo) y trae los beneficiarios guardados.
    /// </summary>
    public async Task CargarAsync()
    {
        IsCargando = true;
        try
        {
            await _dataService.InicializarAsync();

            var beneficiarios = await _dataService.ObtenerBeneficiariosAsync();

            Beneficiarios.Clear();
            foreach (var beneficiario in beneficiarios)
            {
                Beneficiarios.Add(beneficiario);
            }
        }
        finally
        {
            IsCargando = false;
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
        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Eliminar beneficiario",
            $"¿Seguro que deseas eliminar a {beneficiario.Nombre} de tu lista de beneficiarios?",
            "Eliminar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        await _dataService.EliminarBeneficiarioAsync(beneficiario);
        Beneficiarios.Remove(beneficiario);

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

        // Se guarda sobre una copia: si la base rechaza el número de cuenta por duplicado, el
        // beneficiario que se está mostrando en la lista queda intacto.
        var candidato = new Beneficiario
        {
            Id = BeneficiarioEnEdicion?.Id ?? 0,
            Nombre = Nombre.Trim(),
            NumeroCuenta = NumeroCuenta.Trim(),
            Banco = Banco.Trim()
        };

        try
        {
            await _dataService.GuardarBeneficiarioAsync(candidato);
        }
        catch (SQLiteException excepcion) when (excepcion.Result == SQLite3.Result.Constraint)
        {
            // La unicidad la garantiza la base de datos (columna NumeroCuenta marcada con [Unique]),
            // no una comprobación en memoria: el mensaje aparece inline, debajo del campo.
            NumeroCuentaError = "Ya existe un beneficiario registrado con ese número de cuenta.";
            return;
        }

        var mensaje = IsEditMode
            ? $"Los datos de {candidato.Nombre} fueron actualizados."
            : $"{candidato.Nombre} fue agregado a tu lista de beneficiarios.";
        var titulo = IsEditMode ? "Beneficiario actualizado" : "Beneficiario agregado";

        IsFormVisible = false;
        LimpiarCampos();
        await CargarAsync();

        await Shell.Current.DisplayAlertAsync(titulo, mensaje, "Aceptar");
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
