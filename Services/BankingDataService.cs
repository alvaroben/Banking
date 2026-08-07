using InternetBankingApp.Models;
using SQLite;

namespace InternetBankingApp.Services;

/// <summary>
/// Capa de acceso a datos de la aplicación. Toda la información vive en un archivo SQLite dentro
/// del almacenamiento privado de la app, así que sobrevive al cierre y a los reinicios.
/// La conexión se crea de forma perezosa (lazy): la primera pantalla que aparece dispara
/// <see cref="InicializarAsync"/> y a partir de ahí se reutiliza la misma conexión.
/// </summary>
public class BankingDataService
{
    public const string NombreArchivo = "internetbanking.db3";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private static readonly Random Random = new();

    private readonly SemaphoreSlim _puertaInicializacion = new(1, 1);
    private SQLiteAsyncConnection? _conexion;

    public string RutaBaseDatos { get; } = Path.Combine(FileSystem.AppDataDirectory, NombreArchivo);

    /// <summary>
    /// Abre la conexión y crea las tablas la primera vez que se llama. El semáforo evita que dos
    /// pantallas que aparecen a la vez inicialicen la base de datos por duplicado.
    /// </summary>
    public async Task<SQLiteAsyncConnection> ObtenerConexionAsync()
    {
        if (_conexion is not null)
        {
            return _conexion;
        }

        await _puertaInicializacion.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_conexion is not null)
            {
                return _conexion;
            }

            // storeDateTimeAsTicks: false guarda las fechas como texto ISO, de modo que el
            // dashboard puede agrupar por mes con strftime() directamente en SQL.
            var conexion = new SQLiteAsyncConnection(
                new SQLiteConnectionString(RutaBaseDatos, Flags, storeDateTimeAsTicks: false));

            await conexion.CreateTableAsync<Cuenta>().ConfigureAwait(false);
            await conexion.CreateTableAsync<Beneficiario>().ConfigureAwait(false);
            await conexion.CreateTableAsync<Prestamo>().ConfigureAwait(false);
            await conexion.CreateTableAsync<Transferencia>().ConfigureAwait(false);
            await conexion.CreateTableAsync<TransferenciaProgramada>().ConfigureAwait(false);
            await conexion.CreateTableAsync<PagoPrestamo>().ConfigureAwait(false);

            // Primera ejecución: deja la app con un escenario demostrable (cuentas, seis meses de
            // transferencias y préstamos a distinto nivel de avance). No hace nada si ya hay datos.
            await DatosEjemploService.SembrarSiVacioAsync(conexion).ConfigureAwait(false);

