namespace InternetBankingApp.Services;

public class AuthService
{
    private const string Usuario = "admin";
    private const string Contrasena = "admin123";

    public bool IsAuthenticated { get; private set; }

    public bool Login(string usuario, string contrasena)
    {
        IsAuthenticated = usuario == Usuario && contrasena == Contrasena;
        return IsAuthenticated;
    }

    public void Logout()
    {
        IsAuthenticated = false;
    }
}
