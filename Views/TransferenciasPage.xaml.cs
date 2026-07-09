using InternetBankingApp.Models;
using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class TransferenciasPage : ContentPage
{
    private readonly TransferenciasViewModel _viewModel;

    public TransferenciasPage(TransferenciasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        CuentaPicker.ItemDisplayBinding = new Binding(nameof(Cuenta.NumeroCuenta));
        BeneficiarioPicker.ItemDisplayBinding = new Binding(nameof(Beneficiario.Nombre));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ActualizarEstado();
    }
}
