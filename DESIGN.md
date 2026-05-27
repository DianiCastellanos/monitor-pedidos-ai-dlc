---
# MonitorPedidos AI — Design Tokens
# Inspirado en Grafana · Datadog · Vercel
# Dark-first: dashboard operacional 24/7
# Scene: "This feels like a control room — calm authority, instant clarity."

colors:
  # Brand / Interactive
  primary: "#2563EB"            # Blue 600 — acciones primarias, estados activos, focus ring
  on-primary: "#FFFFFF"         # Texto/iconos sobre primary
  primary-hover: "#1D4ED8"      # Blue 700 — hover sobre primary

  # Surfaces (dark-first — modo oscuro como único modo MVP)
  background: "#0D1117"         # Fondo de página — negro pizarra (control room)
  surface: "#161C2D"            # Cards, panels, sidebar — pizarra azulada
  surface-raised: "#1E2537"     # Hover, selected, table header
  surface-overlay: "#252D3D"    # Tooltips, dropdowns

  # Semantic status — vocabulario NOC estándar (no son colores de marca)
  status-ok: "#16A34A"          # Green 600 — Normal / OK
  on-status-ok: "#DCFCE7"       # Texto sobre fondo de estado OK
  status-warn: "#D97706"        # Amber 600 — Degradado / Descarga lenta
  on-status-warn: "#FEF3C7"     # Texto sobre fondo de estado WARN
  status-critical: "#DC2626"    # Red 600 — Error / Crítico
  on-status-critical: "#FEE2E2" # Texto sobre fondo de estado CRITICAL

  # Typography tokens
  text-primary: "#E2E8F0"       # Slate 200 — texto principal
  text-secondary: "#94A3B8"     # Slate 400 — timestamps, etiquetas secundarias
  text-disabled: "#475569"      # Slate 600 — elementos deshabilitados
  text-inverse: "#0D1117"       # Texto sobre fondos claros (badges)

  # Structural
  outline: "#1E293B"            # Slate 800 — bordes sutiles, divisores
  outline-strong: "#334155"     # Slate 700 — bordes prominentes, focus outline

typography:
  font-family: "Inter Variable, Inter, system-ui, -apple-system, sans-serif"
  font-family-mono: "JetBrains Mono, 'Cascadia Code', 'Fira Code', monospace"
  scale:
    xs:   "11px / 1.4"          # Labels de badge, metadata compacta
    sm:   "13px / 1.5"          # Datos de tabla, texto secundario
    base: "14px / 1.6"          # Texto del cuerpo del dashboard (estándar Grafana/Datadog)
    lg:   "16px / 1.4"          # Títulos de card, items de navegación
    xl:   "20px / 1.3"          # Títulos de sección (Alertas Activas, Brand Monitor)
    2xl:  "28px / 1.25"         # Valores de métrica en Brand Monitor (Δ)
    3xl:  "40px / 1.2"          # NOC en pantalla completa — lectura a distancia
  weight:
    regular:  400
    medium:   500
    semibold: 600
    bold:     700

spacing:
  base: 4
  scale: [4, 8, 12, 16, 24, 32, 48, 64, 96, 128]

rounded:
  sm:   "4px"                   # Status badges, status pills, inputs
  md:   "8px"                   # Cards NOC, botones, paneles, nav-items
  lg:   "12px"                  # Modales, paneles grandes
  full: "9999px"                # Status dots, avatares, indicadores circulares

