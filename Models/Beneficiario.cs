using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace InternetBankingApp.Models;

[Table("Beneficiarios")]
public partial class Beneficiario : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumen))]
    private string nombre = string.Empty;

    /// <summary>
    /// Campo único de la entidad: no se admiten dos beneficiarios con la misma cuenta destino.
    /// La restricción vive en la base de datos, no solo en la validación del ViewModel.
    /// </summary>
    [ObservableProperty]
    [property: Unique]
    [property: MaxLength(12)]
    private string numeroCuenta = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumen))]
    private string banco = string.Empty;

    /// <summary>Texto que se muestra en los Picker de beneficiario. No se persiste.</summary>
    [Ignore]
    public string Resumen => $"{Nombre} · {Banco}";
}
