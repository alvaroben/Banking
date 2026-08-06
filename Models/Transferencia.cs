using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace InternetBankingApp.Models;

/// <summary>Distingue lo que el usuario hizo a mano de lo que ejecutó el motor de programaciones.</summary>
public enum OrigenTransferencia
{
    Manual,
    Programada
}

[Table("Transferencias")]
public partial class Transferencia : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ObservableProperty]
    [property: Indexed]
    private string cuentaOrigen = string.Empty;

    [ObservableProperty]
    private string beneficiarioDestino = string.Empty;

    [ObservableProperty]
    private string concepto = string.Empty;

    [ObservableProperty]
    private decimal monto;

    [ObservableProperty]
    private DateTime fecha = DateTime.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsProgramada))]
    private OrigenTransferencia origen;

    [Ignore]
    public bool EsProgramada => Origen == OrigenTransferencia.Programada;
}
