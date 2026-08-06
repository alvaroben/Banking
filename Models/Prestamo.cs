using CommunityToolkit.Mvvm.ComponentModel;
using InternetBankingApp.Services;
using SQLite;

namespace InternetBankingApp.Models;

[Table("Prestamos")]
public partial class Prestamo : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ObservableProperty]
    private string producto = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CuotaMensual))]
    [NotifyPropertyChangedFor(nameof(TotalAPagar))]
    [NotifyPropertyChangedFor(nameof(TotalIntereses))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(ProgresoPago))]
    private decimal montoSolicitado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CuotaMensual))]
    [NotifyPropertyChangedFor(nameof(TotalAPagar))]
    [NotifyPropertyChangedFor(nameof(TotalIntereses))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(ProgresoPago))]
    [NotifyPropertyChangedFor(nameof(CuotasTexto))]
    private int plazoMeses;

    /// <summary>Tasa de interés anual fija que el banco asigna según el producto (ej. 18.5 = 18.5%).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CuotaMensual))]
    [NotifyPropertyChangedFor(nameof(TotalAPagar))]
    [NotifyPropertyChangedFor(nameof(TotalIntereses))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(TasaTexto))]
    private decimal tasaAnual;

    [ObservableProperty]
    private DateTime fechaSolicitud = DateTime.Now;

    /// <summary>Cuotas ya saldadas. Avanza cuando el usuario paga desde la pantalla de detalle.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(ProgresoPago))]
    [NotifyPropertyChangedFor(nameof(CuotasTexto))]
    [NotifyPropertyChangedFor(nameof(EstaSaldado))]
    private int cuotasPagadas;

    [Ignore]
    public decimal CuotaMensual => AmortizacionService.CalcularCuota(MontoSolicitado, TasaAnual, PlazoMeses);

    [Ignore]
    public decimal TotalAPagar => CuotaMensual * PlazoMeses;

    [Ignore]
    public decimal TotalIntereses => TotalAPagar - MontoSolicitado;

    [Ignore]
    public decimal SaldoPendiente => CuotaMensual * Math.Max(PlazoMeses - CuotasPagadas, 0);

    [Ignore]
    public double ProgresoPago => PlazoMeses == 0 ? 0 : (double)CuotasPagadas / PlazoMeses;

    [Ignore]
    public bool EstaSaldado => PlazoMeses > 0 && CuotasPagadas >= PlazoMeses;

    [Ignore]
    public string CuotasTexto => $"{CuotasPagadas}/{PlazoMeses} cuotas pagadas";

    [Ignore]
    public string TasaTexto => $"{TasaAnual:0.##}% anual";
}
