using InternetBankingApp.ViewModels;

namespace InternetBankingApp.Views;

public partial class BeneficiariosPage : ContentPage
{
    public BeneficiariosPage(BeneficiariosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
