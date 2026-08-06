# InternetBankingApp — Documento de entrega final

**Actividad 6: Persistencia con SQLite e innovación**
Álvaro Bencosme · .NET MAUI (net10.0) · Patrón MVVM

---

## 1. Qué aplicación construí y cuál es su propósito

**InternetBankingApp** es una aplicación de banca por internet para móvil y escritorio. Su propósito
es que una persona pueda manejar su vida financiera desde un solo lugar: abrir cuentas, guardar los
beneficiarios a los que le transfiere, mover dinero entre ellos, solicitar préstamos y entender de
un vistazo cómo está parada financieramente.

La app no es solo un CRUD de formularios: el dinero se mueve de verdad dentro de la aplicación. Una
transferencia descuenta el saldo de la cuenta origen, borrarla lo devuelve, pagar una cuota de
préstamo debita la cuenta elegida y todo eso queda guardado en una base de datos SQLite que
sobrevive al cierre de la aplicación.

**Acceso de prueba:** usuario `admin`, contraseña `admin123`.

---

## 2. Funcionalidades que tiene la app al terminar el curso

### 2.1 Módulos base

| Módulo | Qué permite |
|---|---|
| **Login** | Autenticación con validación inline de usuario y contraseña. Al cerrar sesión el menú lateral se bloquea. |
| **Cuentas** | Solicitar cuentas de Ahorro o Corriente. El número de 10 dígitos lo genera el banco y es único. Editar y eliminar. |
| **Beneficiarios** | Registrar a quién se le transfiere (nombre, cuenta destino, banco). La cuenta destino es única. |
| **Transferencias** | Transferir de una cuenta propia a un beneficiario, con validación de fondos. Editar y eliminar revirtiendo saldos. |
| **Préstamos** | Solicitar préstamos del catálogo del banco, con tasa por producto y cuota calculada. |

Todos los formularios validan campo por campo y muestran el error **debajo del campo**
correspondiente (no en un cuadro de diálogo genérico), tal como se definió en la actividad anterior.

### 2.2 Identidad visual

La app dejó de usar los recursos de la plantilla de .NET MAUI y tiene logotipo propio: un **escudo
con tres barras ascendentes** —el escudo por la protección del dinero, las barras por el crecimiento
del patrimonio, que es justo lo que mide el dashboard—. Está dibujado en SVG sobre el navy
institucional `#0B3550`, con la figura dentro del círculo seguro que recortan los lanzadores de
Android, y se reutiliza en el ícono de la app, la pantalla de arranque, el login y el encabezado del
menú lateral. Todas las tarjetas de las listas comparten el mismo tratamiento (fondo blanco, esquinas
de 12 px y borde gris claro) para que las pantallas nuevas y las viejas se vean como una sola app.

### 2.3 Persistencia con SQLite (Actividad 6)

- Paquete **`sqlite-net-pcl`** (1.9.172) con el motor nativo `SQLitePCLRaw.bundle_green`.
- Seis tablas: `Cuentas`, `Beneficiarios`, `Prestamos`, `Transferencias`,
  `TransferenciasProgramadas` y `PagosPrestamo`.
- Cada entidad tiene un `Id` `[PrimaryKey]` `[AutoIncrement]`, y las columnas que no deben repetirse
  están marcadas con `[Unique]`. Las propiedades calculadas llevan `[Ignore]` para que no intenten
  guardarse como columnas.
- El servicio de datos usa **`SQLiteAsyncConnection`** y todos sus métodos son `async`.
- La base de datos se abre de forma **lazy**: la primera pantalla que aparece llama a
  `InicializarAsync()` desde su `OnAppearing`, y a partir de ahí la conexión se reutiliza.
- Las operaciones que tocan más de una tabla (transferir, pagar una cuota, editar una transferencia)
  van dentro de una **transacción**: o se completan enteras o no dejan rastro.
- El intento de guardar un valor duplicado en un campo único se captura como
  `SQLiteException` con `Result.Constraint` y se muestra **inline, debajo del campo**.

### 2.4 Funcionalidades innovadoras

#### A. Dashboard financiero con gráfico propio

Pantalla de inicio que responde "¿cómo estoy?" sin que el usuario tenga que sacar cuentas:

- **Patrimonio neto** = saldo disponible − deuda vigente de préstamos.
- Tarjetas de saldo total, deuda por préstamos, cuota mensual comprometida y total transferido.
- **Gráfico de barras de transferencias por mes**, dibujado a mano con `Microsoft.Maui.Graphics`
  (`IDrawable` + `GraphicsView`), sin librerías externas: escala automática según el mes más alto,
  barra destacada para el mes de mayor movimiento y rótulos de monto y cantidad de operaciones.
