using InternetBankingApp.Services;
using InternetBankingApp.Views;

namespace InternetBankingApp;

public partial class AppShell : Shell
{
	private readonly AuthService _authService;

	public AppShell(AuthService authService)
	{
		InitializeComponent();
		_authService = authService;
	}

	private async void OnCerrarSesionClicked(object? sender, EventArgs e)
	{
		_authService.Logout();
		FlyoutBehavior = FlyoutBehavior.Disabled;
		await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
	}
}
