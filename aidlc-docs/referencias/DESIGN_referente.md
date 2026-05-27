
# MonitorPedidos — DESIGN

## 1. Design Principles

El diseño del dashboard sigue principios de monitoreo operacional:

- claridad sobre complejidad
- jerarquía visual fuerte
- feedback inmediato
- consistencia visual
- minimalismo funcional

Inspirado en herramientas reales:

- Grafana
- Datadog
- Vercel Dashboard

---

## 2. Layout Structure

### Main Layout

El sistema se organiza en:

- **Header**: nombre del sistema + navegación
- **Navigation**: Dashboard / Historial / Reglas
- **Content Area**: vista principal del monitoreo

---

## 3. System Status Cards (NOC View)

El dashboard presenta tarjetas de estado tipo NOC (Network Operations Center) para mostrar el estado de los módulos críticos.

### Módulos actuales:

- **M2 — BD Pedidos**
- **M3 — APIs Externas**
- **M4 — Health BD**
- **M11 — Jobs Sync**

---

### Card Structure

Cada tarjeta contiene:

- identificador del módulo (M2, M3…)
- nombre del módulo
- icono de estado
- timestamp del último chequeo

---

### Visual Encoding

Estados:

- 🔴 círculo rojo → fallo/estado crítico  
- ✅ check verde → funcionamiento normal  

---

### Example
M2
BD Pedidos
🔴
25/05/2026 22:12
---

## 4. Active Alerts Section

El dashboard incluye una tabla de alertas activas.

### Columns:

- Módulo
- Severidad
- Qué ocurrió
- Detectado
- Causa
- Siguiente paso

---

### UX Behavior

- las alertas activas tienen prioridad visual alta  
- cuando no hay alertas, la tabla puede aparecer vacía  
- en mejoras futuras se recomienda mostrar estado "Sin alertas ✅"

---

## 5. Brand Monitor (Business Monitoring)

Se introduce una vista de análisis de negocio basada en tendencias de pedidos.

### Table Structure

| Marca | Actual | Hace 10 min | Δ | Estado |

---

### Status Logic

- 🟢 Normal → disminución suficiente
- 🟡 Descarga lenta → disminución leve
- 🔴 Riesgo → aumento o sin cambio

---

### Purpose

Permitir monitoreo del comportamiento del negocio, no solo infraestructura.

---

## 6. Red-Teaming Control Panel

Sección exclusiva para desarrollo que permite simular fallos.

### Actions:

- Job detenido / activo
- Token inválido / válido
- BD timeout / normal
- Estado incorrecto / normal

---

### Design Considerations

- visualmente destacada pero separada del flujo principal
- claramente etiquetada como entorno de desarrollo
- no visible en producción

---

## 7. Interaction Design

- actualización automática periódica
- botones de acción ("Chequear ahora", "Activar alertas")
- feedback visual inmediato al usuario

---

## 8. Information Hierarchy

Orden de prioridad:

1. estado de módulos (cards M2–M11)
2. alertas activas
3. monitoreo de marcas (Brand Monitor)
4. histórico y logs

---

## 9. Visual Design

### Colors

- Verde (#16A34A) → normal
- Amarillo (#FACC15) → degradación
- Rojo (#DC2626) → error/crítico

---

### UI Characteristics

- uso de tarjetas (cards)
- espaciado consistente
- diseño plano (flat UI) en MVP

---

## 10. Accessibility

- contraste adecuado para lectura
- uso redundante de colores + iconos
- estructura clara y legible

---

## 11. Performance Considerations

- no se carga histórico pesado
- queries optimizadas
- solo datos necesarios por vista

---

## 12. Consistency

El diseño es consistente con:

- incidentes (U2)
- checkers (U3)
- reglas (U5)
- integraciones (U4)

---

## 13. Design Philosophy

El sistema no busca ser decorativo sino:

- informativo
- accionable
- rápido de interpretar

---

## 14. Future Enhancements

- mejoras visuales en tarjetas (bordes por estado)
- animaciones en estado crítico
- vista de tendencias gráficas
- modo oscuro (dark mode)