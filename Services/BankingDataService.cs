using System.Collections.ObjectModel;
using System.Linq;
using InternetBankingApp.Models;

namespace InternetBankingApp.Services;

public class BankingDataService
{
    private static readonly Random Random = new();

    public ObservableCollection<Cuenta> Cuentas { get; } = new();
    public ObservableCollection<Prestamo> Prestamos { get; } = new();
    public ObservableCollection<Beneficiario> Beneficiarios { get; } = new();
    public ObservableCollection<Transferencia> Transferencias { get; } = new();

    public void AgregarCuenta(Cuenta cuenta) => Cuentas.Add(cuenta);
    public void AgregarPrestamo(Prestamo prestamo) => Prestamos.Add(prestamo);
    public void AgregarBeneficiario(Beneficiario beneficiario) => Beneficiarios.Add(beneficiario);

    public void EliminarCuenta(Cuenta cuenta) => Cuentas.Remove(cuenta);
    public void EliminarPrestamo(Prestamo prestamo) => Prestamos.Remove(prestamo);
    public void EliminarBeneficiario(Beneficiario beneficiario) => Beneficiarios.Remove(beneficiario);

    /// <summary>Genera un número de cuenta de 10 dígitos que no esté en uso.</summary>
    public string GenerarNumeroCuenta()
    {
        string numero;
        do
        {
            numero = $"10{Random.Next(0, 100_000_000):D8}";
        } while (Cuentas.Any(c => c.NumeroCuenta == numero));

        return numero;
    }

    /// <summary>Descuenta el monto de la cuenta origen y registra la transferencia. Devuelve false si los fondos son insuficientes.</summary>
    public bool RegistrarTransferencia(Cuenta cuentaOrigen, Beneficiario beneficiarioDestino, string concepto, decimal monto)
    {
        if (monto <= 0 || monto > cuentaOrigen.Saldo)
        {
            return false;
        }

        cuentaOrigen.Saldo -= monto;

        Transferencias.Add(new Transferencia
        {
            CuentaOrigen = cuentaOrigen.NumeroCuenta,
            BeneficiarioDestino = beneficiarioDestino.Nombre,
            Concepto = concepto,
            Monto = monto
        });

        return true;
    }

    /// <summary>Revierte el monto a la cuenta origen (si aún existe) y elimina la transferencia.</summary>
    public void EliminarTransferencia(Transferencia transferencia)
    {
        var cuentaOrigen = Cuentas.FirstOrDefault(c => c.NumeroCuenta == transferencia.CuentaOrigen);
        if (cuentaOrigen is not null)
        {
            cuentaOrigen.Saldo += transferencia.Monto;
        }

        Transferencias.Remove(transferencia);
    }

    /// <summary>
    /// Revierte el efecto de la transferencia original sobre su cuenta origen y aplica los nuevos
    /// valores. Devuelve false (sin dejar cambios) si los fondos resultantes son insuficientes.
    /// </summary>
    public bool ActualizarTransferencia(Transferencia transferencia, Cuenta nuevaCuentaOrigen, Beneficiario nuevoBeneficiarioDestino, string concepto, decimal nuevoMonto)
    {
        var cuentaOrigenAnterior = Cuentas.FirstOrDefault(c => c.NumeroCuenta == transferencia.CuentaOrigen);
        if (cuentaOrigenAnterior is not null)
        {
            cuentaOrigenAnterior.Saldo += transferencia.Monto;
        }

        if (nuevoMonto <= 0 || nuevoMonto > nuevaCuentaOrigen.Saldo)
        {
            if (cuentaOrigenAnterior is not null)
            {
                cuentaOrigenAnterior.Saldo -= transferencia.Monto;
            }

            return false;
        }

        nuevaCuentaOrigen.Saldo -= nuevoMonto;

        transferencia.CuentaOrigen = nuevaCuentaOrigen.NumeroCuenta;
        transferencia.BeneficiarioDestino = nuevoBeneficiarioDestino.Nombre;
        transferencia.Concepto = concepto;
        transferencia.Monto = nuevoMonto;

        return true;
    }
}
