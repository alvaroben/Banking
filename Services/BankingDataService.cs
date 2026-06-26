using System.Collections.ObjectModel;
using InternetBankingApp.Models;

namespace InternetBankingApp.Services;

public class BankingDataService
{
    public ObservableCollection<Cuenta> Cuentas { get; } = new();
    public ObservableCollection<Prestamo> Prestamos { get; } = new();
    public ObservableCollection<Beneficiario> Beneficiarios { get; } = new();
    public ObservableCollection<Transferencia> Transferencias { get; } = new();

    public void AgregarCuenta(Cuenta cuenta) => Cuentas.Add(cuenta);
    public void AgregarPrestamo(Prestamo prestamo) => Prestamos.Add(prestamo);
    public void AgregarBeneficiario(Beneficiario beneficiario) => Beneficiarios.Add(beneficiario);

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
