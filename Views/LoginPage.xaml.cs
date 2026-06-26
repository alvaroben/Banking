using InternetBankingApp.Services;

namespace InternetBankingApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnIniciarSesionClicked(object? sender, EventArgs e)
    {
        var usuario = UsuarioEntry.Text?.Trim() ?? string.Empty;
        var contrasena = ContrasenaEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
        {
            MostrarError("Usuario y contraseña son obligatorios.");
            return;
        }

        if (!_authService.Login(usuario, contrasena))
        {
            MostrarError("Usuario o contraseña incorrectos.");
            return;
        }

        ErrorLabel.IsVisible = false;
        UsuarioEntry.Text = string.Empty;
        ContrasenaEntry.Text = string.Empty;

        Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        await Shell.Current.GoToAsync($"//{nameof(CuentasPage)}");
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}
