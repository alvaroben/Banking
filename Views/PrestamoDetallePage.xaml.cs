using InternetBankingApp.Models;
using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class PrestamoDetallePage : ContentPage
{
    private readonly PrestamoDetalleViewModel _viewModel;

    public PrestamoDetallePage(PrestamoDetalleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        CuentaPicker.ItemDisplayBinding = new Binding(nameof(Cuenta.Resumen));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Shell ya asignó PrestamoId desde el parámetro de la ruta antes de llegar aquí.
        await _viewModel.CargarAsync();
    }
}
