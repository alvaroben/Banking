using CommunityToolkit.Mvvm.ComponentModel;

namespace InternetBankingApp.Models;

public partial class Transferencia : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string cuentaOrigen = string.Empty;

    [ObservableProperty]
    private string beneficiarioDestino = string.Empty;

    [ObservableProperty]
    private string concepto = string.Empty;

    [ObservableProperty]
    private decimal monto;

    [ObservableProperty]
    private DateTime fecha = DateTime.Now;
}
