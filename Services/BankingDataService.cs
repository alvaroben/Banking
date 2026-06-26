using System.Collections.ObjectModel;
using InternetBankingApp.Models;

namespace InternetBankingApp.Services;

public class BankingDataService
{
    public ObservableCollection<Cuenta> Cuentas { get; } = new();
    public ObservableCollection<Prestamo> Prestamos { get; } = new();
    public ObservableCollection<Beneficiario> Beneficiarios { get; } = new();
    public ObservableCollection<Movimiento> Movimientos { get; } = new();

    public void AgregarCuenta(Cuenta cuenta) => Cuentas.Add(cuenta);
    public void AgregarPrestamo(Prestamo prestamo) => Prestamos.Add(prestamo);
    public void AgregarBeneficiario(Beneficiario beneficiario) => Beneficiarios.Add(beneficiario);
    public void AgregarMovimiento(Movimiento movimiento) => Movimientos.Add(movimiento);
}
