namespace InternetBankingApp.Models;

public class Beneficiario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string NumeroCuenta { get; set; } = string.Empty;
    public string Banco { get; set; } = string.Empty;
}
