using System.ComponentModel;
using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class CuentasPage : ContentPage
{
    private readonly CuentasViewModel _viewModel;

    public CuentasPage(CuentasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Inicialización lazy de SQLite + carga de las cuentas persistidas.
        await _viewModel.CargarAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CuentasViewModel.IsBusy))
        {
            return;
        }

        if (_viewModel.IsBusy)
        {
            LoadingIndicator.Start();
        }
        else
        {
            LoadingIndicator.Stop();
        }
    }
}
