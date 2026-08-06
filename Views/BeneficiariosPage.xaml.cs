using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class BeneficiariosPage : ContentPage
{
    private readonly BeneficiariosViewModel _viewModel;

    public BeneficiariosPage(BeneficiariosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Inicialización lazy de SQLite + carga de la lista persistida.
        await _viewModel.CargarAsync();
    }
}
