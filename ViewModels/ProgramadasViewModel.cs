using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;
using SQLite;

namespace InternetBankingApp.ViewModels;

/// <summary>
/// Órdenes permanentes de transferencia. El usuario define qué se paga, desde qué cuenta y cada
/// cuánto; el motor las ejecuta solo cuando vencen.
/// </summary>
public partial class ProgramadasViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;
    private readonly ProgramacionesService _programacionesService;

    public ProgramadasViewModel(BankingDataService dataService, ProgramacionesService programacionesService)
    {
        _dataService = dataService;
        _programacionesService = programacionesService;
    }

    public ObservableCollection<TransferenciaProgramada> Programaciones { get; } = [];
    public ObservableCollection<Cuenta> Cuentas { get; } = [];
    public ObservableCollection<Beneficiario> Beneficiarios { get; } = [];

    public IReadOnlyList<string> Frecuencias { get; } =
    [
        "Cada semana",
        "Cada 15 días",
        "Cada mes"
    ];

    [ObservableProperty]
    private bool isCargando;

    [ObservableProperty]
    private bool puedeProgramar;

    [ObservableProperty]
    private bool requisitoVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    private bool isFormVisible;

    public string ToggleButtonText => IsFormVisible ? "Cancelar" : "+ Nueva orden programada";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(GuardarButtonText))]
    private TransferenciaProgramada? programacionEnEdicion;

    public bool IsEditMode => ProgramacionEnEdicion is not null;

    public string FormTitle => IsEditMode ? "Editar orden programada" : "Nueva orden programada";

    public string GuardarButtonText => IsEditMode ? "Guardar cambios" : "Programar";

    [ObservableProperty]
    private Cuenta? cuentaOrigenSeleccionada;

    [ObservableProperty]
    private Beneficiario? beneficiarioDestinoSeleccionado;

    [ObservableProperty]
    private string concepto = string.Empty;

    [ObservableProperty]
    private string monto = string.Empty;

    [ObservableProperty]
    private string? frecuenciaSeleccionada;

    [ObservableProperty]
    private DateTime primeraEjecucion = DateTime.Today;

    [ObservableProperty]
    private string? cuentaOrigenError;

    [ObservableProperty]
    private string? beneficiarioDestinoError;

    [ObservableProperty]
    private string? conceptoError;

    [ObservableProperty]
    private string? montoError;

    [ObservableProperty]
    private string? frecuenciaError;

    public async Task CargarAsync()
    {
        IsCargando = true;
        try
        {
            await _dataService.InicializarAsync();

            var programaciones = await _dataService.ObtenerProgramacionesAsync();
            var cuentas = await _dataService.ObtenerCuentasAsync();
            var beneficiarios = await _dataService.ObtenerBeneficiariosAsync();

            Programaciones.Clear();
            foreach (var programacion in programaciones)
            {
                Programaciones.Add(programacion);
            }

            Cuentas.Clear();
            foreach (var cuenta in cuentas)
            {
                Cuentas.Add(cuenta);
            }

            Beneficiarios.Clear();
            foreach (var beneficiario in beneficiarios)
            {
                Beneficiarios.Add(beneficiario);
            }

            PuedeProgramar = Cuentas.Count > 0 && Beneficiarios.Count > 0;
            RequisitoVisible = !PuedeProgramar;

            if (!PuedeProgramar)
            {
                IsFormVisible = false;
                LimpiarCampos();
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
    private void Editar(TransferenciaProgramada programacion)
    {
        ProgramacionEnEdicion = programacion;
        CuentaOrigenSeleccionada = Cuentas.FirstOrDefault(c => c.NumeroCuenta == programacion.CuentaOrigen);
        BeneficiarioDestinoSeleccionado = Beneficiarios.FirstOrDefault(b => b.Nombre == programacion.BeneficiarioDestino);
        Concepto = programacion.Concepto;
        Monto = programacion.Monto.ToString(CultureInfo.InvariantCulture);
        FrecuenciaSeleccionada = Frecuencias[(int)programacion.Frecuencia];
        PrimeraEjecucion = programacion.ProximaEjecucion;
        LimpiarErrores();
        IsFormVisible = true;
    }

    /// <summary>Pausa o reanuda la orden. Al reanudar, si la fecha quedó atrás, se corre a hoy.</summary>
    [RelayCommand]
    private async Task AlternarEstadoAsync(TransferenciaProgramada programacion)
    {
        programacion.Activa = !programacion.Activa;
        programacion.MotivoPausa = programacion.Activa ? null : "pausada por el usuario";

        if (programacion.Activa && programacion.ProximaEjecucion.Date < DateTime.Today)
        {
            programacion.ProximaEjecucion = DateTime.Today;
        }

        await _dataService.GuardarProgramacionAsync(programacion);
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync(TransferenciaProgramada programacion)
    {
        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Eliminar orden programada",
            $"¿Seguro que deseas eliminar la orden \"{programacion.Concepto}\" hacia {programacion.BeneficiarioDestino}? Las transferencias ya ejecutadas se conservan.",
            "Eliminar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        await _dataService.EliminarProgramacionAsync(programacion);

        if (ProgramacionEnEdicion == programacion)
        {
            IsFormVisible = false;
            LimpiarCampos();
        }

        await CargarAsync();
    }

    /// <summary>Fuerza una corrida del motor sin esperar a que el usuario vuelva al inicio.</summary>
    [RelayCommand]
    private async Task EjecutarAhoraAsync()
    {
        var resultado = await _programacionesService.EjecutarPendientesAsync();
        await CargarAsync();

        await Shell.Current.DisplayAlertAsync(
            "Órdenes programadas",
            resultado.HuboMovimiento ? resultado.Mensaje : "No hay órdenes vencidas por ejecutar hoy.",
            "Aceptar");
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!Validar())
        {
            return;
        }

        var monto = decimal.Parse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture);
        var frecuencia = (FrecuenciaProgramacion)Frecuencias.ToList().IndexOf(FrecuenciaSeleccionada!);

        // Igual que con los beneficiarios, se guarda una copia para que un rechazo por duplicado no
        // deje a medias la orden que se está mostrando en la lista.
        var candidata = new TransferenciaProgramada
        {
            Id = ProgramacionEnEdicion?.Id ?? 0,
            CuentaOrigen = CuentaOrigenSeleccionada!.NumeroCuenta,
            BeneficiarioDestino = BeneficiarioDestinoSeleccionado!.Nombre,
            Concepto = Concepto.Trim(),
            Monto = monto,
            Frecuencia = frecuencia,
            ProximaEjecucion = PrimeraEjecucion.Date,
            Activa = ProgramacionEnEdicion?.Activa ?? true,
            EjecucionesRealizadas = ProgramacionEnEdicion?.EjecucionesRealizadas ?? 0,
            MotivoPausa = ProgramacionEnEdicion?.MotivoPausa
        };

        try
        {
            await _dataService.GuardarProgramacionAsync(candidata);
        }
        catch (SQLiteException excepcion) when (excepcion.Result == SQLite3.Result.Constraint)
        {
            // Índice único compuesto (cuenta + beneficiario + concepto) definido en el modelo.
            ConceptoError = "Ya tienes una orden programada con ese concepto hacia ese beneficiario desde esa cuenta.";
            return;
        }

        var titulo = IsEditMode ? "Orden actualizada" : "Orden programada";
        var mensaje = IsEditMode
            ? $"La orden \"{candidata.Concepto}\" fue actualizada."
            : $"Se transferirán {candidata.Monto:C2} a {candidata.BeneficiarioDestino} {candidata.FrecuenciaTexto.ToLowerInvariant()}, a partir del {candidata.ProximaEjecucion:dd/MM/yyyy}.";

        IsFormVisible = false;
        LimpiarCampos();
        await CargarAsync();

        await Shell.Current.DisplayAlertAsync(titulo, mensaje, "Aceptar");
    }

    private bool Validar()
    {
        var esValido = true;
        LimpiarErrores();

        if (CuentaOrigenSeleccionada is null)
        {
            CuentaOrigenError = "Selecciona una cuenta de origen.";
            esValido = false;
        }

        if (BeneficiarioDestinoSeleccionado is null)
        {
            BeneficiarioDestinoError = "Selecciona un beneficiario destino.";
            esValido = false;
        }

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
            MontoError = $"La cuenta {CuentaOrigenSeleccionada.NumeroCuenta} solo tiene {CuentaOrigenSeleccionada.Saldo:C2} disponibles.";
            esValido = false;
        }

        if (string.IsNullOrEmpty(FrecuenciaSeleccionada))
        {
            FrecuenciaError = "Selecciona cada cuánto debe repetirse.";
            esValido = false;
        }

        return esValido;
    }

    private void LimpiarErrores()
    {
        CuentaOrigenError = null;
        BeneficiarioDestinoError = null;
        ConceptoError = null;
        MontoError = null;
        FrecuenciaError = null;
    }

    private void LimpiarCampos()
    {
        ProgramacionEnEdicion = null;
        CuentaOrigenSeleccionada = null;
        BeneficiarioDestinoSeleccionado = null;
        Concepto = string.Empty;
        Monto = string.Empty;
        FrecuenciaSeleccionada = null;
        PrimeraEjecucion = DateTime.Today;
        LimpiarErrores();
    }
}
