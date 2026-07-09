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
}
