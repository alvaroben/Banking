using System.ComponentModel;
using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class PrestamosPage : ContentPage
{
    private readonly PrestamosViewModel _viewModel;

    public PrestamosPage(PrestamosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Inicialización lazy de SQLite + carga de los préstamos persistidos (y sus cuotas pagadas).
        await _viewModel.CargarAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PrestamosViewModel.IsBusy))
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
