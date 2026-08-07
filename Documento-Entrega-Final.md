# InternetBankingApp — Documento de entrega final

**Actividad 6: Persistencia con SQLite e innovación**
Álvaro Bencosme · .NET MAUI (net10.0) · Patrón MVVM

---

## 1. Qué aplicación construí y cuál es su propósito

**InternetBankingApp** es una aplicación de banca por internet para móvil y escritorio. Su propósito
es que una persona pueda manejar su vida financiera desde un solo lugar: abrir cuentas, guardar los
beneficiarios a los que le transfiere, mover dinero entre ellos, solicitar préstamos y entender de
un vistazo cómo está parada financieramente.

No es solo un CRUD de formularios: el dinero se mueve de verdad. Una transferencia descuenta el
saldo de la cuenta origen, borrarla lo devuelve, pagar una cuota de préstamo debita la cuenta
elegida, y todo eso queda guardado en una base de datos **SQLite** que sobrevive al cierre de la
aplicación.

**Acceso de prueba:** usuario `admin`, contraseña `admin123`.

---

## 2. Funcionalidades al terminar el curso

| Módulo | Qué permite |
|---|---|
| **Login** | Autenticación con validación inline. Al cerrar sesión el menú lateral se bloquea. |
| **Cuentas** | Solicitar cuentas de Ahorro o Corriente (número único generado por el banco). Editar y eliminar. |
| **Beneficiarios** | Registrar a quién se le transfiere (nombre, cuenta destino, banco). Cuenta destino única. |
| **Transferencias** | Transferir entre cuenta propia y beneficiario, con validación de fondos. Editar y eliminar revirtiendo saldos. |
| **Préstamos** | Solicitar del catálogo del banco (tasa por producto), simulación en vivo y plan de pagos. |

Todos los formularios validan campo por campo, con el error mostrado **debajo del campo**, y la
persistencia corre sobre **SQLite** (`sqlite-net-pcl`): seis tablas, transacciones para las
operaciones que tocan más de una tabla (transferir, pagar una cuota, editar una transferencia), y
captura de `SQLiteException`/`Constraint` para mostrar los duplicados como error inline.

**Innovaciones sobre la base del curso:**

- **Dashboard financiero** — patrimonio neto (saldo − deuda), tarjetas resumen y un gráfico de
  barras de transferencias por mes dibujado a mano con `Microsoft.Maui.Graphics`, alimentado por
  `SUM`/`GROUP BY` calculados en SQLite, no en la app.
- **Transferencias programadas** — órdenes permanentes (semanal/quincenal/mensual) que un motor
  (`ProgramacionesService`) ejecuta solo al abrir la app, poniéndose al día con ocurrencias
  atrasadas y pausando (con motivo) las que no puede cobrar.
- **Simulador y plan de pagos de préstamos** — catálogo de productos con tasa fija, tabla de
  amortización por el sistema francés, y pago real de cuotas que debita la cuenta elegida.

---

## 3. Qué fue lo más difícil y cómo lo resolví

**Que el saldo nunca quedara inconsistente.** Una transferencia toca dos tablas: baja el saldo y
crea el movimiento. Lo resolví metiendo esas operaciones en `RunInTransactionAsync` y, dentro de la
transacción, releyendo el saldo desde la base en vez de confiar en la copia de la pantalla. Para
editar una transferencia (que primero devuelve el monto viejo y luego cobra el nuevo) uso una
excepción propia, `SaldoInsuficienteException`, lanzada dentro de la transacción para que sqlite-net
haga rollback completo si los fondos no alcanzan.

**Los atributos de SQLite sobre propiedades generadas por el MVVM Toolkit.** Los modelos usan
`[ObservableProperty]` sobre campos privados; `[Unique]` hay que ponerlo sobre la propiedad que
genera el Toolkit, no sobre el campo. Se resuelve con el objetivo explícito `[property: Unique]`.

**Agrupar por mes dentro de SQLite.** Quería que el gráfico saliera de un `GROUP BY` real, pero
sqlite-net guarda las fechas como *ticks* por defecto y `strftime()` no puede leerlas. Se resuelve
abriendo la conexión con `storeDateTimeAsTicks: false`, que guarda las fechas como texto ISO.

**La aritmética de la amortización.** Calcular capital = cuota − interés mes a mes acumulaba
redondeo y el balance final no cerraba en cero. Lo resolví haciendo que la última cuota liquide
exactamente el balance vivo, y lo verifiqué comprobando que la suma de capitales da el monto
prestado exacto.

---

## 4. Qué le agregaría si tuviera más tiempo

- **Exportar el estado de cuenta a CSV/PDF**, compartible con el `Share` de MAUI.
- **Bloqueo biométrico** (Face ID / huella) con PIN de respaldo, en lugar del login fijo.
- **Notificaciones locales** un día antes de una orden programada o el vencimiento de una cuota.
- **Presupuestos y alertas** de gasto mensual, aprovechando los agregados que ya existen.
- **Pruebas unitarias con xUnit** sobre `AmortizacionService` y `ProgramacionesService`.

---

## 5. Por qué escogí estas funcionalidades innovadoras

Las tres responden a la misma pregunta: *¿qué haría que alguien abriera esta app y no solo la lista
de movimientos de su banco?*

**El dashboard**, porque los datos ya existían pero no decían nada: cuentas por un lado, préstamos
por otro, y ningún lugar respondía "¿cuánto tengo realmente?". El patrimonio neto es esa respuesta en
un solo número, y de paso me obligó a mover el cálculo a SQL en vez de sumar en C# y a dibujar el
gráfico yo mismo en lugar de arrastrar una librería externa.

**Las transferencias programadas**, porque atacan el trabajo repetitivo real (alquiler, colegio,
cuota mensual) y convierten la app en algo que trabaja cuando el usuario no la está mirando. Es
también la más compleja: hay que ponerse al día con lo que venció mientras la app estaba cerrada y
decidir qué hacer cuando no hay fondos.

**El simulador de amortización**, porque antes un préstamo era solo un monto y un plazo guardados,
sin intereses ni cuota. Con la matemática financiera real (cuota francesa, desglose capital/interés)
y el pago de cuotas, el préstamo deja de ser un registro y pasa a ser algo que se paga y se termina.
