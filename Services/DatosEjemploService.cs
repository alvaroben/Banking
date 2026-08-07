using InternetBankingApp.Models;
using SQLite;

namespace InternetBankingApp.Services;

/// <summary>
/// Siembra la base de datos con un escenario financiero realista la primera vez que se abre la app.
/// Existe para que el dashboard, el simulador de préstamos y el plan de pagos tengan algo que mostrar
/// desde el arranque, en vez de exigirle al usuario capturar decenas de registros a mano para poder
/// ver el gráfico por mes o una tabla de amortización a medio pagar.
///
/// Los registros se insertan como HISTORIA YA OCURRIDA: las transferencias y los pagos de cuotas no
/// pasan por RegistrarTransferenciaAsync ni PagarCuotaPrestamoAsync (que debitarían saldo), sino que
/// se escriben directo con los saldos finales ya calculados. Así el seed es idempotente y no depende
/// del orden de inserción.
/// </summary>
public static class DatosEjemploService
{
    private const string CuentaAhorro = "1002345678";
    private const string CuentaCorriente = "1009876543";

    /// <summary>
    /// Inserta el escenario de ejemplo solo si la base está vacía. Si el usuario ya creó datos
    /// propios (o si el seed corrió antes), no toca nada.
    /// </summary>
    public static async Task SembrarSiVacioAsync(SQLiteAsyncConnection conexion)
    {
        var yaHayDatos = await conexion.Table<Cuenta>().CountAsync() > 0
            || await conexion.Table<Prestamo>().CountAsync() > 0
            || await conexion.Table<Beneficiario>().CountAsync() > 0;

        if (yaHayDatos)
        {
            return;
        }

        var hoy = DateTime.Today;

        await SembrarCuentasAsync(conexion, hoy);
        var beneficiarios = await SembrarBeneficiariosAsync(conexion);
        await SembrarTransferenciasAsync(conexion, hoy);
        await SembrarPrestamosAsync(conexion, hoy);
        await SembrarProgramadasAsync(conexion, hoy, beneficiarios);
    }

    // ───────────────────────────── Cuentas ─────────────────────────────

    private static async Task SembrarCuentasAsync(SQLiteAsyncConnection conexion, DateTime hoy)
    {
        // Los saldos ya reflejan todo el movimiento histórico que se siembra más abajo.
        await conexion.InsertAllAsync(new[]
        {
            new Cuenta
            {
                NumeroCuenta = CuentaAhorro,
                Tipo = TipoCuenta.Ahorro,
                FechaApertura = hoy.AddYears(-2).AddMonths(-3),
                Saldo = 485_000.00m
            },
            new Cuenta
            {
                NumeroCuenta = CuentaCorriente,
                Tipo = TipoCuenta.Corriente,
                FechaApertura = hoy.AddMonths(-14),
                Saldo = 128_600.75m
            }
        });
    }

    // ────────────────────────── Beneficiarios ──────────────────────────

    private static async Task<List<Beneficiario>> SembrarBeneficiariosAsync(SQLiteAsyncConnection conexion)
    {
        var beneficiarios = new List<Beneficiario>
        {
            new() { Nombre = "María Fernández", NumeroCuenta = "2001234567", Banco = "Banco Popular" },
            new() { Nombre = "Carlos Ramírez", NumeroCuenta = "2007654321", Banco = "Banco BHD" },
            new() { Nombre = "Ana Gutiérrez", NumeroCuenta = "2003456789", Banco = "Banreservas" },
            new() { Nombre = "Luis Martínez", NumeroCuenta = "2009871234", Banco = "Scotiabank" },
            new() { Nombre = "Sofía Peña", NumeroCuenta = "2005647382", Banco = "Banco Santa Cruz" }
        };

        await conexion.InsertAllAsync(beneficiarios);
        return beneficiarios;
    }

    // ─────────────────────────── Transferencias ────────────────────────

