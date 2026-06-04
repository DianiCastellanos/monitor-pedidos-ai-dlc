# Business Rules — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## BR-SIM — Reglas del Simulador

| ID | Regla | RF/RT | Componente |
|----|-------|-------|-----------|
| BR-SIM-01 | `OrdersSimulatorService` es **exclusivo del MVP**. Post-MVP se desactiva (`Simulation:Enabled=false`) y los checkers apuntan a la BD real. No debe haber lógica de simulación mezclada con la lógica de dominio de los checkers. | C-06 | `OrdersSimulatorService` |
| BR-SIM-02 | Cuando `NoOrdersMode=true`, el simulador no inserta ningún pedido en el tick. El estado de `simulated_job_statuses` no se modifica por esta opción. | RT1 | `OrdersSimulatorService` |
| BR-SIM-03 | Cuando `FailureProbability=1.0`, todos los pedidos insertados tienen `Status="Error"` e `IsFailure=true`. Cuando `FailureProbability=0.0`, todos son normales (`Status="Pending"`). Valores intermedios producen mezcla proporcional. | RT5 | `OrdersSimulatorService` |
| BR-SIM-04 | `OrdersSimulatorService` inserta pedidos para las 2 sources (Salesforce, Multivende) y los 4 sites (Patprimo, SevenSeven, Atmos, Ostu) en cada tick — total 8 pedidos por tick en modo normal. | — | `OrdersSimulatorService` |
| BR-SIM-05 | La limpieza de `simulated_orders` elimina filas con `created_at < NOW()-48h`. Se ejecuta una vez por día. No afecta `simulated_job_statuses`. | C-06 | `OrdersSimulatorService` |
| BR-SIM-06 | `SimulationOptions` se lee de `appsettings.json` con soporte de variables de entorno (prefijo `Simulation__`). No requiere recompilar para cambiar escenarios. | — | `SimulationOptions` |

---

## BR-RT — Reglas de Red-Teaming

| ID | Regla | Escenario | Verificación esperada |
|----|-------|-----------|----------------------|
| BR-RT-01 | **RT1** (job SF apagado): con `NoOrdersMode=true` O con `simulated_job_statuses.status='Failed'` para `SalesforceDownload`, el sistema emite `CheckResult.Critical` en el siguiente tick (< 5 min). La causa debe clasificarse como `Job`. | RT1 | Incidente en `incidents` con `causa=Job`; alerta en `RealtimePage` en < 5 min |
| BR-RT-02 | **RT2** (token SF revocado): con credencial de Salesforce inválida en User Secrets, `SalesforceApiChecker` recibe HTTP 401, **no reintenta** (Polly ADR-U4-01) y emite `Critical` con `causa=Token` + referencia `SOP-001` en `accion_sugerida`. | RT2 | `retry_metadata` vacío o null; `IncidentDetailPage` muestra SOP-001 |
| BR-RT-03 | **RT3** (SQL apagado): con SQL Server MonitorPedidosDb (172.16.0.41) detenido, `DbHealthChecker` falla al abrir conexión y emite `Critical` inmediato. El tiempo de detección debe ser < 1 min desde que el servicio se detiene. | RT3 | Incidente CRITICAL con `causa=DbHealth` visible en RealtimePage |
| BR-RT-04 | **RT5** (estado incorrecto): pedidos con `IsFailure=true` aparecen en `DiscrepanciesPage` **sin** generar alerta WARN/CRITICAL al operador. El dashboard no muestra estado degradado por esta condición. | RT5 | `DiscrepanciesPage` lista los pedidos; `RealtimePage` permanece en Ok para DbOrders |
| BR-RT-05 | **RT7** (cancelado ignorado): un pedido con `Status="Cancelled"` en `simulated_orders` **no** genera incidente. `DbOrderChecker` cuenta solo pedidos con `Status` válido (Pending/Processing). | RT7 | Sin incidente generado; checker retorna Ok |
| BR-RT-06 | **RT-Persist** (cerrar/reabrir dashboard): los incidentes abiertos persisten en SQL Server. Al reabrir el navegador, `RealtimePage.OnInitializedAsync` carga el estado actual desde `IIncidentService` — el estado CRITICAL sigue visible. | RT-Persist | Estado correcto al reabrir sin reload del servidor |

---

## BR-CLEAN — Reglas de aislamiento del simulador

| ID | Regla | RF |
|----|-------|-----|
| BR-CLEAN-01 | Los checkers (U3) no conocen si los datos vienen del simulador o de la BD real. Leen de `simulated_orders` y `simulated_job_statuses` igual que leerían de las tablas reales — sin flag ni modo especial en el checker. | C-06 |
| BR-CLEAN-02 | La configuración `Simulation:Enabled=false` detiene `OrdersSimulatorService` pero no afecta a ningún otro servicio. Los checkers siguen leyendo de las tablas (que quedarán vacías o con los datos existentes). | C-06 |
| BR-CLEAN-03 | `simulated_job_statuses` se actualiza **manualmente** (script SQL) para los escenarios RT1/RT2, no por `OrdersSimulatorService`. El simulador solo escribe en `simulated_orders`. | RT1, RT2 |

---

## Resumen de reglas por categoría

| Categoría | Cantidad |
|-----------|---------|
| BR-SIM    | 6       |
| BR-RT     | 6       |
| BR-CLEAN  | 3       |
| **Total** | **15**  |
