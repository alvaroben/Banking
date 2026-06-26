using System.ComponentModel;

namespace InternetBankingApp.Models;

public enum TipoCuenta
{
    Ahorro,
    Corriente
}

public class Cuenta : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string NumeroCuenta { get; set; } = string.Empty;
    public TipoCuenta Tipo { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.Now;

    private decimal _saldo;
    public decimal Saldo
    {
        get => _saldo;
        set
        {
            if (_saldo == value)
            {
                return;
            }

            _saldo = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Saldo)));
        }
    }
}
