using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace InternetBankingApp.Models;

public enum FrecuenciaProgramacion
{
    Semanal,
    Quincenal,
    Mensual
}

/// <summary>
/// Orden permanente de transferencia. El motor de programaciones la ejecuta sola cada vez que
/// vence, sin que el usuario tenga que abrir el formulario de transferencias.
/// </summary>
[Table("TransferenciasProgramadas")]
public partial class TransferenciaProgramada : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Las tres columnas del índice compuesto forman la clave única de negocio: no tiene sentido
    /// tener dos órdenes idénticas (misma cuenta, mismo destino y mismo concepto) compitiendo por
    /// el mismo saldo. Un duplicado hace que SQLite lance SQLiteException con Result.Constraint.
    /// </summary>
    [ObservableProperty]
    [property: Indexed(Name = "IX_Programada_Unica", Order = 1, Unique = true)]
    private string cuentaOrigen = string.Empty;

    [ObservableProperty]
    [property: Indexed(Name = "IX_Programada_Unica", Order = 2, Unique = true)]
    private string beneficiarioDestino = string.Empty;

    [ObservableProperty]
    [property: Indexed(Name = "IX_Programada_Unica", Order = 3, Unique = true)]
    private string concepto = string.Empty;

    [ObservableProperty]
    private decimal monto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrecuenciaTexto))]
    [NotifyPropertyChangedFor(nameof(EstadoTexto))]
    private FrecuenciaProgramacion frecuencia;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoTexto))]
    private DateTime proximaEjecucion = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoTexto))]
    [NotifyPropertyChangedFor(nameof(AccionTexto))]
    private bool activa = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EjecucionesTexto))]
    private int ejecucionesRealizadas;

    /// <summary>Se llena cuando el motor tiene que pausar la orden (por ejemplo, fondos insuficientes).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstadoTexto))]
    private string? motivoPausa;

    [Ignore]
    public string FrecuenciaTexto => Frecuencia switch
    {
        FrecuenciaProgramacion.Semanal => "Cada semana",
        FrecuenciaProgramacion.Quincenal => "Cada 15 días",
        _ => "Cada mes"
    };

    [Ignore]
    public string EstadoTexto => Activa
        ? $"Activa · próxima el {ProximaEjecucion:dd/MM/yyyy}"
        : $"Pausada · {MotivoPausa ?? "detenida por el usuario"}";

    [Ignore]
    public string EjecucionesTexto => EjecucionesRealizadas == 1
        ? "1 ejecución realizada"
        : $"{EjecucionesRealizadas} ejecuciones realizadas";

    [Ignore]
    public string AccionTexto => Activa ? "Pausar" : "Reanudar";

    /// <summary>Avanza la fecha de la siguiente ocurrencia según la frecuencia configurada.</summary>
    public DateTime CalcularSiguienteFecha(DateTime desde) => Frecuencia switch
    {
        FrecuenciaProgramacion.Semanal => desde.AddDays(7),
        FrecuenciaProgramacion.Quincenal => desde.AddDays(15),
        _ => desde.AddMonths(1)
    };
}
