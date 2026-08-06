using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace InternetBankingApp.Models;

public enum TipoCuenta
{
    Ahorro,
    Corriente
}

[Table("Cuentas")]
public partial class Cuenta : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Número de cuenta de 10 dígitos. Es único a nivel de base de datos: si dos escrituras
    /// generaran el mismo número, SQLite lanza SQLiteException con Result.Constraint.
    /// </summary>
    [ObservableProperty]
    [property: Unique]
    [property: MaxLength(10)]
    private string numeroCuenta = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumen))]
    private TipoCuenta tipo;

    [ObservableProperty]
    private DateTime fechaApertura = DateTime.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumen))]
    private decimal saldo;

    /// <summary>Texto que se muestra en los Picker de cuenta origen. No se persiste.</summary>
    [Ignore]
    public string Resumen => $"{NumeroCuenta} · {Tipo} · {Saldo:C2}";
}
