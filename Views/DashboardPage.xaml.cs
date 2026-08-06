using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Inicialización lazy: la primera vez abre la base de datos y crea las tablas; además
        // ejecuta las transferencias programadas que hayan vencido mientras la app estuvo cerrada.
        await _viewModel.CargarAsync();

        // El GraphicsView no se entera solo de que cambiaron los datos del drawable.
        GraficoTransferencias.Invalidate();
    }
}
