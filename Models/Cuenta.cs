using CommunityToolkit.Mvvm.ComponentModel;

namespace InternetBankingApp.Models;

public enum TipoCuenta
{
    Ahorro,
    Corriente
}

public partial class Cuenta : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string numeroCuenta = string.Empty;

    [ObservableProperty]
    private TipoCuenta tipo;

    [ObservableProperty]
    private DateTime fechaApertura = DateTime.Now;

    [ObservableProperty]
    private decimal saldo;
}
