namespace InternetBankingApp.Models;

public class Transferencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CuentaOrigen { get; set; } = string.Empty;
    public string BeneficiarioDestino { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}