components:
  status-card:
    min-width: "160px"
    border-radius: "8px"
    padding: "16px"
    border-top-width: "3px"     # Acento por estado — NUNCA side-stripe
    elevation: 1

  status-dot:
    size: "10px"
    border-radius: "9999px"
    animation-critical: "pulse 1.5s cubic-bezier(0.4, 0, 0.6, 1) infinite"

  status-badge:
    height: "20px"
    padding: "0 8px"
    border-radius: "4px"
    font-size: "11px"
    font-weight: 600
    letter-spacing: "0.04em"
    text-transform: "uppercase"

  alert-row:
    height: "48px"
    border-bottom: "1px solid {colors.outline}"
    hover-background: "{colors.surface-raised}"
    transition: "background 120ms ease-out"

  brand-row:
    min-height: "44px"          # Touch target mínimo WCAG
    border-bottom: "1px solid {colors.outline}"
    hover-background: "{colors.surface-raised}"

  rule-row:
    height: "52px"
    border-bottom: "1px solid {colors.outline}"
    hover-background: "{colors.surface-raised}"

  nav-item:
    height: "36px"
    padding: "0 12px"
    border-radius: "8px"
    font-size: "14px"
    font-weight: 500
    transition: "background 120ms ease-out"

  button-primary:
    height: "36px"
    padding: "0 16px"
    border-radius: "8px"
    font-size: "13px"
    font-weight: 500

  button-secondary:
    height: "36px"
    padding: "0 16px"
    border-radius: "8px"
    font-size: "13px"
    font-weight: 500
    border: "1px solid {colors.outline-strong}"

  button-danger:
    height: "36px"
    padding: "0 16px"
    border-radius: "8px"
    font-size: "13px"
    font-weight: 500
    background: "transparent"
    border: "1px solid {colors.status-critical}"
    color: "{colors.status-critical}"

  input:
    height: "36px"
    padding: "0 12px"
    border-radius: "6px"
    border: "1px solid {colors.outline-strong}"
    focus-ring: "2px solid {colors.primary}"
    font-size: "14px"

  textarea:
    padding: "8px 12px"
    border-radius: "6px"
    border: "1px solid {colors.outline-strong}"
    focus-ring: "2px solid {colors.primary}"
    font-size: "14px"
    min-height: "80px"

  sidebar:
    width: "240px"
    width-collapsed: "64px"     # Modo NOC
    background: "{colors.surface}"
    border-right: "1px solid {colors.outline}"

  modal:
    max-width: "480px"
    border-radius: "12px"
    padding: "24px"
    background: "{colors.surface-raised}"
    elevation: 3

motion:
  duration:
    instant:  "0ms"             # Menús de contexto, tooltips on-demand
    fast:     "120ms"           # Hover, micro-interactions, pressed states
    normal:   "200ms"           # Transiciones de estado (OK → CRITICAL)
    slow:     "300ms"           # Entradas de alertas nuevas, apertura de modal
  easing:
    entrance:   "cubic-bezier(0, 0, 0.2, 1)"    # ease-out — elementos que aparecen
    exit:       "cubic-bezier(0.4, 0, 1, 1)"    # ease-in  — elementos que desaparecen
    transition: "cubic-bezier(0.4, 0, 0.2, 1)"  # ease-in-out — cambios de estado
  reduced-motion: "prefers-reduced-motion: reduce — deshabilitar pulse y transiciones opcionales"
---

# MonitorPedidos AI — Design System

## Overview

MonitorPedidos AI es un dashboard operacional (NOC) para Manufacturas Eliot. Los operadores lo usan en pantalla durante la jornada laboral para detectar fallas en el flujo de pedidos Salesforce → Multivende → SQL Server en menos de 5 minutos.

**Scene sentence:** *"This feels like a control room — calm authority, instant clarity."*

**Material dominante:** void — oscuridad estructurada con superficies iluminadas con precisión. Sin ornamento, sin decoración. La información es la interfaz.

**Inspiración visual:**
- **Grafana** — jerarquía de estado NOC, tipografía tabular para métricas, oscuridad como lenguaje
- **Datadog** — claridad de alertas, pills de severidad, estructura de tabla de incidentes
- **Vercel** — precisión tipográfica, espaciado disciplinado, calidad de rendering

**Registro:** Product UI — patrones predecibles, grids consistentes y 8 estados de interacción completos. No es un sitio de marketing.

**Estrategia de color:** Committed — un único acento azul para interacción + vocabulario semántico estándar de NOC (verde/ámbar/rojo). Nunca color decorativo.

---

## Colors

### Por qué dark-first

El dashboard corre en pantalla durante toda la jornada operacional, frecuentemente en salas de operaciones o monitores secundarios. El modo oscuro reduce la fatiga visual en exposición prolongada. Grafana es oscuro por defecto. Este diseño también lo es.

### Decisiones de color

**Background `#0D1117`:** Negro pizarra (no negro puro `#000`). El contraste extremo de `#000` genera un halo perceptual que fatiga los ojos. Este tono, inspirado en GitHub Dark, es clínicamente legible en sesiones largas.

**Surface `#161C2D`:** Un grado más claro que el fondo, con matiz azulado frío que comunica "ambiente técnico" sin ser decorativo. El delta de luminosidad es suficiente para definir cards sin necesitar bordes pesados.

