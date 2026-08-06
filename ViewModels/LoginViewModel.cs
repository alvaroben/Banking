using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetBankingApp.Services;
using InternetBankingApp.Views;

namespace InternetBankingApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string usuario = string.Empty;

    [ObservableProperty]
    private string contrasena = string.Empty;

    [ObservableProperty]
    private string? usuarioError;

    [ObservableProperty]
    private string? contrasenaError;

    [ObservableProperty]
    private string? errorMensaje;

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        if (!Validar())
        {
            return;
        }

        ErrorMensaje = null;

        if (!_authService.Login(Usuario.Trim(), Contrasena))
        {
            ErrorMensaje = "Usuario o contraseña incorrectos.";
            return;
        }

        Usuario = string.Empty;
        Contrasena = string.Empty;

        Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
    }

    private bool Validar()
    {
        var esValido = true;

        UsuarioError = null;
        if (string.IsNullOrWhiteSpace(Usuario))
        {
            UsuarioError = "El usuario es obligatorio.";
            esValido = false;
        }

        ContrasenaError = null;
        if (string.IsNullOrWhiteSpace(Contrasena))
        {
            ContrasenaError = "La contraseña es obligatoria.";
            esValido = false;
        }
        else if (Contrasena.Length < 4)
        {
            ContrasenaError = "La contraseña debe tener al menos 4 caracteres.";
            esValido = false;
        }

        return esValido;
    }
}
