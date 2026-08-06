using InternetBankingApp.Models;
using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class ProgramadasPage : ContentPage
{
    private readonly ProgramadasViewModel _viewModel;

    public ProgramadasPage(ProgramadasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        CuentaPicker.ItemDisplayBinding = new Binding(nameof(Cuenta.Resumen));
        BeneficiarioPicker.ItemDisplayBinding = new Binding(nameof(Beneficiario.Resumen));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarAsync();
    }
}
