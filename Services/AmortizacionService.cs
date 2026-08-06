using InternetBankingApp.Models;

namespace InternetBankingApp.Services;

/// <summary>
/// Matemática financiera del módulo de préstamos: catálogo de productos con su tasa, cuota por el
/// sistema francés (cuota fija) y construcción de la tabla de amortización completa.
/// Es cálculo puro, sin dependencias de base de datos ni de UI, para poder razonarlo y probarlo aparte.
/// </summary>
public static class AmortizacionService
{
    /// <summary>Productos que ofrece el banco. La tasa la fija el producto, no el usuario.</summary>
    public static IReadOnlyList<ProductoPrestamo> Catalogo { get; } =
    [
        new() { Nombre = "Préstamo personal", TasaAnual = 18.50m, MontoMaximo = 500_000m },
        new() { Nombre = "Préstamo de vehículo", TasaAnual = 12.90m, MontoMaximo = 1_500_000m },
        new() { Nombre = "Préstamo hipotecario", TasaAnual = 9.75m, MontoMaximo = 8_000_000m },
        new() { Nombre = "Préstamo educativo", TasaAnual = 8.50m, MontoMaximo = 750_000m },
        new() { Nombre = "Préstamo comercial", TasaAnual = 15.25m, MontoMaximo = 3_000_000m }
    ];

    public static ProductoPrestamo? BuscarProducto(string? nombre) =>
        Catalogo.FirstOrDefault(p => p.Nombre == nombre);

    /// <summary>
    /// Cuota fija mensual por el sistema francés: C = P · i / (1 − (1 + i)^−n), donde i es la tasa
    /// mensual y n el plazo en meses. Con tasa cero degenera en el capital repartido en n cuotas.
    /// </summary>
    public static decimal CalcularCuota(decimal capital, decimal tasaAnual, int plazoMeses)
    {
        if (capital <= 0 || plazoMeses <= 0)
        {
            return 0m;
        }

        if (tasaAnual <= 0)
        {
            return Math.Round(capital / plazoMeses, 2, MidpointRounding.AwayFromZero);
        }

        var tasaMensual = (double)(tasaAnual / 100m / 12m);
        var factor = Math.Pow(1 + tasaMensual, plazoMeses);
        var cuota = (double)capital * tasaMensual * factor / (factor - 1);

        return Math.Round((decimal)cuota, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Construye la tabla de amortización mes a mes. El interés de cada período se calcula sobre el
    /// balance vivo y la última cuota absorbe el redondeo acumulado para que el balance cierre en cero.
    /// </summary>
    public static List<CuotaAmortizacion> GenerarTabla(Prestamo prestamo)
    {
        var tabla = new List<CuotaAmortizacion>();

        var cuota = CalcularCuota(prestamo.MontoSolicitado, prestamo.TasaAnual, prestamo.PlazoMeses);
        if (cuota <= 0)
        {
            return tabla;
        }

        var tasaMensual = prestamo.TasaAnual / 100m / 12m;
        var balance = prestamo.MontoSolicitado;

        for (var numero = 1; numero <= prestamo.PlazoMeses; numero++)
        {
            var interes = Math.Round(balance * tasaMensual, 2, MidpointRounding.AwayFromZero);
            var capital = cuota - interes;
            var esUltima = numero == prestamo.PlazoMeses;

            if (esUltima || capital > balance)
            {
                // La última cuota (o cualquiera que se pase) liquida exactamente lo que queda.
                capital = balance;
                cuota = capital + interes;
            }

            balance -= capital;

            tabla.Add(new CuotaAmortizacion
            {
                Numero = numero,
                Fecha = prestamo.FechaSolicitud.AddMonths(numero).Date,
                Cuota = Math.Round(cuota, 2, MidpointRounding.AwayFromZero),
                Capital = Math.Round(capital, 2, MidpointRounding.AwayFromZero),
                Interes = interes,
                Balance = Math.Round(Math.Max(balance, 0m), 2, MidpointRounding.AwayFromZero),
                Pagada = numero <= prestamo.CuotasPagadas,
                EsProximaCuota = numero == prestamo.CuotasPagadas + 1
            });
        }

        return tabla;
    }

    /// <summary>Devuelve la cuota que toca pagar, o null si el préstamo ya está saldado.</summary>
    public static CuotaAmortizacion? ObtenerProximaCuota(Prestamo prestamo) =>
        GenerarTabla(prestamo).FirstOrDefault(c => c.Numero == prestamo.CuotasPagadas + 1);
}
