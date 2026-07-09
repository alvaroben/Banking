using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;

namespace InternetBankingApp.ViewModels;

public partial class PrestamosViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;

    public PrestamosViewModel(BankingDataService dataService)
    {
        _dataService = dataService;
        Prestamos = _dataService.Prestamos;
    }

    public ObservableCollection<Prestamo> Prestamos { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    private bool isFormVisible;

    public string ToggleButtonText => IsFormVisible ? "Cancelar" : "+ Solicitar préstamo";

    [ObservableProperty]
    private string producto = string.Empty;

    [ObservableProperty]
    private string monto = string.Empty;

    [ObservableProperty]
    private string plazo = string.Empty;

    [ObservableProperty]
    private string? productoError;

    [ObservableProperty]
    private string? montoError;

    [ObservableProperty]
    private string? plazoError;

    [ObservableProperty]
    private bool isBusy;

    [RelayCommand]
    private void ToggleForm() => IsFormVisible = !IsFormVisible;

    [RelayCommand]
    private async Task SolicitarAsync()
    {
        if (!Validar())
        {
            return;
        }

        IsBusy = true;
        await Task.Delay(3000);
        IsBusy = false;

        var prestamo = new Prestamo
        {
            Producto = Producto.Trim(),
            MontoSolicitado = decimal.Parse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture),
            PlazoMeses = int.Parse(Plazo, CultureInfo.InvariantCulture)
        };

        _dataService.AgregarPrestamo(prestamo);

        LimpiarFormulario();

        await Shell.Current.DisplayAlert(
            "Préstamo aprobado",
            $"Tu préstamo de {prestamo.Producto} por {prestamo.MontoSolicitado:C2} a {prestamo.PlazoMeses} meses fue aprobado.",
            "Aceptar");
    }

    private bool Validar()
    {
        var esValido = true;

        ProductoError = null;
        if (string.IsNullOrWhiteSpace(Producto))
        {
            ProductoError = "El producto es obligatorio.";
            esValido = false;
        }
        else if (Producto.Trim().Length < 3)
        {
            ProductoError = "El producto debe tener al menos 3 caracteres.";
            esValido = false;
        }

        MontoError = null;
        if (string.IsNullOrWhiteSpace(Monto))
        {
            MontoError = "El monto solicitado es obligatorio.";
            esValido = false;
        }
        else if (!decimal.TryParse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture, out var monto))
        {
            MontoError = "El monto solicitado debe ser un número válido.";
            esValido = false;
        }
        else if (monto <= 0)
        {
            MontoError = "El monto solicitado debe ser mayor a cero.";
            esValido = false;
        }
        else if (monto > 1_000_000)
        {
            MontoError = "El monto solicitado no puede exceder RD$1,000,000.00.";
            esValido = false;
        }

        PlazoError = null;
        if (string.IsNullOrWhiteSpace(Plazo))
        {
            PlazoError = "El plazo es obligatorio.";
            esValido = false;
        }
        else if (!int.TryParse(Plazo, out var plazo))
        {
            PlazoError = "El plazo debe ser un número entero válido.";
            esValido = false;
        }
        else if (plazo < 6 || plazo > 360)
        {
            PlazoError = "El plazo debe estar entre 6 y 360 meses.";
            esValido = false;
        }

        return esValido;
    }

    private void LimpiarFormulario()
    {
        Producto = string.Empty;
        Monto = string.Empty;
        Plazo = string.Empty;
        ProductoError = null;
        MontoError = null;
        PlazoError = null;
        IsFormVisible = false;
    }
}