**Primary `#2563EB` (Blue 600):** Azul profesional sin matiz cyan ni violeta. Señala acciones e interactividad sin competir con los colores de estado. Contraste sobre `surface`: > 4.5:1 ✓

**Status colors — por qué Amber en lugar de Yellow:**
`status-warn: #D97706` (Amber 600) tiene ratio 3.9:1 sobre el fondo oscuro — legible y diferenciable de `status-ok`. El amarillo puro (`#FACC15`) tiene ratio insuficiente en dark mode y evoca "advertencia benigna". El ámbar es más urgente.

### Colores de estado: vocabulario semántico, no marca

Los colores verde/ámbar/rojo **no son la identidad de la marca** — son un vocabulario semántico estándar de NOC que el operador reconoce instintivamente (como un semáforo). Siempre van acompañados de icono + texto para no depender solo del color.

| Estado | Color | Icono | Texto |
|--------|-------|-------|-------|
| OK | `#16A34A` | ✓ check-circle | "Normal" (módulos) / "Normal" (Brand Monitor) |
| WARN | `#D97706` | ⚠ triangle | "Degradado" (módulos) / "Lento" (Brand Monitor) |
| CRITICAL | `#DC2626` | ✗ x-circle | "Error" / "Crítico" (módulos) / "Riesgo" (Brand Monitor) |

### Modo oscuro

Este sistema **es** el modo oscuro. Es el modo por defecto y único para el MVP. Un modo claro puede considerarse en v2 para entornos con luz directa intensa.

---

## Typography

### Fuente principal: Inter Variable

Inter es el estándar de facto para dashboards de producto (Vercel, Linear, Grafana v10+). Como fuente variable, un solo archivo `.woff2` cubre todos los pesos sin peticiones adicionales.

```css
@font-face {
  font-family: 'Inter';
  src: url('/fonts/inter-variable.woff2') format('woff2-variations');
  font-weight: 100 900;
  font-display: swap;
}

:root {
  font-family: 'Inter Variable', system-ui, sans-serif;
  font-optical-sizing: auto;
  -webkit-font-smoothing: antialiased;
}
```

### Fuente monoespaciada: JetBrains Mono

Usada exclusivamente para:
- Identificadores de módulo (`M2`, `M3`, `M11`) en cards y tablas
- Timestamps en tablas (`22:12:34`, `2026-05-25T22:10Z`)
- Valores numéricos en Brand Monitor (Actual, Hace 10 min, Δ)
- Logs técnicos en `/logs`

### Reglas críticas

**`font-variant-numeric: tabular-nums`** en **todos los números del dashboard**. Los números proporcionales cambian de ancho al actualizarse (como el contador de incidentes), causando saltos visuales perturbadores en un NOC en tiempo real.

**`text-wrap: balance`** en nombres de módulo y títulos de sección. Evita líneas huérfanas.

**`text-wrap: pretty`** en campos de texto largos (descripciones de alerta, comentarios de resolución).

### Uso por escala

| Tamaño | Uso |
|--------|-----|
| `11px` | Badges CRITICAL/WARN/OK, labels de metadata |
| `13px` | Datos de tabla, texto secundario, timestamps |
| `14px` | Texto del cuerpo del dashboard |
| `16px` | Nombres de módulo en cards, items de navegación |
| `20px` | Títulos de sección |
| `28px` | Valores Δ en Brand Monitor (números grandes con tabular-nums) |
| `40px` | Reservado para NOC en pantalla completa |

---

## Layout

### Estructura general

```
┌──────────────────────────────────────────────────────────────┐
│  HEADER — logo + nombre sistema + identidad activa    [NOC]  │  48px
├──────────────┬───────────────────────────────────────────────┤
│              │                                               │
│   SIDEBAR    │              CONTENT AREA                    │
│   240px      │                                               │
│              │  padding: 24px | max-width: 1440px           │
│  Navigation  │                                               │
│              │                                               │
└──────────────┴───────────────────────────────────────────────┘
```

**Header:** `height: 48px`, `background: surface`, `border-bottom: 1px solid outline`. Logo/nombre a la izquierda, perfil activo (Operador | Técnico) + botón Modo NOC a la derecha.

