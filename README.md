# InternetBankingApp

Aplicación de banca por internet (*internet banking*) de demostración, construida con **.NET MAUI**. Simula las operaciones básicas de un cliente bancario: iniciar sesión, gestionar cuentas, solicitar préstamos, administrar beneficiarios y realizar transferencias.

> ⚠️ **Proyecto de demostración / académico.** Todos los datos viven en memoria durante la ejecución (no hay base de datos ni backend) y las credenciales están fijas en el código. No debe usarse en producción.

## ¿Qué hace la app?

La aplicación arranca en una pantalla de **Login**. Tras autenticarse, se habilita un menú lateral (*flyout*) con acceso a las distintas secciones:

| Sección | Descripción |
|---------|-------------|
| 🏦 **Cuentas** | Lista las cuentas del usuario y permite solicitar nuevas cuentas (Ahorro o Corriente) con un saldo inicial. Al aprobarse se genera automáticamente un número de cuenta único. |
| 💰 **Préstamos** | Muestra los préstamos solicitados y permite pedir nuevos indicando producto, monto y plazo en meses. |
| 👥 **Beneficiarios** | Administra la lista de beneficiarios (nombre, número de cuenta y banco) a los que se puede transferir. |
| 🔁 **Transferencias** | Registra transferencias desde una cuenta propia hacia un beneficiario. Descuenta el monto del saldo de la cuenta origen y valida que haya fondos suficientes. Requiere tener al menos una cuenta y un beneficiario. |
| 🚪 **Cerrar sesión** | Cierra la sesión y regresa a la pantalla de Login. |

### Detalles de comportamiento

- **Autenticación:** credenciales fijas (`admin` / `admin123`) gestionadas por `AuthService`.
- **Datos en memoria:** `BankingDataService` mantiene las colecciones de cuentas, préstamos, beneficiarios y transferencias mediante `ObservableCollection`, de modo que la interfaz se actualiza automáticamente. Los datos se pierden al cerrar la app.
- **Validaciones:** cada formulario valida campos obligatorios y formatos numéricos (saldo, monto, plazo).
- **Simulación de procesamiento:** las solicitudes de cuenta y préstamo muestran una animación de carga y un retraso de ~3 segundos para simular la aprobación.
- **Reglas de transferencia:** el monto debe ser positivo y no puede exceder el saldo disponible de la cuenta origen.

## Arquitectura

Proyecto .NET MAUI organizado en capas simples:

```
├── Models/           # Entidades del dominio
│   ├── Cuenta.cs         (con enum TipoCuenta: Ahorro / Corriente)
│   ├── Prestamo.cs
│   ├── Beneficiario.cs
│   └── Transferencia.cs
├── Services/         # Lógica y estado de la aplicación
│   ├── AuthService.cs           (autenticación)
│   └── BankingDataService.cs    (almacén de datos en memoria + reglas de negocio)
├── Views/            # Páginas de UI (XAML + code-behind)
│   ├── LoginPage
│   ├── CuentasPage
│   ├── PrestamosPage
│   ├── BeneficiariosPage
│   └── TransferenciasPage
├── Controls/         # Controles reutilizables (LoadingDots)
├── Resources/        # Estilos, colores, fuentes, íconos e imágenes
├── Platforms/        # Código específico por plataforma
├── AppShell.xaml     # Navegación (Shell + flyout)
└── MauiProgram.cs    # Configuración e inyección de dependencias
```

- **Navegación:** basada en `Shell`. El *flyout* permanece oculto hasta iniciar sesión.
- **Inyección de dependencias:** `AuthService` y `BankingDataService` se registran como *singletons*; las páginas como *transient*.
- **Patrón:** las páginas usan *code-behind* directo (sin MVVM completo), enlazando las colecciones del servicio a las listas de la UI.

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
