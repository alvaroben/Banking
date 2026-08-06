using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Models;
using InternetBankingApp.Services;
using InternetBankingApp.Views;

namespace InternetBankingApp.ViewModels;

public partial class PrestamosViewModel : ObservableObject
{
    private readonly BankingDataService _dataService;

    public PrestamosViewModel(BankingDataService dataService)
    {
        _dataService = dataService;
    }

    public ObservableCollection<Prestamo> Prestamos { get; } = [];

    /// <summary>Catálogo del banco: cada producto trae su tasa anual y su tope de monto.</summary>
    public IReadOnlyList<ProductoPrestamo> Productos { get; } = AmortizacionService.Catalogo;

    [ObservableProperty]
    private bool isCargando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleButtonText))]
    private bool isFormVisible;

    public string ToggleButtonText => IsFormVisible ? "Cancelar" : "+ Solicitar préstamo";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(GuardarButtonText))]
    private Prestamo? prestamoEnEdicion;

    public bool IsEditMode => PrestamoEnEdicion is not null;

    public string FormTitle => IsEditMode ? "Editar préstamo" : "Nuevo préstamo";

    public string GuardarButtonText => IsEditMode ? "Guardar cambios" : "Solicitar préstamo";

    [ObservableProperty]
    private ProductoPrestamo? productoSeleccionado;

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

    // ── Simulación en vivo: se recalcula mientras el usuario escribe ──

    [ObservableProperty]
    private bool simulacionVisible;

    [ObservableProperty]
    private decimal cuotaSimulada;

    [ObservableProperty]
    private decimal totalPagarSimulado;

    [ObservableProperty]
    private decimal totalInteresesSimulado;

    [ObservableProperty]
    private string tasaSimulada = string.Empty;

    partial void OnProductoSeleccionadoChanged(ProductoPrestamo? value) => ActualizarSimulacion();

    partial void OnMontoChanged(string value) => ActualizarSimulacion();

    partial void OnPlazoChanged(string value) => ActualizarSimulacion();

    /// <summary>
    /// Calcula la cuota estimada con lo que haya escrito hasta ahora. No valida ni muestra errores:
    /// si los datos aún no dan, simplemente esconde el recuadro de simulación.
    /// </summary>
    private void ActualizarSimulacion()
    {
        var hayDatos = ProductoSeleccionado is not null
            && decimal.TryParse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture, out var montoValor)
            && montoValor > 0
            && int.TryParse(Plazo, out var plazoValor)
            && plazoValor > 0;

        if (!hayDatos)
        {
            SimulacionVisible = false;
            return;
        }

        var capital = decimal.Parse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture);
        var meses = int.Parse(Plazo, CultureInfo.InvariantCulture);
        var tasa = ProductoSeleccionado!.TasaAnual;

        CuotaSimulada = AmortizacionService.CalcularCuota(capital, tasa, meses);
        TotalPagarSimulado = CuotaSimulada * meses;
        TotalInteresesSimulado = TotalPagarSimulado - capital;
        TasaSimulada = $"{tasa:0.##}% anual";
        SimulacionVisible = true;
    }

    public async Task CargarAsync()
    {
        IsCargando = true;
        try
        {
            await _dataService.InicializarAsync();

            var prestamos = await _dataService.ObtenerPrestamosAsync();

            Prestamos.Clear();
            foreach (var prestamo in prestamos)
            {
                Prestamos.Add(prestamo);
            }
        }
        finally
        {
            IsCargando = false;
        }
    }

    [RelayCommand]
    private void ToggleForm()
    {
        if (IsFormVisible)
        {
            IsFormVisible = false;
            LimpiarCampos();
        }
        else
        {
            LimpiarCampos();
            IsFormVisible = true;
        }
    }

    [RelayCommand]
    private void Editar(Prestamo prestamo)
    {
        PrestamoEnEdicion = prestamo;
        ProductoSeleccionado = AmortizacionService.BuscarProducto(prestamo.Producto);
        Monto = prestamo.MontoSolicitado.ToString(CultureInfo.InvariantCulture);
        Plazo = prestamo.PlazoMeses.ToString(CultureInfo.InvariantCulture);
        ProductoError = null;
        MontoError = null;
        PlazoError = null;
        IsFormVisible = true;
    }

    /// <summary>Abre el plan de pagos (tabla de amortización) del préstamo.</summary>
    [RelayCommand]
    private async Task VerDetalleAsync(Prestamo prestamo)
    {
        await Shell.Current.GoToAsync($"{nameof(PrestamoDetallePage)}?prestamoId={prestamo.Id}");
    }

    [RelayCommand]
    private async Task EliminarAsync(Prestamo prestamo)
    {
        var confirmar = await Shell.Current.DisplayAlertAsync(
            "Eliminar préstamo",
            $"¿Seguro que deseas eliminar el préstamo \"{prestamo.Producto}\"? Se borrará también su historial de pagos.",
            "Eliminar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        await _dataService.EliminarPrestamoAsync(prestamo);
        Prestamos.Remove(prestamo);

        if (PrestamoEnEdicion == prestamo)
        {
            IsFormVisible = false;
            LimpiarCampos();
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!Validar())
        {
            return;
        }

        var capital = decimal.Parse(Monto, NumberStyles.Number, CultureInfo.InvariantCulture);
        var meses = int.Parse(Plazo, CultureInfo.InvariantCulture);
        var producto = ProductoSeleccionado!;

        if (IsEditMode)
        {
            var prestamo = PrestamoEnEdicion!;

            if (prestamo.CuotasPagadas > 0 && prestamo.CuotasPagadas > meses)
            {
                PlazoError = $"Ya pagaste {prestamo.CuotasPagadas} cuotas: el plazo no puede ser menor.";
                return;
            }

            prestamo.Producto = producto.Nombre;
            prestamo.TasaAnual = producto.TasaAnual;
            prestamo.MontoSolicitado = capital;
            prestamo.PlazoMeses = meses;

            await _dataService.GuardarPrestamoAsync(prestamo);

            IsFormVisible = false;
            LimpiarCampos();
            await CargarAsync();

            await Shell.Current.DisplayAlertAsync(
                "Préstamo actualizado",
                $"Los datos del préstamo \"{prestamo.Producto}\" fueron actualizados.",
                "Aceptar");
            return;
        }

        IsBusy = true;
        await Task.Delay(3000);
        IsBusy = false;

        var nuevoPrestamo = new Prestamo
        {
            Producto = producto.Nombre,
            TasaAnual = producto.TasaAnual,
            MontoSolicitado = capital,
            PlazoMeses = meses
        };

        await _dataService.GuardarPrestamoAsync(nuevoPrestamo);

        IsFormVisible = false;
        LimpiarCampos();
        await CargarAsync();

        await Shell.Current.DisplayAlertAsync(
            "Préstamo aprobado",
            $"Tu {nuevoPrestamo.Producto.ToLowerInvariant()} por {nuevoPrestamo.MontoSolicitado:C2} fue aprobado a {nuevoPrestamo.TasaTexto}.\n\n" +
            $"Cuota mensual: {nuevoPrestamo.CuotaMensual:C2} durante {nuevoPrestamo.PlazoMeses} meses.",
            "Aceptar");
    }

    private bool Validar()
    {
        var esValido = true;

        ProductoError = null;
        if (ProductoSeleccionado is null)
        {
            ProductoError = "Selecciona un producto de préstamo.";
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
        else if (ProductoSeleccionado is not null && monto > ProductoSeleccionado.MontoMaximo)
        {
            MontoError = $"El monto máximo para {ProductoSeleccionado.Nombre.ToLowerInvariant()} es {ProductoSeleccionado.MontoMaximo:C2}.";
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

    private void LimpiarCampos()
    {
        PrestamoEnEdicion = null;
        ProductoSeleccionado = null;
        Monto = string.Empty;
        Plazo = string.Empty;
        ProductoError = null;
        MontoError = null;
        PlazoError = null;
        SimulacionVisible = false;
    }
}
