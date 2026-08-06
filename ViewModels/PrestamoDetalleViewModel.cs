using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.ViewModels;

/// <summary>
/// Plan de pagos de un préstamo: tabla de amortización completa, resumen de costo y pago de
/// cuotas debitando una cuenta real del usuario.
/// </summary>
[QueryProperty(nameof(PrestamoId), "prestamoId")]
public partial class PrestamoDetalleViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;

    public PrestamoDetalleViewModel(BankingDataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>Lo asigna Shell a partir del parámetro de la ruta antes del OnAppearing.</summary>
    [ObservableProperty]
    private int prestamoId;

    public ObservableCollection<CuotaAmortizacion> Cuotas { get; } = [];
    public ObservableCollection<PagoPrestamo> Pagos { get; } = [];
    public ObservableCollection<Cuenta> Cuentas { get; } = [];

    private List<CuotaAmortizacion> _tablaCompleta = [];

    [ObservableProperty]
    private bool isCargando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titulo))]
    private Prestamo? prestamo;

    public string Titulo => Prestamo?.Producto ?? "Plan de pagos";

    [ObservableProperty]
    private Cuenta? cuentaSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoBotonPagar))]
    private CuotaAmortizacion? proximaCuota;

    [ObservableProperty]
    private bool puedePagar;

    [ObservableProperty]
    private bool estaSaldado;

    [ObservableProperty]
    private bool hayPagos;

    [ObservableProperty]
    private string? pagoError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoFiltro))]
    private bool mostrarSoloPendientes = true;

    [ObservableProperty]
    private decimal capitalPagado;

    [ObservableProperty]
    private decimal interesesPagados;

    [ObservableProperty]
    private double progreso;

    public string TextoFiltro => MostrarSoloPendientes ? "Ver todas las cuotas" : "Ver solo pendientes";

    public string TextoBotonPagar => ProximaCuota is null
        ? "Préstamo saldado"
        : $"Pagar cuota {ProximaCuota.Numero} · {ProximaCuota.Cuota:C2}";

    public async Task CargarAsync()
    {
        IsCargando = true;
        try
        {
            await _dataService.InicializarAsync();

            Prestamo = await _dataService.ObtenerPrestamoAsync(PrestamoId);
            if (Prestamo is null)
            {
                return;
            }

            _tablaCompleta = AmortizacionService.GenerarTabla(Prestamo);
            AplicarFiltro();

            var cuentas = await _dataService.ObtenerCuentasAsync();
            Cuentas.Clear();
            foreach (var cuenta in cuentas)
            {
                Cuentas.Add(cuenta);
            }

            // Si la cuenta elegida antes sigue existiendo, se conserva la selección tras recargar.
            CuentaSeleccionada = Cuentas.FirstOrDefault(c => c.Id == CuentaSeleccionada?.Id) ?? Cuentas.FirstOrDefault();

            var pagos = await _dataService.ObtenerPagosPrestamoAsync(Prestamo.Id);
            Pagos.Clear();
            foreach (var pago in pagos)
            {
                Pagos.Add(pago);
            }

            HayPagos = Pagos.Count > 0;
            CapitalPagado = pagos.Sum(p => p.CapitalPagado);
            InteresesPagados = pagos.Sum(p => p.InteresPagado);

            ProximaCuota = _tablaCompleta.FirstOrDefault(c => c.Numero == Prestamo.CuotasPagadas + 1);
            EstaSaldado = ProximaCuota is null;
            PuedePagar = !EstaSaldado && Cuentas.Count > 0;
            Progreso = Prestamo.ProgresoPago;
        }
        finally
        {
            IsCargando = false;
        }
    }

    [RelayCommand]
    private void AlternarFiltro()
    {
        MostrarSoloPendientes = !MostrarSoloPendientes;
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var filas = MostrarSoloPendientes
            ? _tablaCompleta.Where(c => !c.Pagada)
            : _tablaCompleta;

        Cuotas.Clear();
        foreach (var fila in filas)
        {
            Cuotas.Add(fila);
        }
    }

    [RelayCommand]
    private async Task PagarCuotaAsync()
    {
        PagoError = null;

        if (Prestamo is null || ProximaCuota is null)
        {
            return;
        }

        if (CuentaSeleccionada is null)
        {
            PagoError = "Selecciona la cuenta desde la que quieres pagar.";
            return;
        }

        var cuota = ProximaCuota;

        var confirmar = await Shell.Current.DisplayAlertAsync(
            $"Pagar cuota {cuota.Numero}",
            $"Se debitarán {cuota.Cuota:C2} de la cuenta {CuentaSeleccionada.NumeroCuenta}.\n\n" +
            $"Capital: {cuota.Capital:C2}\nInterés: {cuota.Interes:C2}",
            "Pagar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        var pagada = await _dataService.PagarCuotaPrestamoAsync(Prestamo, CuentaSeleccionada, cuota);

        if (!pagada)
        {
            PagoError = $"La cuenta {CuentaSeleccionada.NumeroCuenta} no tiene fondos suficientes para esta cuota.";
            return;
        }

        await CargarAsync();

        var mensaje = EstaSaldado
            ? $"¡Felicidades! Terminaste de pagar tu {Prestamo.Producto.ToLowerInvariant()}."
            : $"Cuota {cuota.Numero} pagada. Te quedan {Prestamo.PlazoMeses - Prestamo.CuotasPagadas} cuotas.";

        await Shell.Current.DisplayAlertAsync("Pago registrado", mensaje, "Aceptar");
    }

    [RelayCommand]
    private static Task VolverAsync() => Shell.Current.GoToAsync("..");
}