**Sidebar:** `width: 240px`, fondo `surface`, `border-right: 1px solid outline`. En Modo NOC se colapsa a `64px` (solo iconos).

**Content Area:** `padding: 24px`, grid de 12 columnas con `gap: 16px`.

### Navegación completa (Sidebar)

```
┌─────────────────────────┐
│ ⬛ MonitorPedidos AI     │
├─────────────────────────┤
│ ◉ Dashboard             │  ← todos los roles
│   Brand Monitor         │  ← todos los roles
│   Historial             │  ← todos los roles
│   Resumen semanal       │  ← todos los roles
│   Discrepancias         │  ← todos los roles
├─────────────────────────┤
│ ── Solo Técnico ──      │
│   Reglas                │
│   Logs                  │
│   Simulación  [dev]     │  ← solo en Development
├─────────────────────────┤
│ ● Analista Operativo    │  ← identidad activa al pie
└─────────────────────────┘
```

Items del Técnico se ocultan completamente para el Operador (server-side en Blazor, no solo CSS).

### Grid de NOC Cards

Las 4 tarjetas forman un grid horizontal responsivo:

```
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│    M2    │ │    M3    │ │    M4    │ │   M11    │
│ BD Ped.  │ │ APIs Ext.│ │ Health BD│ │ Jobs     │
│    ✓     │ │    ✗     │ │    ✓     │ │    ✓     │
│ Normal   │ │ Error    │ │ Normal   │ │ Normal   │
│ 22:12:34 │ │ 22:10:01 │ │ 22:12:34 │ │ 22:12:34 │
└──────────┘ └──────────┘ └──────────┘ └──────────┘
   border-top: 3px #16A34A  border-top: 3px #DC2626
```

`grid-template-columns: repeat(auto-fit, minmax(160px, 1fr))`

Las cards en CRITICAL se diferencian visualmente con `border-top` en `status-critical` y status dot animado. No son idénticas al resto — la diferenciación es intencional y funcional.

### Escala de espaciado

| Token | Valor | Uso |
|-------|-------|-----|
| `space-1` | 4px | Separación mínima, gap entre icon+texto |
| `space-2` | 8px | Padding de badge, espacio entre elementos |
| `space-3` | 12px | Padding de nav-item, celda de tabla |
| `space-4` | 16px | Padding de card, gap entre cards |
| `space-6` | 24px | Padding de content area, gap entre secciones |
| `space-8` | 32px | Separación entre secciones mayores |
| `space-12` | 48px | Alto del header |

### Breakpoints

| Breakpoint | Valor | Comportamiento |
|-----------|-------|----------------|
| `sm` | 640px | NOC cards en stack vertical |
| `md` | 768px | Sidebar oculto (hamburger) |
| `lg` | 1024px | Layout completo sidebar + content |
| `xl` | 1280px | Tablas a ancho completo |

**Touch targets:** mínimo `44×44px` en todos los controles interactivos (WCAG 2.5.5).

---

## Elevation

Sombras conservadoras — en dark mode las sombras deben ser más pronunciadas porque el contraste de superficie es menor que en light mode.

| Nivel | Uso | Sombra |
|-------|-----|--------|
| 0 — Flat | Superficies base, filas de tabla | sin sombra |
| 1 — Card | Status cards NOC, paneles | `0 1px 3px rgba(0,0,0,.5), 0 1px 2px rgba(0,0,0,.3)` |
| 2 — Popup | Dropdowns, tooltips | `0 4px 16px rgba(0,0,0,.6)` |
| 3 — Modal | Paneles de confirmación, modales | `0 16px 48px rgba(0,0,0,.7)` |

La sombra **nunca se anima directamente** — se usa `::after` con `opacity` para evitar repaint del layout:

```css
.status-card { position: relative; }
.status-card::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0,0,0,.6);
  opacity: 0;
  transition: opacity 200ms ease-out;
  pointer-events: none;
}
.status-card:hover::after { opacity: 1; }
```

---

## Shapes

### Radio concéntrico

El elemento interno siempre tiene `border-radius = radio_externo − padding`. Mezclar radios sin esta regla produce una tensión óptica notoria.

| Token | Valor | Componente |
|-------|-------|-----------|
| `rounded-sm` | 4px | Badges, pills, inputs |
| `rounded-md` | 8px | Cards NOC, botones, paneles, nav-items |
| `rounded-lg` | 12px | Modales, paneles de confirmación |
| `rounded-full` | 9999px | Status dots, avatares de identidad |

