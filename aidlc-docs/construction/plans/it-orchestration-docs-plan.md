# Plan: Documentación de Orquestación (Estación 6-7)

## Objetivo
Documentar todo el trabajo realizado en MonitorPedidos AI como paquetes de tareas orquestables, lista para publicar en Linear. Crear ficha del harness (OpenCode) y mapa de orquestación.

## Alcance
Documentación de orquestación en `aidlc-docs/orchestration/`.

## Pasos

### Paso 1: Crear task-package.yaml
- [x] 25 tareas con id, title, milestone, priority, estimate, status, blockedBy
- [x] 8 milestones (MS1-MS8)
- [x] Formato YAML estructurado

### Paso 2: Crear milestones.md
- [x] MS1 a MS8 con descripción y fecha estimada

### Paso 3: Crear archivos de tarea individuales (tasks/001-025)
- [x] 25 archivos .md con template: id, title, milestone, summary, scope, deliverables, acceptance criteria
- [x] Categorías: Inception, MVP Core, SQL Server, Brand+Dashboard, Salesforce, Stability, Validate+RT, Operations

### Paso 4: Crear harness-ficha.md
- [x] Identity: OpenCode CLI, modelo big-pickle
- [x] Capabilities table
- [x] Workflow description
- [x] Accuracy & reliability notes

### Paso 5: Crear orchestration-map.md
- [x] Diagrama Mermaid de dependencias entre módulos
- [x] Milestone progress table
- [x] File map

### Paso 6: Actualizar aidlc-state.md
- [x] Agregar IT6 faltante
- [x] Actualizar Current Status (24 tests, 5 reglas, 11 ITs)
- [x] Agregar sección ORCHESTRATION

### Paso 7: Crear build-and-test-summary.md
- [x] Build status
- [x] Unit/integration tests
- [x] Playwright E2E (14 specs, 24 tests)
- [x] Red-teaming summary

### Paso 8: (Opcional) Publicar en Linear
- [ ] Usar Linear MCP para crear Issues desde task-package.yaml

### Paso 9: (Opcional) Llenar operations/
- [ ] Runbook de deploy y operaciones

## Artefactos generados
| Archivo | Descripción |
|---------|-------------|
| `aidlc-docs/orchestration/task-package.yaml` | 25 tareas, 8 milestones |
| `aidlc-docs/orchestration/milestones.md` | Detalle de milestones |
| `aidlc-docs/orchestration/tasks/001-025.md` | Tareas individuales |
| `aidlc-docs/orchestration/harness-ficha.md` | Ficha del harness |
| `aidlc-docs/orchestration/orchestration-map.md` | Mapa de dependencias |
| `aidlc-docs/build-and-test-summary.md` | Resumen de builds y tests |
| `aidlc-state.md` (actualizado) | Estado actual del proyecto |

## Criterios de aceptación
- [ ] 25 task files creados con template consistente
- [ ] task-package.yaml válido y referenciable desde Linear MCP
- [ ] harness-ficha.md documenta OpenCode como herramienta
- [ ] orchestration-map.md tiene diagrama Mermaid válido
- [ ] aidlc-state.md refleja el estado real (24 tests, 5 reglas, 11 ITs)
