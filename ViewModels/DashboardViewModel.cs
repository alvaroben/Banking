using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Controls;
using InternetBankingApp.Models;
using InternetBankingApp.Services;
using InternetBankingApp.Views;

namespace InternetBankingApp.ViewModels;

/// <summary>
/// Pantalla de inicio: resume la posición financiera del usuario combinando consultas agregadas de
/// SQLite (sumas y GROUP BY hechos por el motor) con el cálculo de la deuda vigente de los
/// préstamos. Además es el punto donde se dispara el motor de transferencias programadas.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;
    private readonly ProgramacionesService _programacionesService;

    public DashboardViewModel(BankingDataService dataService, ProgramacionesService programacionesService)
    {
        _dataService = dataService;
        _programacionesService = programacionesService;
    }

    /// <summary>Objeto que pinta el gráfico de barras; la página solo lo invalida al recargar.</summary>
    public GraficoBarrasDrawable GraficoDrawable { get; } = new();

    public ObservableCollection<TotalPorBeneficiario> TopBeneficiarios { get; } = [];

    public ObservableCollection<Cuenta> Cuentas { get; } = [];

    [ObservableProperty]
    private bool isCargando;

    [ObservableProperty]
    private decimal saldoTotal;

    [ObservableProperty]
    private decimal deudaPrestamos;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PatrimonioEsPositivo))]
    private decimal patrimonioNeto;

    [ObservableProperty]
    private decimal totalTransferido;

    [ObservableProperty]
    private decimal cuotaMensualComprometida;

    [ObservableProperty]
    private int cantidadCuentas;

    [ObservableProperty]
    private int cantidadBeneficiarios;

    [ObservableProperty]
    private int prestamosActivos;

    [ObservableProperty]
    private bool hayDatosGrafico;

    [ObservableProperty]
    private bool hayBeneficiarios;

    [ObservableProperty]
    private string mesMasActivo = string.Empty;

    public bool PatrimonioEsPositivo => PatrimonioNeto >= 0;

    /// <summary>
    /// Carga completa del dashboard. Antes de leer nada ejecuta las órdenes programadas vencidas,
    /// para que las cifras que se muestran ya incluyan esos movimientos.
    /// </summary>
    public async Task CargarAsync(bool ejecutarProgramaciones = true)
    {
        IsCargando = true;
        try
        {
            await _dataService.InicializarAsync();

            if (ejecutarProgramaciones)
            {
                var resultado = await _programacionesService.EjecutarPendientesAsync();
                if (resultado.HuboMovimiento)
                {
                    await Shell.Current.DisplayAlertAsync("Transferencias programadas", resultado.Mensaje, "Aceptar");
                }
            }

            var cuentas = await _dataService.ObtenerCuentasAsync();
            var prestamos = await _dataService.ObtenerPrestamosAsync();

            SaldoTotal = await _dataService.ObtenerSaldoTotalAsync();
            TotalTransferido = await _dataService.ObtenerTotalTransferidoAsync();
            CantidadCuentas = cuentas.Count;
            CantidadBeneficiarios = await _dataService.ContarBeneficiariosAsync();

            // La deuda no está guardada en ninguna columna: sale de la tabla de amortización de
            // cada préstamo, contando solo las cuotas que todavía no se han pagado.
            var vigentes = prestamos.Where(p => !p.EstaSaldado).ToList();
            PrestamosActivos = vigentes.Count;
            DeudaPrestamos = vigentes.Sum(p => p.SaldoPendiente);
            CuotaMensualComprometida = vigentes.Sum(p => p.CuotaMensual);
            PatrimonioNeto = SaldoTotal - DeudaPrestamos;

            Cuentas.Clear();
            foreach (var cuenta in cuentas)
            {
                Cuentas.Add(cuenta);
            }

            var totalesPorMes = await _dataService.ObtenerTotalesPorMesAsync();
            GraficoDrawable.Datos = totalesPorMes;
            HayDatosGrafico = totalesPorMes.Count > 0;
            MesMasActivo = totalesPorMes.Count == 0
                ? string.Empty
                : $"Mes con más movimiento: {totalesPorMes.OrderByDescending(m => m.Total).First().Etiqueta}";

            var top = await _dataService.ObtenerTopBeneficiariosAsync();
            TopBeneficiarios.Clear();
            foreach (var fila in top)
            {
                TopBeneficiarios.Add(fila);
            }

            HayBeneficiarios = TopBeneficiarios.Count > 0;
        }
        finally
        {
            IsCargando = false;
        }
    }

    [RelayCommand]
    private Task RefrescarAsync() => CargarAsync(ejecutarProgramaciones: true);

    [RelayCommand]
    private static Task IrACuentasAsync() => Shell.Current.GoToAsync($"//{nameof(CuentasPage)}");

    [RelayCommand]
    private static Task IrATransferenciasAsync() => Shell.Current.GoToAsync($"//{nameof(TransferenciasPage)}");

    [RelayCommand]
    private static Task IrAProgramadasAsync() => Shell.Current.GoToAsync($"//{nameof(ProgramadasPage)}");
}