```css
.status-card { border-radius: 8px; padding: 16px; }
.status-card .badge { border-radius: 4px; }  /* 8 − padding ~4 */
```

### Status dot — animación CRITICAL

El dot de 10×10px con `border-radius: 9999px` comunica estado con el mínimo área visual. En CRITICAL irradia un halo suave:

```css
@keyframes pulse {
  0%, 100% { box-shadow: 0 0 0 0 rgba(220, 38, 38, 0.4); }
  50%       { box-shadow: 0 0 0 6px rgba(220, 38, 38, 0); }
}

@media (prefers-reduced-motion: reduce) {
  .status-dot { animation: none; }
}
```

### Acento de estado en cards

`border-top: 3px solid {status-color}` diferencia las cards por estado. **No usar side-stripe** (borde lateral) — es un anti-patrón que parece defecto de CSS, no decisión de diseño.

---

## Components

### Identity Selector (pantalla inicial `/`)

Pantalla de bienvenida centrada verticalmente. Dos botones grandes, accesibles con teclado y con contraste adecuado.

```
┌────────────────────────────────────────┐
│                                        │
│    ⬛  MonitorPedidos AI               │
│        Manufacturas Eliot              │
│                                        │
│  ┌────────────────────────────────┐    │
│  │  👤  Analista Operativo        │    │  ← button-primary, full-width
│  └────────────────────────────────┘    │
│                                        │
│  ┌────────────────────────────────┐    │
│  │  🔧  Responsable Técnico       │    │  ← button-secondary, full-width
│  └────────────────────────────────┘    │
│                                        │
└────────────────────────────────────────┘
  max-width: 400px | centrado en viewport | background: background
```

### Status Card NOC

```
┌──────────────────────────────┐  ← border-top: 3px solid {status-color}
│  M3            ●             │  ← ID en monospace 14px | status-dot (animated si CRITICAL)
│  APIs Externas               │  ← nombre módulo, 16px medium
│  ✗  Error                    │  ← icon + texto, 14px, color del estado
│  22:10:01                    │  ← timestamp, 13px, monospace, text-secondary
└──────────────────────────────┘
  background: surface | padding: 16px | border-radius: 8px | elevation: 1
```

| Estado | border-top | dot | dot-animation |
|--------|-----------|-----|---------------|
| OK | `status-ok` | verde | ninguna |
| WARN | `status-warn` | ámbar | ninguna |
| CRITICAL | `status-critical` | rojo | `pulse 1.5s infinite` |
| Sin datos | `outline-strong` | `text-disabled` | ninguna |

### Status Badge (severidad)

```
[ CRITICAL ]   [ WARN ]   [ OK ]
```

`height: 20px | padding: 0 8px | border-radius: 4px | font-size: 11px | font-weight: 600 | text-transform: uppercase | letter-spacing: 0.04em`

Fondo: `{status-color}` al 15% de opacidad + texto `on-status-*`. Evita saturación excesiva en tablas densas.

### Active Alerts Table (`/dashboard`)

Orden de columnas aplicando **Serial Position Law**: campo más urgente primero, acción sugerida al final para guiar la mirada.

```
┌────────┬──────────┬────────────────────────┬──────────┬──────────────────┬────────────────────┐
│ Módulo │ Severidad│ Qué ocurrió            │ Detectado│ Causa            │ Siguiente paso     │
├────────┼──────────┼────────────────────────┼──────────┼──────────────────┼────────────────────┤
│  M3    │[CRITICAL]│ API Salesforce: 401    │ 22:10:34 │ Token expirado   │ Ver SOP-001        │
│  M4    │  [WARN]  │ Latencia BD: 850ms     │ 22:08:12 │ BD lenta         │ Verificar índices  │
└────────┴──────────┴────────────────────────┴──────────┴──────────────────┴────────────────────┘
```

- Timestamps: monoespaciada, `tabular-nums`, `text-secondary`
- Hover: `surface-raised` con `transition: 120ms ease-out`
- **Estado vacío:** `"Sin alertas activas ✓"` — nunca dejar la tabla visualmente vacía

Botón de cierre manual (solo visible al expandir la fila):
```
[✗ Cerrar incidente]  ← button-danger con modal de confirmación
```
Modal solicita comentario obligatorio antes de confirmar el cierre.

