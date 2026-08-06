using InternetBankingApp.Models;

namespace InternetBankingApp.Services;

/// <summary>Resumen de lo que hizo el motor en una corrida, para avisarle al usuario.</summary>
public class ResultadoProgramaciones
{
    public int Ejecutadas { get; set; }

    public decimal MontoTotal { get; set; }

    /// <summary>Descripción de las órdenes que el motor tuvo que pausar y por qué.</summary>
    public List<string> Pausadas { get; } = [];

    public bool HuboMovimiento => Ejecutadas > 0 || Pausadas.Count > 0;

    public string Mensaje
    {
        get
        {
            var partes = new List<string>();

            if (Ejecutadas > 0)
            {
                partes.Add(Ejecutadas == 1
                    ? $"Se ejecutó 1 transferencia programada por {MontoTotal:C2}."
                    : $"Se ejecutaron {Ejecutadas} transferencias programadas por un total de {MontoTotal:C2}.");
            }

            if (Pausadas.Count > 0)
            {
                partes.Add("Órdenes pausadas:\n• " + string.Join("\n• ", Pausadas));
            }

            return string.Join("\n\n", partes);
        }
    }
}

/// <summary>
/// Motor de transferencias programadas. Cada vez que la app arranca (o el usuario entra al
/// dashboard) revisa qué órdenes vencieron y las ejecuta, incluidas las ocurrencias atrasadas de
/// los días en que la app estuvo cerrada. Si una orden no puede cobrarse, se pausa con su motivo
/// en lugar de fallar en silencio.
/// </summary>
public class ProgramacionesService
{
    /// <summary>
    /// Tope de seguridad por orden y por corrida: si la app estuvo meses sin abrirse, no queremos
    /// vaciar una cuenta de golpe con decenas de cargos retroactivos.
    /// </summary>
    private const int MaxOcurrenciasPorCorrida = 12;

    private readonly BankingDataService _dataService;

    public ProgramacionesService(BankingDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<ResultadoProgramaciones> EjecutarPendientesAsync()
    {
        var resultado = new ResultadoProgramaciones();

        await _dataService.InicializarAsync();

        var programaciones = await _dataService.ObtenerProgramacionesAsync();
        if (programaciones.Count == 0)
        {
            return resultado;
        }

        var cuentas = await _dataService.ObtenerCuentasAsync();
        var beneficiarios = await _dataService.ObtenerBeneficiariosAsync();
        var hoy = DateTime.Today;

        foreach (var programacion in programaciones.Where(p => p.Activa && p.ProximaEjecucion.Date <= hoy))
        {
            var cuenta = cuentas.FirstOrDefault(c => c.NumeroCuenta == programacion.CuentaOrigen);
            var beneficiario = beneficiarios.FirstOrDefault(b => b.Nombre == programacion.BeneficiarioDestino);

            if (cuenta is null || beneficiario is null)
            {
                Pausar(programacion, resultado, "la cuenta origen o el beneficiario ya no existen");
                await _dataService.GuardarProgramacionAsync(programacion);
                continue;
            }

            var ocurrencias = 0;
            var seModifico = false;

            while (programacion.Activa
                   && programacion.ProximaEjecucion.Date <= hoy
                   && ocurrencias < MaxOcurrenciasPorCorrida)
            {
                // La ocurrencia se registra con la fecha en que vencía, aunque el motor la ejecute
                // hoy porque la app estuvo cerrada.
                var fechaOcurrencia = programacion.ProximaEjecucion;

                var cobrada = await _dataService.RegistrarTransferenciaAsync(
                    cuenta,
                    beneficiario,
                    programacion.Concepto,
                    programacion.Monto,
                    OrigenTransferencia.Programada,
                    fechaOcurrencia);

                if (!cobrada)
                {
                    Pausar(programacion, resultado,
                        $"fondos insuficientes en la cuenta {cuenta.NumeroCuenta} para cobrar {programacion.Monto:C2}");
                    seModifico = true;
                    break;
                }

                programacion.EjecucionesRealizadas++;
                programacion.ProximaEjecucion = programacion.CalcularSiguienteFecha(programacion.ProximaEjecucion);
                programacion.MotivoPausa = null;

                resultado.Ejecutadas++;
                resultado.MontoTotal += programacion.Monto;
                ocurrencias++;
                seModifico = true;
            }

            if (seModifico)
            {
                await _dataService.GuardarProgramacionAsync(programacion);
            }
        }

        return resultado;
    }

    private static void Pausar(TransferenciaProgramada programacion, ResultadoProgramaciones resultado, string motivo)
    {
        programacion.Activa = false;
        programacion.MotivoPausa = motivo;
        resultado.Pausadas.Add($"{programacion.Concepto} → {programacion.BeneficiarioDestino}: {motivo}");
    }
}
