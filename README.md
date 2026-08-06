# InternetBankingApp

Aplicación de banca por internet (*internet banking*) construida con **.NET MAUI** y patrón **MVVM**. Simula las operaciones de un cliente bancario: iniciar sesión, gestionar cuentas, solicitar préstamos y pagarlos cuota a cuota, administrar beneficiarios, transferir dinero, programar transferencias recurrentes y consultar un panel financiero.

> ⚠️ **Proyecto de demostración / académico.** Las credenciales están fijas en el código y no hay backend: los datos se guardan localmente en SQLite, en el almacenamiento privado de la app. No debe usarse en producción.

## ¿Qué hace la app?

La aplicación arranca en una pantalla de **Login**. Tras autenticarse, se habilita un menú lateral (*flyout*) con acceso a las distintas secciones:

| Sección | Descripción |
|---------|-------------|
| 📊 **Inicio** | Panel financiero: patrimonio neto (saldo − deuda), saldo total, deuda por préstamos, cuota mensual comprometida, gráfico de transferencias por mes y ranking de beneficiarios. También ejecuta las transferencias programadas vencidas. |
| 🏦 **Cuentas** | Lista las cuentas del usuario y permite solicitar nuevas (Ahorro o Corriente) con un saldo inicial. Al aprobarse se genera automáticamente un número de cuenta único de 10 dígitos. |
| 💰 **Préstamos** | Catálogo de productos con tasa anual fija, simulador de cuota en vivo y, por cada préstamo, un plan de pagos con tabla de amortización y pago de cuotas. |
| 👥 **Beneficiarios** | Administra la lista de beneficiarios (nombre, número de cuenta y banco) a los que se puede transferir. El número de cuenta no se puede repetir. |
| 🔁 **Transferencias** | Registra transferencias desde una cuenta propia hacia un beneficiario. Descuenta el monto del saldo de la cuenta origen y valida que haya fondos suficientes. |
| 🗓️ **Programadas** | Órdenes permanentes de transferencia (semanal, quincenal o mensual) que se ejecutan solas al vencer, incluidas las ocurrencias atrasadas. |
| 🚪 **Cerrar sesión** | Cierra la sesión y regresa a la pantalla de Login. |

### Detalles de comportamiento

- **Autenticación:** credenciales fijas (`admin` / `admin123`) gestionadas por `AuthService`.
- **Persistencia:** `BankingDataService` guarda todo en un archivo SQLite (`internetbanking.db3`) mediante `SQLiteAsyncConnection`. La conexión se abre de forma perezosa desde el `OnAppearing` de cada lista y las tablas se crean en el primer arranque.
- **Integridad:** las operaciones que tocan varias tablas (transferir, pagar una cuota, editar una transferencia) se ejecutan dentro de una transacción, releyendo el saldo desde la base para no confiar en la copia en pantalla.
- **Unicidad:** el número de cuenta (propio y de beneficiario) es `[Unique]`, y las órdenes programadas tienen un índice único compuesto (cuenta + beneficiario + concepto). El rechazo se captura como `SQLiteException/Constraint` y se muestra debajo del campo.
- **Validaciones:** cada formulario valida campo por campo y muestra el error inline.
- **Simulación de procesamiento:** las solicitudes de cuenta y préstamo muestran una animación de carga de ~3 segundos para simular la aprobación.

## Identidad visual

El logotipo es un **escudo con tres barras ascendentes**: el escudo por la protección del dinero y
las barras por el crecimiento del patrimonio, que es justamente lo que mide el dashboard. Está
dibujado en SVG (`Resources/AppIcon/appiconfg.svg`) sobre el navy institucional `#0B3550`, con la
figura contenida dentro del círculo seguro que recortan los lanzadores de Android. El mismo trazo se
reutiliza en la pantalla de arranque, en el login y en el encabezado del menú lateral.

Paleta: navy `#0B3550` / `#06243B` para superficies de marca, esmeralda `#10B981` / `#047857` como
acento de crecimiento, y `#DC2626` para errores y acciones destructivas.

## Arquitectura

Proyecto .NET MAUI con MVVM (CommunityToolkit.Mvvm):

```text
├── Models/           # Entidades del dominio (mapeadas a tablas SQLite)
│   ├── Cuenta.cs                  (enum TipoCuenta: Ahorro / Corriente)
│   ├── Prestamo.cs                (cuota, intereses y saldo pendiente calculados)
│   ├── Beneficiario.cs
│   ├── Transferencia.cs           (enum OrigenTransferencia: Manual / Programada)
│   ├── TransferenciaProgramada.cs (orden permanente con frecuencia)
│   ├── PagoPrestamo.cs            (cuota saldada con desglose capital/interés)
│   ├── CuotaAmortizacion.cs       (fila calculada, no persistida)
│   └── ResumenFinanciero.cs       (resultados de las consultas agregadas)
├── Services/
│   ├── AuthService.cs             (autenticación)
│   ├── BankingDataService.cs      (acceso a datos SQLite + reglas de negocio)
│   ├── ProgramacionesService.cs   (motor de transferencias programadas)
│   └── AmortizacionService.cs     (catálogo de productos y matemática financiera)
├── ViewModels/       # Un ViewModel por página (estado, comandos y validaciones)
├── Views/            # Páginas de UI (XAML + code-behind)
│   ├── LoginPage, DashboardPage, CuentasPage, PrestamosPage,
│   ├── PrestamoDetallePage (ruta con parámetro), BeneficiariosPage,
│   └── TransferenciasPage, ProgramadasPage
├── Controls/         # LoadingDots y GraficoBarrasDrawable (gráfico con Maui.Graphics)
├── Converters/       # InverseBoolConverter, StringNotEmptyConverter
├── Resources/        # Estilos, colores, fuentes, íconos e imágenes
├── Platforms/        # Código específico por plataforma
├── AppShell.xaml     # Navegación (Shell + flyout)
└── MauiProgram.cs    # Configuración e inyección de dependencias
```

- **Navegación:** basada en `Shell`. El *flyout* permanece oculto hasta iniciar sesión. El detalle de préstamo se registra como ruta con parámetro (`prestamoId`).
- **Inyección de dependencias:** `AuthService`, `BankingDataService` y `ProgramacionesService` son *singletons*; páginas y ViewModels, *transient*.
- **Patrón:** MVVM con `ObservableObject`, `[ObservableProperty]` y `[RelayCommand]`; las páginas solo enlazan y llaman `CargarAsync()` en su `OnAppearing`.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/) con la carga de trabajo de MAUI (`dotnet workload install maui`).
- Plataformas objetivo: **Android**, **iOS**, **macOS (Mac Catalyst)** y **Windows**.

## Ejecutar

```bash
# Restaurar dependencias
dotnet restore

# Ejecutar (ejemplo en Mac Catalyst)
dotnet build -t:Run -f net10.0-maccatalyst

# Otros targets disponibles:
#   -f net10.0-android
#   -f net10.0-ios
#   -f net10.0-windows10.0.19041.0
```

### Credenciales de acceso

| Usuario | Contraseña |
|---------|-----------|
| `admin` | `admin123` |

## Documentación

- [Documento-Entrega-Final.md](Documento-Entrega-Final.md) — persistencia con SQLite y funcionalidades innovadoras (Actividad 6).
- [Documento-Validaciones-MVVM.md](Documento-Validaciones-MVVM.md) — validaciones y refactorización a MVVM (Actividad 5).
