# Plan IT6 — UI Navigation & Modo NOC

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado

---

## Objetivo

Documentar cambios de UI que quedaron sin registrar de sesiones anteriores, agregar enlace a Modo NOC en la navegación principal, y mejorar la experiencia del layout NOC para uso en TV como panel de monitoreo.

---

## 1. Cambios de UI no documentados (sesiones iniciales)

### 1.1 Rutas renombradas respecto al diseño original

| Diseñado | Implementado | Dónde se usó |
|----------|-------------|--------------|
| `/history` | `/incidents` | HistoricPage.razor |
| `/summary` | `/incidents/weekly` | WeeklySummaryPage.razor |
| `/rules/{id}/edit` | `/rules/edit/{RuleId:guid}` | RuleEditPage.razor |
| `/rules/{id}/history` | `/rules/history/{RuleId:guid}` | RuleHistoryPage.razor |

### 1.2 MainLayout vs NavMenu

El template original de Blazor incluye `NavMenu.razor` (sidebar) que **no se usa como layout activo**. La navegación real está en `MainLayout.razor` (barra horizontal). Como resultado:

| Página | Ruta | En MainLayout | En NavMenu |
|--------|------|:------------:|:----------:|
| Dashboard | `/dashboard` | ✅ | ✅ |
| Historial | `/incidents` | ✅ | ✅ |
| Resumen Semanal | `/incidents/weekly` | ✅ | ✅ |
| Reglas | `/rules` | ✅ (solo Técnico) | ✅ (solo Técnico) |
| Logs | `/logs` | ✅ (solo Técnico) | ✅ (solo Técnico) |
| Brand Monitor | `/brand-monitor` | ❌ | ❌ |
| Discrepancias | `/discrepancies` | ❌ | ✅ |
| **Modo NOC** | **`/noc`** | **❌** | **✅** |

**Este plan resuelve el caso de Modo NOC.** Brand Monitor y Discrepancias quedan para futura iteración si el usuario lo solicita.

### 1.3 Layout real vs diseño

| Aspecto | Diseñado (U1 docs) | Real (MainLayout) |
|---------|-------------------|-------------------|
| Estilo | Sidebar 240px + Header | Barra horizontal Bootstrap |
| Tema | Dark-first | Claro (Bootstrap default) |
| Identity | 2 botones sin contraseña | ✅ Implementado en `/Identity/Select` |

---

## 2. Modo NOC — Mejora para TV

### 2.1 Navegación

Agregar enlace "Modo NOC" en `MainLayout.razor` accesible para todos los roles autenticados, después de "Resumen Semanal", con estilo distintivo (outline/badge) para indicar que abre una vista diferente.

### 2.2 NocLayout

Layout mínimo, sin navegación, con header compacto y enlace de salida. Se estiliza con fondo oscuro (`bg-dark text-light`) para mejor visibilidad en pantallas de TV.

### 2.3 NocPage

- Cards de dominio en grid 4-columnas con estado visual claro
- Tabla de incidentes activos con severity badge
- Refresco en tiempo real vía SignalR (`AlertBroadcaster`)
- **Adicional:** Timer periódico de respaldo (10s) para refrescar aunque no hayan eventos SignalR

---

## 3. Archivos a modificar

| Archivo | Acción |
|---------|--------|
| `src/MonitorPedidos.Web/Components/Layout/MainLayout.razor` | Agregar enlace "Modo NOC" |
| `src/MonitorPedidos.Web/Components/Layout/NocLayout.razor` | Mejorar estilo para TV (fondo oscuro, tipografía grande) |
| `src/MonitorPedidos.Web/Components/Pages/Noc/NocPage.razor` | Agregar timer periódico de respaldo (30s) |