            _conexion = conexion;
            return conexion;
        }
        finally
        {
            _puertaInicializacion.Release();
        }
    }

    /// <summary>Inicialización lazy que invocan los OnAppearing de las listas.</summary>
    public Task InicializarAsync() => ObtenerConexionAsync();

    // ───────────────────────────── Cuentas ─────────────────────────────

    public async Task<List<Cuenta>> ObtenerCuentasAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Cuenta>().OrderBy(c => c.NumeroCuenta).ToListAsync();
    }

    public async Task<Cuenta?> ObtenerCuentaPorNumeroAsync(string numeroCuenta)
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Cuenta>().Where(c => c.NumeroCuenta == numeroCuenta).FirstOrDefaultAsync();
    }

    /// <summary>Inserta si el Id es 0, actualiza en caso contrario.</summary>
    public async Task GuardarCuentaAsync(Cuenta cuenta)
    {
        var conexion = await ObtenerConexionAsync();

        if (cuenta.Id == 0)
        {
            await conexion.InsertAsync(cuenta);
        }
        else
        {
            await conexion.UpdateAsync(cuenta);
        }
    }

    public async Task EliminarCuentaAsync(Cuenta cuenta)
    {
        var conexion = await ObtenerConexionAsync();
        await conexion.DeleteAsync(cuenta);
    }

    /// <summary>Genera un número de cuenta de 10 dígitos que no exista todavía en la base de datos.</summary>
    public async Task<string> GenerarNumeroCuentaAsync()
    {
        var conexion = await ObtenerConexionAsync();

        string numero;
        do
        {
            numero = $"10{Random.Next(0, 100_000_000):D8}";
        }
        while (await conexion.Table<Cuenta>().Where(c => c.NumeroCuenta == numero).CountAsync() > 0);

        return numero;
    }

    // ────────────────────────── Beneficiarios ──────────────────────────

    public async Task<List<Beneficiario>> ObtenerBeneficiariosAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Beneficiario>().OrderBy(b => b.Nombre).ToListAsync();
    }

    public async Task GuardarBeneficiarioAsync(Beneficiario beneficiario)
    {
        var conexion = await ObtenerConexionAsync();

        if (beneficiario.Id == 0)
        {
            await conexion.InsertAsync(beneficiario);
        }
        else
        {
            await conexion.UpdateAsync(beneficiario);
        }
    }

    public async Task EliminarBeneficiarioAsync(Beneficiario beneficiario)
    {
        var conexion = await ObtenerConexionAsync();
        await conexion.DeleteAsync(beneficiario);
    }

    // ───────────────────────────── Préstamos ────────────────────────────

    public async Task<List<Prestamo>> ObtenerPrestamosAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Prestamo>().OrderByDescending(p => p.FechaSolicitud).ToListAsync();
    }

    public async Task<Prestamo?> ObtenerPrestamoAsync(int id)
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Prestamo>().Where(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task GuardarPrestamoAsync(Prestamo prestamo)
    {
        var conexion = await ObtenerConexionAsync();

        if (prestamo.Id == 0)
        {
            await conexion.InsertAsync(prestamo);
        }
        else
        {
            await conexion.UpdateAsync(prestamo);
        }
    }

    /// <summary>Borra el préstamo junto con su historial de pagos, en una sola transacción.</summary>
    public async Task EliminarPrestamoAsync(Prestamo prestamo)
    {
        var conexion = await ObtenerConexionAsync();

        await conexion.RunInTransactionAsync(transaccion =>
        {
            transaccion.Execute("DELETE FROM PagosPrestamo WHERE PrestamoId = ?", prestamo.Id);
            transaccion.Delete(prestamo);
        });
    }

    public async Task<List<PagoPrestamo>> ObtenerPagosPrestamoAsync(int prestamoId)
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<PagoPrestamo>()
            .Where(p => p.PrestamoId == prestamoId)
            .OrderByDescending(p => p.NumeroCuota)
            .ToListAsync();
    }

    /// <summary>
    /// Paga una cuota del préstamo debitándola de una cuenta. Todo ocurre dentro de una
    /// transacción: o se debita el saldo, se registra el pago y avanza el contador de cuotas, o no
    /// pasa nada. Devuelve false si la cuenta no tiene fondos.
    /// </summary>
    public async Task<bool> PagarCuotaPrestamoAsync(Prestamo prestamo, Cuenta cuenta, CuotaAmortizacion cuota)
    {
        var conexion = await ObtenerConexionAsync();
        var exitoso = false;

        await conexion.RunInTransactionAsync(transaccion =>
        {
            var cuentaActual = transaccion.Find<Cuenta>(cuenta.Id);
            var prestamoActual = transaccion.Find<Prestamo>(prestamo.Id);

            if (cuentaActual is null || prestamoActual is null || cuentaActual.Saldo < cuota.Cuota)
            {
                return;
            }

            cuentaActual.Saldo -= cuota.Cuota;
            prestamoActual.CuotasPagadas = cuota.Numero;

            transaccion.Update(cuentaActual);
            transaccion.Update(prestamoActual);
            transaccion.Insert(new PagoPrestamo
            {
                PrestamoId = prestamoActual.Id,
                NumeroCuota = cuota.Numero,
                Fecha = DateTime.Now,
                Monto = cuota.Cuota,
                CapitalPagado = cuota.Capital,
                InteresPagado = cuota.Interes,
                CuentaOrigen = cuentaActual.NumeroCuenta
            });

            exitoso = true;
        });

        if (exitoso)
        {
            // Refleja en los objetos que ya están enlazados a la UI lo que quedó escrito en la base.
            cuenta.Saldo -= cuota.Cuota;
            prestamo.CuotasPagadas = cuota.Numero;
        }

        return exitoso;
    }

    // ─────────────────────────── Transferencias ─────────────────────────

    public async Task<List<Transferencia>> ObtenerTransferenciasAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Transferencia>().OrderByDescending(t => t.Fecha).ToListAsync();
    }

    /// <summary>
    /// Debita la cuenta origen y registra la transferencia en una sola transacción. El saldo se
    /// vuelve a leer dentro de la transacción para no confiar en la copia que tiene la UI.
    /// Devuelve false si los fondos son insuficientes.
    /// </summary>
    public async Task<bool> RegistrarTransferenciaAsync(
        Cuenta cuentaOrigen,
        Beneficiario beneficiarioDestino,
        string concepto,
        decimal monto,
        OrigenTransferencia origen = OrigenTransferencia.Manual,
        DateTime? fecha = null)
    {
        if (monto <= 0)
        {
            return false;
        }

        var conexion = await ObtenerConexionAsync();
        var exitoso = false;

        await conexion.RunInTransactionAsync(transaccion =>
        {
            var cuentaActual = transaccion.Find<Cuenta>(cuentaOrigen.Id);
            if (cuentaActual is null || cuentaActual.Saldo < monto)
            {
                return;
            }

            cuentaActual.Saldo -= monto;
            transaccion.Update(cuentaActual);
            transaccion.Insert(new Transferencia
            {
                CuentaOrigen = cuentaActual.NumeroCuenta,
                BeneficiarioDestino = beneficiarioDestino.Nombre,
                Concepto = concepto,
                Monto = monto,
                // Una orden programada atrasada se registra con la fecha que le tocaba, no con la
                // de hoy, para que el historial y el gráfico por mes reflejen cuándo correspondía.
                Fecha = fecha ?? DateTime.Now,
                Origen = origen
            });

            exitoso = true;
        });

        if (exitoso)
        {
            cuentaOrigen.Saldo -= monto;
        }

        return exitoso;
    }

    /// <summary>Revierte el monto a la cuenta origen (si aún existe) y elimina la transferencia.</summary>
    public async Task EliminarTransferenciaAsync(Transferencia transferencia)
    {
        var conexion = await ObtenerConexionAsync();

        await conexion.RunInTransactionAsync(transaccion =>
        {
            var cuentaOrigen = transaccion.Table<Cuenta>()
                .FirstOrDefault(c => c.NumeroCuenta == transferencia.CuentaOrigen);

            if (cuentaOrigen is not null)
            {
                cuentaOrigen.Saldo += transferencia.Monto;
                transaccion.Update(cuentaOrigen);
            }

            transaccion.Delete(transferencia);
        });
    }

    /// <summary>
    /// Devuelve el monto anterior a su cuenta y aplica los valores nuevos. Si los fondos no
    /// alcanzan, se lanza una excepción dentro de la transacción para que sqlite-net haga rollback
    /// de todo y el método devuelva false sin dejar rastros a medias.
    /// </summary>
    public async Task<bool> ActualizarTransferenciaAsync(
        Transferencia transferencia,
        Cuenta nuevaCuentaOrigen,
        Beneficiario nuevoBeneficiarioDestino,
        string concepto,
        decimal nuevoMonto)
    {
        if (nuevoMonto <= 0)
        {
            return false;
        }

        var conexion = await ObtenerConexionAsync();
        var cuentaOrigenAnterior = transferencia.CuentaOrigen;
        var montoAnterior = transferencia.Monto;

        try
        {
            await conexion.RunInTransactionAsync(transaccion =>
            {
                var cuentaAnterior = transaccion.Table<Cuenta>()
                    .FirstOrDefault(c => c.NumeroCuenta == cuentaOrigenAnterior);

                if (cuentaAnterior is not null)
                {
                    cuentaAnterior.Saldo += montoAnterior;
                    transaccion.Update(cuentaAnterior);
                }

                // Si la cuenta que ahora paga es la misma que acabamos de acreditar, hay que
                // trabajar sobre ese mismo objeto para partir del saldo ya revertido.
                var cuentaNueva = cuentaAnterior is not null && cuentaAnterior.Id == nuevaCuentaOrigen.Id
                    ? cuentaAnterior
                    : transaccion.Find<Cuenta>(nuevaCuentaOrigen.Id);

                if (cuentaNueva is null || cuentaNueva.Saldo < nuevoMonto)
                {
                    throw new SaldoInsuficienteException();
                }

                cuentaNueva.Saldo -= nuevoMonto;
                transaccion.Update(cuentaNueva);

                transferencia.CuentaOrigen = cuentaNueva.NumeroCuenta;
                transferencia.BeneficiarioDestino = nuevoBeneficiarioDestino.Nombre;
                transferencia.Concepto = concepto;
                transferencia.Monto = nuevoMonto;
                transaccion.Update(transferencia);
            });
        }
        catch (SaldoInsuficienteException)
        {
            // La base quedó intacta; deshacemos también los cambios del objeto en memoria.
            transferencia.CuentaOrigen = cuentaOrigenAnterior;
            transferencia.Monto = montoAnterior;
            return false;
        }

        return true;
    }

    // ──────────────────── Transferencias programadas ────────────────────

    public async Task<List<TransferenciaProgramada>> ObtenerProgramacionesAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<TransferenciaProgramada>()
            .OrderByDescending(p => p.Activa)
            .ThenBy(p => p.ProximaEjecucion)
            .ToListAsync();
    }

    public async Task GuardarProgramacionAsync(TransferenciaProgramada programacion)
    {
        var conexion = await ObtenerConexionAsync();

        if (programacion.Id == 0)
        {
            await conexion.InsertAsync(programacion);
        }
        else
        {
            await conexion.UpdateAsync(programacion);
        }
    }

    public async Task EliminarProgramacionAsync(TransferenciaProgramada programacion)
    {
        var conexion = await ObtenerConexionAsync();
        await conexion.DeleteAsync(programacion);
    }

    // ────────────────── Consultas agregadas del dashboard ───────────────

    /// <summary>Suma de los saldos de todas las cuentas, calculada por SQLite y no en memoria.</summary>
    public async Task<decimal> ObtenerSaldoTotalAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.ExecuteScalarAsync<decimal>("SELECT IFNULL(SUM(Saldo), 0) FROM Cuentas");
    }

    public async Task<decimal> ObtenerTotalTransferidoAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.ExecuteScalarAsync<decimal>("SELECT IFNULL(SUM(Monto), 0) FROM Transferencias");
    }

    public async Task<int> ContarCuentasAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Cuenta>().CountAsync();
    }

    public async Task<int> ContarBeneficiariosAsync()
    {
        var conexion = await ObtenerConexionAsync();
        return await conexion.Table<Beneficiario>().CountAsync();
    }

    /// <summary>
    /// Transferencias agrupadas por mes. Como las fechas se guardan en texto ISO, strftime() puede
    /// hacer el GROUP BY dentro de SQLite en vez de traer todas las filas a memoria.
    /// </summary>
    public async Task<List<TotalPorMes>> ObtenerTotalesPorMesAsync(int meses = 6)
    {
        var conexion = await ObtenerConexionAsync();

        var filas = await conexion.QueryAsync<TotalPorMes>(
            """
            SELECT strftime('%Y-%m', Fecha) AS Mes,
                   SUM(Monto)               AS Total,
                   COUNT(*)                 AS Cantidad
            FROM Transferencias
            GROUP BY Mes
            ORDER BY Mes DESC
            LIMIT ?
            """,
            meses);

        // La consulta trae los meses del más reciente al más antiguo; el gráfico los quiere al revés.
        filas.Reverse();
        return filas;
    }

    /// <summary>Beneficiarios que más dinero han recibido, de mayor a menor.</summary>
    public async Task<List<TotalPorBeneficiario>> ObtenerTopBeneficiariosAsync(int top = 5)
    {
        var conexion = await ObtenerConexionAsync();

        return await conexion.QueryAsync<TotalPorBeneficiario>(
            """
            SELECT BeneficiarioDestino AS Nombre,
                   SUM(Monto)          AS Total,
                   COUNT(*)            AS Cantidad
            FROM Transferencias
            GROUP BY BeneficiarioDestino
            ORDER BY Total DESC
            LIMIT ?
            """,
            top);
    }
}

/// <summary>
/// Señal interna para abortar una transacción de sqlite-net: al propagarse fuera del callback de
/// RunInTransactionAsync, la librería revierte todo lo escrito.
/// </summary>
public class SaldoInsuficienteException : Exception
{
    public SaldoInsuficienteException() : base("Fondos insuficientes en la cuenta seleccionada.")
    {
    }
}
