using Microsoft.Extensions.DependencyInjection;

namespace InternetBankingApp;

public partial class App : Application
{
	private readonly AppShell _appShell;

	// AppShell se resuelve aquí, después de InitializeComponent(), en vez de recibirse como
	// parámetro del constructor: si fuera parámetro, el contenedor de DI lo construiría (y con él,
	// el XAML de AppShell) ANTES de que este constructor corra InitializeComponent(), que es lo que
	// fusiona Colors.xaml/Styles.xaml en los recursos de la aplicación. Ese orden hacía que
	// StaticResource como "Tertiary" (usado directamente en Shell.FlyoutHeader) no existiera todavía
	// y tumbaba la app al arrancar.
	public App(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		_appShell = serviceProvider.GetRequiredService<AppShell>();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_appShell);
	}
}