    /// <summary>
    /// Seis meses de movimiento para que el gráfico del dashboard (GROUP BY mes) tenga barras
    /// comparables y el ranking de beneficiarios tenga un ganador claro.
    /// </summary>
    private static async Task SembrarTransferenciasAsync(SQLiteAsyncConnection conexion, DateTime hoy)
    {
        var transferencias = new List<Transferencia>();
        var primerDiaMesActual = new DateTime(hoy.Year, hoy.Month, 1);

        // Gastos recurrentes: se repiten los 6 meses, así el alquiler domina el ranking.
        var recurrentes = new (string Destino, string Concepto, decimal Monto, int Dia, OrigenTransferencia Origen)[]
        {
            ("María Fernández", "Alquiler mensual", 28_000m, 1, OrigenTransferencia.Programada),
            ("Ana Gutiérrez", "Mensualidad colegio", 15_500m, 5, OrigenTransferencia.Programada)
        };

        // Gastos variables: rotan por mes para que las barras no queden todas iguales.
        var variables = new (string Destino, string Concepto, decimal Monto, int Dia)[]
        {
            ("Carlos Ramírez", "Préstamo entre amigos", 12_400m, 9),
            ("Luis Martínez", "Servicios profesionales", 7_800m, 15),
            ("Sofía Peña", "Compra de equipos", 5_250m, 22),
            ("Carlos Ramírez", "Reparación del vehículo", 9_600m, 18),
            ("Luis Martínez", "Diseño de la tienda", 11_200m, 12),
            ("Sofía Peña", "Mobiliario de oficina", 6_400m, 26)
        };

        for (var offset = 5; offset >= 0; offset--)
        {
            var mes = primerDiaMesActual.AddMonths(-offset);

            foreach (var fijo in recurrentes)
            {
                Agregar(transferencias, hoy, mes, fijo.Dia, CuentaAhorro, fijo.Destino, fijo.Concepto, fijo.Monto, fijo.Origen);
            }

            // Dos gastos variables por mes, tomados de la lista de forma rotativa.
            var indice = (5 - offset) * 2;
            for (var i = 0; i < 2; i++)
            {
                var variable = variables[(indice + i) % variables.Length];
                Agregar(transferencias, hoy, mes, variable.Dia, CuentaCorriente, variable.Destino, variable.Concepto, variable.Monto, OrigenTransferencia.Manual);
            }
        }

        await conexion.InsertAllAsync(transferencias);
    }

    /// <summary>Agrega la transferencia salvo que la fecha caiga en el futuro (mes en curso).</summary>
    private static void Agregar(
        List<Transferencia> destino,
        DateTime hoy,
        DateTime mes,
        int dia,
        string cuentaOrigen,
        string beneficiario,
        string concepto,
        decimal monto,
        OrigenTransferencia origen)
    {
        var fecha = mes.AddDays(Math.Min(dia, DateTime.DaysInMonth(mes.Year, mes.Month)) - 1);

        if (fecha > hoy)
        {
            return;
        }

        destino.Add(new Transferencia
        {
            CuentaOrigen = cuentaOrigen,
            BeneficiarioDestino = beneficiario,
            Concepto = concepto,
            Monto = monto,
            Fecha = fecha,
            Origen = origen
        });
    }

    // ───────────────────────────── Préstamos ───────────────────────────

