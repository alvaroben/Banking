namespace InternetBankingApp.Models;

public class Prestamo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Producto { get; set; } = string.Empty;
    public decimal MontoSolicitado { get; set; }
    public int PlazoMeses { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;
}