- **Ranking de beneficiarios** que más dinero han recibido.
- Los agregados los calcula **SQLite**, no la app: `SUM()`, `COUNT()` y
  `GROUP BY strftime('%Y-%m', Fecha)`. Para que ese `strftime` funcione, la conexión se abre con
  `storeDateTimeAsTicks: false`, de modo que las fechas se guardan como texto ISO.

#### B. Transferencias programadas (órdenes permanentes) con motor de ejecución

- El usuario define una orden: cuenta origen, beneficiario, concepto, monto, frecuencia
  (semanal / quincenal / mensual) y fecha de la primera ejecución.
- Un **motor** (`ProgramacionesService`) revisa al abrir el dashboard qué órdenes vencieron y las
  ejecuta solo, **incluidas las ocurrencias atrasadas** de los días en que la app estuvo cerrada
  (por ejemplo, si estuvo dos meses sin abrirse, cobra las dos mensualidades pendientes).
- Cada ocurrencia se registra con **la fecha en que le tocaba**, no con la de hoy, para que el
  historial y el gráfico por mes queden correctos.
- Si una orden no puede cobrarse (fondos insuficientes, o el beneficiario fue eliminado), no falla en
  silencio: **se pausa con su motivo** y el usuario recibe el detalle al entrar.
- Tope de seguridad de 12 ocurrencias por orden y por corrida, para que una app abandonada por un
  año no vacíe una cuenta de un golpe.
- Las órdenes se pueden pausar, reanudar, editar, eliminar y forzar con "Ejecutar vencidas".
- La unicidad aquí es un **índice compuesto** (cuenta + beneficiario + concepto): no tiene sentido
  tener dos órdenes idénticas compitiendo por el mismo saldo.

#### C. Simulador y plan de pagos de préstamos

- **Catálogo de productos** con su tasa anual fija (personal 18.5%, vehículo 12.9%, hipotecario
  9.75%, educativo 8.5%, comercial 15.25%) y su monto máximo, que se valida al solicitar.
- **Simulador en vivo**: mientras el usuario escribe monto y plazo, el formulario va mostrando la
  cuota mensual, los intereses totales y el total a pagar.
- **Tabla de amortización completa** por el sistema francés (cuota fija): cuota por cuota, cuánto es
  capital, cuánto es interés y qué balance queda. La última cuota absorbe el redondeo acumulado para
  que el balance cierre exactamente en cero.
- **Pago de cuotas real**: se elige la cuenta, se debita el monto, se registra el pago con su
  desglose capital/interés en la tabla `PagosPrestamo` y avanza la barra de progreso del préstamo.
  Todo dentro de una transacción.
- Filtro para ver solo las cuotas pendientes o la tabla completa, e historial de pagos realizados.

---

## 3. Qué fue lo más difícil y cómo lo resolví

**1. Que el saldo nunca quedara inconsistente.**
Una transferencia toca dos tablas: baja el saldo de la cuenta y crea el registro del movimiento. Con
listas en memoria eso era trivial; con base de datos, si la app se cerraba entre una escritura y la
otra, el dinero podía desaparecer o duplicarse. Lo resolví metiendo esas operaciones en
`RunInTransactionAsync` y, dentro de la transacción, **releyendo el saldo desde la base** en vez de
confiar en la copia que tenía la pantalla. Para el caso de editar una transferencia (que primero
devuelve el monto viejo y después cobra el nuevo) uso una excepción propia,
`SaldoInsuficienteException`, lanzada dentro de la transacción: sqlite-net hace rollback de todo y el
método devuelve `false` sin dejar nada a medias.

**2. Los atributos de SQLite sobre propiedades generadas por el MVVM Toolkit.**
Los modelos usan `[ObservableProperty]` sobre campos privados, y el Toolkit genera la propiedad
pública. El problema es que `[Unique]` hay que ponerlo sobre **la propiedad**, no sobre el campo. La
solución fue usar el objetivo explícito `[property: Unique]`, que le dice al generador que traslade
ese atributo a la propiedad que crea.

**3. Agrupar por mes dentro de SQLite.**
Quería que el gráfico saliera de un `GROUP BY` real y no de traer todas las transferencias a memoria.
Pero sqlite-net, por defecto, guarda las fechas como *ticks* (un número enorme), y `strftime()` no
puede leer eso. Lo resolví abriendo la conexión con `storeDateTimeAsTicks: false`, que guarda las
fechas como texto ISO y deja que `strftime('%Y-%m', Fecha)` agrupe directamente en SQL.

**4. La aritmética de la amortización.**
Al calcular capital = cuota − interés mes a mes, el redondeo a dos decimales hacía que el balance
final terminara en unos centavos en lugar de cero. Lo resolví haciendo que la última cuota liquide
exactamente el balance que quede vivo, ajustando su monto. Lo verifiqué con una prueba: la suma de
todos los capitales da exactamente el monto prestado y el balance final es 0.00.

