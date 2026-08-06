using System.Globalization;

namespace InternetBankingApp.Models;

/// <summary>Fila que devuelve la consulta agregada de transferencias por mes (GROUP BY en SQL).</summary>
public class TotalPorMes
{
    /// <summary>Clave "yyyy-MM" tal como la produce strftime en SQLite.</summary>
    public string Mes { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int Cantidad { get; set; }

    /// <summary>
    /// Etiqueta corta del mes ("ene", "feb"...) para el eje del gráfico. Se fija el idioma en
    /// español para que no dependa de la configuración regional del dispositivo.
    /// </summary>
    public string Etiqueta =>
        DateTime.TryParse($"{Mes}-01", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha.ToString("MMM", CultureInfo.GetCultureInfo("es")).TrimEnd('.').ToLowerInvariant()
            : Mes;
}

/// <summary>Fila que devuelve la consulta agregada de gasto acumulado por beneficiario.</summary>
public class TotalPorBeneficiario
{
    public string Nombre { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int Cantidad { get; set; }

    public string CantidadTexto => Cantidad == 1 ? "1 transferencia" : $"{Cantidad} transferencias";
}
