namespace InternetBankingApp.Models;

public enum TipoCuenta
{
    Ahorro,
    Corriente
}

public class Cuenta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NumeroCuenta { get; set; } = string.Empty;
    public TipoCuenta Tipo { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.Now;
}