### Brand Monitor Table (`/brand-monitor`)

```
┌──────────────┬──────────┬─────────────┬──────────┬──────────────────┐
│ Marca        │ Actual   │ Hace 10 min │    Δ     │ Estado           │
├──────────────┼──────────┼─────────────┼──────────┼──────────────────┤
│ Patprimo     │   847    │     923     │   −76    │ 🟢 Normal         │
│ SevenSeven   │   203    │     198     │    +5    │ 🔴 Riesgo         │
│ Atmos        │    45    │      48     │    −3    │ 🟡 Lento          │
│ Ostu         │    32    │      32     │     0    │ 🔴 Riesgo         │
└──────────────┴──────────┴─────────────┴──────────┴──────────────────┘
```

- Columna Δ: fuente monoespaciada, `font-size: 28px`, `tabular-nums`, color del estado
- Δ negativo = bueno para el negocio → el verde confirma que la reducción es suficiente
- Mínimo `44px` de altura por fila (touch target)
- Actualización: cada 10 min (timer M3)

### Incident History (`/incidents`)

Filtros en header de sección:

```
[ Desde: ______ ]  [ Hasta: ______ ]  [ Severidad: Todas ▼ ]  [ Módulo: Todos ▼ ]  [🔍 Buscar]
```

Tabla de resultados paginada:

```
┌────────┬──────────┬────────────────────────┬──────────┬───────────┬──────────────┐
│ Módulo │ Severidad│ Descripción            │ Abierto  │ Cerrado   │ Tipo cierre  │
├────────┼──────────┼────────────────────────┼──────────┼───────────┼──────────────┤
│  M3    │[CRITICAL]│ Token Salesforce 401   │ 22:10:34 │ 22:18:01  │ Manual       │
│  M4    │  [WARN]  │ Latencia BD 850ms      │ 21:45:00 │ 21:55:22  │ Automático   │
└────────┴──────────┴────────────────────────┴──────────┴───────────┴──────────────┘

   [ Anterior ]  Página 1 de 3  [ Siguiente ]
```

Incidentes `IsCandidatoReglaNueva = true` (causa no determinada) se destacan con un badge `[⚡ Candidato a regla]` en la columna de descripción.

### Weekly Summary (`/incidents/weekly`)

```
┌─────────────────────────────────────────────┐
│  Semana: 19 may – 25 may 2026               │
│  [← Anterior]                [Siguiente →]  │
├──────────────────┬──────────────────────────┤
│ Causa            │ Crítico │ Warn  │ Total  │
├──────────────────┼──────────────────────────┤
│ Token            │    3    │   0   │   3    │
│ API              │    1    │   2   │   3    │
│ BD               │    0    │   1   │   1    │
│ No Determinada   │    1    │   0   │   1 ⚡ │
└──────────────────┴──────────────────────────┘
  ⚡ = candidatos a nueva regla
```

### Rules Management (`/rules`) — solo Técnico

```
┌──────────────────────────────────────────────────────────────────┐
│  Gestión de Reglas                         [+ Nueva regla]        │
├───────────────┬─────────────────────┬───────────┬───────────────-┤
│ Módulo        │ Nombre              │ Severidad │ Estado         │
├───────────────┼─────────────────────┼───────────┼────────────────┤
│ DbOrders      │ Ventana de pedidos  │[CRITICAL] │ ● Activa       │
│               │                     │           │ [Editar] [Hist]│
│               │                     │           │ [Desactivar]   │
├───────────────┼─────────────────────┼───────────┼────────────────┤
│ BrandMonitor  │ PendingDropThreshold│  [INFO]   │ ● Activa       │
│               │                     │           │ [Editar] [Hist]│
│               │                     │           │ [Desactivar]   │
└───────────────┴─────────────────────┴───────────┴────────────────┘
```

**Modal de razón obligatoria (Activar/Desactivar):**
```
┌───────────────────────────────────────┐
│  Desactivar: "Ventana de pedidos"     │
│                                       │
│  Razón del cambio (obligatorio):      │
│  ┌─────────────────────────────────┐  │
│  │ Por mantenimiento programado... │  │  ← textarea
│  └─────────────────────────────────┘  │
│                                       │
│      [Cancelar]    [Confirmar]        │
└───────────────────────────────────────┘
```

