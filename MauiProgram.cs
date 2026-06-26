using InternetBankingApp.Services;
using InternetBankingApp.Views;
using Microsoft.Extensions.Logging;

namespace InternetBankingApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<BankingDataService>();

		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<CuentasPage>();
		builder.Services.AddTransient<PrestamosPage>();
		builder.Services.AddTransient<BeneficiariosPage>();
		builder.Services.AddTransient<MovimientosPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
