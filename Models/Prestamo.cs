using CommunityToolkit.Mvvm.ComponentModel;

namespace InternetBankingApp.Models;

public partial class Prestamo : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string producto = string.Empty;

    [ObservableProperty]
    private decimal montoSolicitado;

    [ObservableProperty]
    private int plazoMeses;

    [ObservableProperty]
    private DateTime fechaSolicitud = DateTime.Now;
}