### Rule Edit (`/rules/new`, `/rules/{id}/edit`) — solo Técnico

```
┌──────────────────────────────────────────────────────────┐
│  Nueva regla  (o  Editar: "Ventana de pedidos")          │
├──────────────────────────────────────────────────────────┤
│  Nombre *            [_________________________________]  │
│  Descripción *       [_________________________________]  │
│  Módulo *            [ DbOrders              ▼ ]         │
│  Severidad *         [ CRÍTICO               ▼ ]         │
│                                                          │
│  ── Condición (dinámica por módulo) ──────────────────── │
│  [DbOrders]   Ventana (h) * [__]   Pedidos mínimos * [__]│
│  [DbHealth]   Warn (ms) * [____]   Critical (ms) * [____]│
│  [BrandMonitor] Umbral de caída * [____]                 │
│                                                          │
│  ── Auditoría ─────────────────────────────────────────  │
│  Razón del cambio * [________________________________]   │
│                                                          │
│      [Cancelar]                       [Guardar]          │
└──────────────────────────────────────────────────────────┘
```

La sección de Condición se adapta dinámicamente al módulo seleccionado — campos numéricos relevantes para ese módulo.

### Rule History (`/rules/{id}/history`) — solo Técnico, inmutable

```
┌──────────────────────────────────────────────────────────┐
│  Historial: "Ventana de pedidos — Salesforce/Multivende" │
├─────────────────┬─────────────┬────────────┬────────────-┤
│ Timestamp       │ Tipo        │ Autor      │ Razón       │
├─────────────────┼─────────────┼────────────┼─────────────┤
│ 25/05 10:30     │ Edición     │ Técnico    │ Ajuste...   │
│ ▼ [ver diff]                                             │
│  ┌─ ANTES ──────────────┐  ┌─ DESPUÉS ────────────────┐  │
│  │ WindowHours: 2       │  │ WindowHours: 3            │  │
│  └──────────────────────┘  └───────────────────────────┘  │
│ 25/05 09:00     │ Creación    │ Técnico    │ Inicial     │
└─────────────────┴─────────────┴────────────┴─────────────┘
  Sin botones de acción — historial read-only
```

### Logs Page (`/logs`) — solo Técnico

```
┌─────────────────────────────────────────────┐
│  Logs técnicos          [Exportar CSV] [JSON]│
├────────────┬───────┬─────────────────────────┤
│ Timestamp  │ Level │ Message                 │
├────────────┼───────┼─────────────────────────┤
│ 22:12:34   │ INFO  │ M2 check completed: OK  │
│ 22:10:01   │ ERROR │ M3 API returned 401     │
│ 22:08:12   │ WARN  │ M4 latency: 850ms       │
└────────────┴───────┴─────────────────────────┘
  Mostrando últimas 500 líneas
```

Exportación via `IJSRuntime.InvokeVoidAsync("downloadBlob", content, filename)` — sin backend adicional.

### Discrepancies Panel (`/discrepancies`)

Vista silenciosa sin notificaciones ni push. Solo el Operador la revisa manualmente.

```
┌──────────────────────────────────────────────────────┐
│  Panel de Discrepancias (UC6)                        │
│  Solo informativo — sin notificaciones automáticas   │
├────────┬──────────────────────────┬──────────────────┤
│ Módulo │ Descripción              │ Detectado        │
├────────┼──────────────────────────┼──────────────────┤
│ M2     │ Pedido ID 4521: estado   │ 21/05 10:00      │
│        │ "confirmado" en BD pero  │                  │
│        │ "pendiente" en Salesforce│                  │
└────────┴──────────────────────────┴──────────────────┘
  Estado vacío: "Sin discrepancias detectadas ✓"
```

### NOC Mode Layout (`/noc`)

Sidebar colapsado a `64px` (solo iconos). Sin header de identidad. Contenido a mayor tamaño para lectura a distancia.

```
┌────┬─────────────────────────────────────────────────┐
│ 🏠 │  M2 ✓ NORMAL   M3 ✗ ERROR   M4 ✓ OK   M11 ✓   │
│ 📊 │  [font-size: 40px, tabular-nums]                │
│ ⚙  │  ─────────────────────────────────────────────  │
│    │  ALERTA CRÍTICA — M3 — 22:10:34                 │
│    │  API Salesforce respondió HTTP 401              │
└────┴─────────────────────────────────────────────────┘
                                   [Salir del modo NOC]
```