    /// <summary>
    /// Cuatro préstamos en etapas distintas del ciclo de vida, para que el plan de pagos se pueda
    /// demostrar completo: uno recién desembolsado, dos a media vida y uno a punto de saldarse.
    /// Las tasas y nombres salen del catálogo de <see cref="AmortizacionService"/>, no inventados,
    /// para que al abrir el préstamo en modo edición el Picker de producto lo reconozca.
    ///
    /// Los montos y plazos se mantienen moderados a propósito. El dashboard calcula la deuda con
    /// <c>SaldoPendiente</c> = cuota × cuotas restantes, es decir, todo lo que falta por desembolsar
    /// (capital + intereses futuros). Con un hipotecario a 20 años eso da millones y hunde el
    /// patrimonio neto hasta parecer un error de datos; con esta cartera queda en un rango creíble.
    /// El hipotecario sigue disponible en el catálogo para probar el simulador a mano.
    /// </summary>
    private static async Task SembrarPrestamosAsync(SQLiteAsyncConnection conexion, DateTime hoy)
    {
        var prestamos = new[]
        {
            // A media vida: la tabla mezcla cuotas pagadas y pendientes.
            Construir("Préstamo de vehículo", 620_000m, 48, hoy.AddMonths(-14), cuotasPagadas: 14),

            // El más largo de la cartera: al principio el interés pesa mucho más que el capital.
            Construir("Préstamo comercial", 450_000m, 60, hoy.AddMonths(-20), cuotasPagadas: 20),

            // Casi saldado: quedan 2 cuotas, ideal para demostrar el pago que cierra el préstamo.
            Construir("Préstamo personal", 180_000m, 24, hoy.AddMonths(-22), cuotasPagadas: 22),

            // Recién desembolsado.
            Construir("Préstamo educativo", 320_000m, 36, hoy.AddMonths(-3), cuotasPagadas: 3)
        };

        foreach (var prestamo in prestamos)
        {
            // Se inserta primero para que SQLite asigne el Id que necesitan los pagos.
            await conexion.InsertAsync(prestamo);

            if (prestamo.CuotasPagadas == 0)
            {
                continue;
            }

            // El historial se arma desde la misma tabla de amortización que muestra la app, así el
            // desglose capital/interés de cada pago coincide exactamente con el plan.
            var pagos = AmortizacionService.GenerarTabla(prestamo)
                .Where(cuota => cuota.Numero <= prestamo.CuotasPagadas)
                .Select(cuota => new PagoPrestamo
                {
                    PrestamoId = prestamo.Id,
                    NumeroCuota = cuota.Numero,
                    Fecha = cuota.Fecha,
                    Monto = cuota.Cuota,
                    CapitalPagado = cuota.Capital,
                    InteresPagado = cuota.Interes,
                    CuentaOrigen = CuentaAhorro
                })
                .ToList();

            await conexion.InsertAllAsync(pagos);
        }
    }

    private static Prestamo Construir(string producto, decimal monto, int plazoMeses, DateTime fechaSolicitud, int cuotasPagadas)
    {
        var delCatalogo = AmortizacionService.BuscarProducto(producto);

        return new Prestamo
        {
            Producto = producto,
            MontoSolicitado = monto,
            PlazoMeses = plazoMeses,
            TasaAnual = delCatalogo?.TasaAnual ?? 0m,
            FechaSolicitud = fechaSolicitud,
            CuotasPagadas = cuotasPagadas
        };
    }

    // ──────────────────── Transferencias programadas ───────────────────

    /// <summary>
    /// Órdenes permanentes con la próxima ejecución en el futuro cercano: aparecen activas en la
    /// pantalla de Programadas sin que el motor las cobre de golpe al abrir el dashboard.
    /// </summary>
    private static async Task SembrarProgramadasAsync(SQLiteAsyncConnection conexion, DateTime hoy, List<Beneficiario> beneficiarios)
    {
        await conexion.InsertAllAsync(new[]
        {
            new TransferenciaProgramada
            {
                CuentaOrigen = CuentaAhorro,
                BeneficiarioDestino = beneficiarios[0].Nombre,
                Concepto = "Alquiler mensual",
                Monto = 28_000m,
                Frecuencia = FrecuenciaProgramacion.Mensual,
                ProximaEjecucion = hoy.AddDays(6),
                Activa = true,
                EjecucionesRealizadas = 6
            },
            new TransferenciaProgramada
            {
                CuentaOrigen = CuentaAhorro,
                BeneficiarioDestino = beneficiarios[2].Nombre,
                Concepto = "Mensualidad colegio",
                Monto = 15_500m,
                Frecuencia = FrecuenciaProgramacion.Mensual,
                ProximaEjecucion = hoy.AddDays(12),
                Activa = true,
                EjecucionesRealizadas = 6
            },
            new TransferenciaProgramada
            {
                CuentaOrigen = CuentaCorriente,
                BeneficiarioDestino = beneficiarios[3].Nombre,
                Concepto = "Iguala de mantenimiento",
                Monto = 4_500m,
                Frecuencia = FrecuenciaProgramacion.Quincenal,
                ProximaEjecucion = hoy.AddDays(3),
                Activa = true,
                EjecucionesRealizadas = 11
            }
        });
    }
}
