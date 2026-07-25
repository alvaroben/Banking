using CommunityToolkit.Mvvm.ComponentModel;

namespace InternetBankingApp.Models;

public partial class Beneficiario : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string nombre = string.Empty;

    [ObservableProperty]
    private string numeroCuenta = string.Empty;

    [ObservableProperty]
    private string banco = string.Empty;
}
