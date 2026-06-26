namespace InternetBankingApp.Models;

public enum TipoMovimiento
{
    Deposito,
    Retiro,
    Transferencia
}

public class Movimiento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Concepto { get; set; } = string.Empty;
    public TipoMovimiento Tipo { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}
