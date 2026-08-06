namespace InternetBankingApp.Models;

/// <summary>
/// Fila de la tabla de amortización. Es un objeto calculado en memoria: no se persiste, se
/// deriva del préstamo cada vez que se abre el detalle.
/// </summary>
public class CuotaAmortizacion
{
    public int Numero { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Cuota { get; set; }

    public decimal Capital { get; set; }

    public decimal Interes { get; set; }

    /// <summary>Capital que sigue debiéndose después de aplicar esta cuota.</summary>
    public decimal Balance { get; set; }

    public bool Pagada { get; set; }

    public bool EsProximaCuota { get; set; }

    public string NumeroTexto => $"Cuota {Numero}";

    public string EstadoTexto => Pagada ? "Pagada" : EsProximaCuota ? "Próxima" : "Pendiente";
}

/// <summary>Producto de préstamo del catálogo del banco, con su tasa anual fija.</summary>
public class ProductoPrestamo
{
    public required string Nombre { get; init; }

    public required decimal TasaAnual { get; init; }

    public required decimal MontoMaximo { get; init; }

    public override string ToString() => $"{Nombre} · {TasaAnual:0.##}%";
}