**5. Probar la persistencia sin poder desplegar la app.**
Esta Mac no puede desplegar la app (la versión de Xcode instalada no coincide con la que pide el SDK
de MacCatalyst, y no hay Android SDK). Para no entregar la persistencia sin comprobar, armé un
pequeño programa de consola que compila **los mismos archivos** de modelos y servicios y ejercita
todo el flujo en dos procesos distintos —uno que escribe y otro que lee, que es exactamente
"cerrar la app y volver a abrirla"—. El resultado fue: los datos siguen ahí, el duplicado se rechaza
con `Constraint`, el motor se pone al día con las tres mensualidades atrasadas, los agregados de SQL
cuadran y la edición sin fondos revierte sin tocar el saldo.

---

## 4. Qué le agregaría si tuviera más tiempo

- **Exportar el estado de cuenta a CSV o PDF** y compartirlo con el `Share` de MAUI, con filtros por
  rango de fechas, cuenta y beneficiario.
- **Bloqueo biométrico** (Face ID / huella) al abrir la app, con PIN de respaldo, en lugar del login
  de usuario y contraseña fijos.
- **Notificaciones locales** que avisen un día antes de que se ejecute una orden programada o venza
  una cuota de préstamo, para que el usuario no se entere solo al abrir la app.
- **Presupuestos y alertas**: fijar un tope de gasto mensual y avisar cuando se acerque, aprovechando
  que ya tengo los agregados por mes.
- **Sincronización con una API real** y multiusuario, dejando SQLite como caché local para trabajar
  sin conexión.
- **Pruebas unitarias formales** con xUnit sobre `AmortizacionService` y `ProgramacionesService`, en
  lugar del programa de consola que usé para verificar.

---

## 5. Por qué escogí estas funcionalidades innovadoras

Las tres salen de la misma pregunta: *¿qué haría que alguien abriera esta app y no solo la lista de
movimientos de su banco?*

**El dashboard**, porque los datos ya estaban ahí pero no decían nada. Un usuario tenía cuentas por
un lado y préstamos por otro, y en ningún lugar la respuesta a "¿cuánto tengo realmente?". El
patrimonio neto (saldo − deuda) es esa respuesta en un solo número. Además me obligaba a hacer algo
técnicamente interesante: mover el cálculo al motor de base de datos con `SUM` y `GROUP BY` en vez de
sumar en C#, y dibujar el gráfico yo mismo con `Microsoft.Maui.Graphics` en lugar de arrastrar una
librería externa.

**Las transferencias programadas**, porque atacan el trabajo repetitivo real: el alquiler, la
mensualidad del colegio, la cuota que se paga todos los meses. Es la funcionalidad que convierte la
app en algo que trabaja **cuando el usuario no la está mirando**. Es también la más compleja: no basta
con guardar una fecha, hay que ponerse al día con las ocurrencias que vencieron mientras la app
estaba cerrada, decidir qué hacer cuando no hay fondos y protegerse de ejecutar cargos retroactivos
sin control.

**El simulador de amortización**, porque en la versión anterior un préstamo era solo un monto y un
plazo guardados, sin intereses ni cuota: el usuario no sabía lo que realmente iba a pagar. Aquí entra
la matemática financiera de verdad —cuota francesa, desglose capital/interés, costo total del
crédito— y le da al usuario la información que un banco suele esconder en letra pequeña. Sumado al
pago de cuotas, cierra el ciclo: el préstamo deja de ser un registro y pasa a ser algo que se paga y
se termina.

---

## 6. Cómo verificar la persistencia (guía para la demostración)

1. Iniciar sesión con `admin` / `admin123`.
2. Solicitar una cuenta (por ejemplo Ahorro con saldo 50000) y registrar un beneficiario.
3. Hacer una transferencia y comprobar que el saldo de la cuenta baja.
4. **Cerrar la aplicación por completo y volver a abrirla.** La cuenta, el beneficiario, la
   transferencia y el saldo ya descontado siguen ahí.
5. Intentar registrar un segundo beneficiario con el **mismo número de cuenta**: aparece el mensaje
   *"Ya existe un beneficiario registrado con ese número de cuenta"* debajo del campo, generado al
   capturar la excepción de unicidad de SQLite.
6. Programar una transferencia con fecha de primera ejecución en el pasado, entrar a **Inicio** y ver
   el aviso de las ocurrencias que el motor ejecutó automáticamente.
7. Solicitar un préstamo, abrir **Plan de pagos**, pagar una cuota y comprobar que el saldo de la
   cuenta baja y la barra de progreso avanza. Al reabrir la app, el pago sigue registrado.