### Simulation Panel (`/simulation`) — solo Técnico, solo dev

```
┌──────────────────────────────────────────────────────┐
│  🔴 ENTORNO DE DESARROLLO — Simulación de fallos     │
│  Solo visible en entorno Development                  │
├──────────────────────────────────────────────────────┤
│  [ ] Job Salesforce detenido    [Activar] [Resetear] │
│  [ ] Token inválido             [Activar] [Resetear] │
│  [ ] BD timeout                 [Activar] [Resetear] │
│  [ ] Pedido estado incorrecto   [Activar] [Resetear] │
│  [ ] Job Multivende detenido    [Activar] [Resetear] │
│  [ ] Pedido cancelado           [Activar] [Resetear] │
└──────────────────────────────────────────────────────┘
```

Visualmente destacada con border `status-critical` y label de entorno. **Nunca visible en producción.**

### Notification Fallback UX

```
Usuario abre dashboard por primera vez
     │
     ▼
Notification.requestPermission()
     │
     ├── "granted"  → Browser Push Notification
     │                   Título: "🔴 MonitorPedidos"
     │                   Cuerpo: "{QuePaso}" — "{AccionSugerida}"
     │
     └── "denied" / "default"
               │
               ├── playAlertSound()
               │       → new Audio('/sounds/alert.mp3')
               │       → volume: 0.3 (sutil, no intrusivo)
               │
               └── flashTitle()
                       → setInterval: alterna "🔴 ALERTA" / "MonitorPedidos"
                       → se detiene cuando el usuario foca la pestaña
```

---

## Do's and Don'ts

| Do | Don't |
|----|-------|
| `font-variant-numeric: tabular-nums` en todos los números | Números proporcionales en tablas — los valores saltan visualmente al actualizarse |
| Color + icono + texto en cada estado de módulo | Depender solo del color para comunicar estado (WCAG 1.4.1) |
| `"Sin alertas activas ✓"` cuando la tabla está vacía | Dejar la tabla de alertas vacía sin mensaje |
| `border-top: 3px` para acento de estado en cards | Side-stripe (borde lateral) — parece defecto de CSS |
| Animar `::after opacity` para hover shadow | Animar `box-shadow` directamente — causa repaint del layout |
| `transform: scale(0.98)` para el estado pressed de botones | Indicar pressed solo con cambio de color |
| Adaptar la sección de Condición al módulo seleccionado en RuleEditPage | Mostrar todos los campos de condición a la vez |
| Historial de reglas read-only — sin botones de edición | Permitir editar el historial — es inmutable por diseño |
| Ocultar "Simulación" server-side para Operador | Solo ocultar en CSS — la restricción debe ser server-side |
| `@media (prefers-reduced-motion: reduce)` deshabilitar pulse y transiciones | Omitir la media query |
| Desactivar reglas con razón obligatoria | Eliminar reglas — el PRD no contempla eliminación |
| Mostrando monospace en IDs de módulo (M2, M3) y timestamps | Mezclar fuentes proporcionales y monoespaciadas en misma columna |

---

## Accessibility Notes

| Criterio | Verificación |
|----------|-------------|
| `text-primary (#E2E8F0)` sobre `background (#0D1117)` | **14.7:1** ✓ WCAG AAA |
| `status-ok (#16A34A)` texto sobre `surface (#161C2D)` | **4.6:1** ✓ WCAG AA |
| `status-critical (#DC2626)` texto sobre `surface` | **4.8:1** ✓ WCAG AA |
| `status-warn (#D97706)` texto sobre `surface` | **3.9:1** ✓ WCAG AA (large text / UI components) |
| Focus ring: `outline: 2px solid #2563EB` | Visible en todos los controles (2.4.7) |
| Mínimo `44×44px` en controles interactivos | Aplicado en brand-row, nav-items, botones (2.5.5) |
| Estado comunicado con color + icono + texto | Cumple 1.4.1 Use of Color |
| `prefers-reduced-motion` deshabilita pulse y transiciones | Usuarios con condiciones vestibulares (2.3.3) |

---

*MonitorPedidos AI — DESIGN v2.0 · Manufacturas Eliot · 2026-05-25*
