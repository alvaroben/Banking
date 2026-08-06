using SQLite;

namespace InternetBankingApp.Models;

/// <summary>
/// Cuota saldada de un préstamo. Guarda el desglose capital/interés del momento del pago para
/// que el historial no dependa de recalcular la tabla de amortización.
/// </summary>
[Table("PagosPrestamo")]
public class PagoPrestamo
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int PrestamoId { get; set; }

    public int NumeroCuota { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public decimal Monto { get; set; }

    public decimal CapitalPagado { get; set; }

    public decimal InteresPagado { get; set; }

    public string CuentaOrigen { get; set; } = string.Empty;
}
