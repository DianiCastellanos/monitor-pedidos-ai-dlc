# MonitorPedidos — PRODUCT

## 1. Overview

MonitorPedidos es un sistema de monitoreo operativo diseñado para supervisar el flujo de pedidos y el estado de los componentes críticos del sistema en tiempo real.

El sistema combina monitoreo técnico (infraestructura) y monitoreo de negocio (tendencias de pedidos), permitiendo visibilidad completa del estado operativo desde un único dashboard.

---

## 2. Problem Statement

Los equipos operativos carecen de visibilidad inmediata sobre:

- fallas en la descarga de pedidos
- estado de APIs externas (Salesforce, Multivende)
- problemas en la base de datos
- fallas en jobs de sincronización
- comportamiento anómalo en pedidos por marca

Esto genera retrasos operativos, acumulación de pedidos y dificultades para detectar incidentes a tiempo.

---

## 3. Solution

MonitorPedidos implementa un sistema de monitoreo basado en agentes que:

- observa continuamente el estado de los componentes del sistema
- detecta fallos en infraestructura y procesos
- analiza comportamientos de negocio (tendencias)
- genera alertas en tiempo real
- presenta información visual clara en un dashboard tipo NOC

---

## 4. Core Capabilities

- ✅ Monitoreo de componentes críticos (BD, APIs, Jobs)
- ✅ Visualización tipo NOC del estado del sistema
- ✅ Detección automática de fallos operativos
- ✅ Alertas en tiempo real (SignalR)
- ✅ Análisis de tendencias por marca (Brand Monitor)
- ✅ Configuración dinámica mediante reglas
- ✅ Simulación de escenarios (Red‑Teaming / U7)

---

## 5. Dashboard Operacional (NOC View)

El sistema presenta un dashboard tipo NOC (Network Operations Center) que permite visualizar rápidamente el estado de los módulos principales.

### Módulos monitoreados:

- **M2 — BD Pedidos**
- **M3 — APIs Externas**
- **M4 — Health BD**
- **M11 — Jobs Sync**

---

### Cada módulo muestra:

- identificador (M2, M3…)
- nombre del componente
- estado actual
- timestamp del último chequeo

---

### Estados:

- 🔴 Error → fallo crítico
- ✅ OK → funcionamiento normal

---

### Objetivo

Permitir que el operador identifique problemas en menos de 2 segundos.

---

## 6. Alerts & Incident Monitoring

El sistema incluye una sección de alertas activas que muestra:

- módulo afectado
- severidad
- descripción del problema
- causa identificada
- siguiente acción recomendada

---

### Beneficio

Permite tomar decisiones rápidas ante incidentes operativos.

---

## 7. Key Feature: Brand Monitoring

El sistema incorpora monitoreo de negocio mediante el módulo **BrandMonitor**.

---

### Funcionalidad

- consulta pedidos pendientes por marca
- compara estado actual vs estado anterior
- calcula variación (Δ)
- clasifica comportamiento

---

### Estados:

- 🟢 Normal → caída significativa
- 🟡 Descarga lenta → caída leve
- 🔴 Riesgo → aumento o sin cambio

---

### Objetivo

Identificar problemas operativos antes de que se conviertan en incidentes críticos.

---

## 8. Red‑Teaming / Simulation (U7)

El sistema incluye un módulo de simulación que permite reproducir fallos de forma controlada.

---

### Escenarios simulables:

- job detenido
- token inválido
- timeout de base de datos
- estado incorrecto de pedidos

---

### Propósito

- validar el comportamiento del sistema
- demostrar detección de incidentes
- permitir pruebas sin afectar datos reales

---

## 9. Target Users

- **Técnico**
  - gestiona reglas
  - ejecuta red-teaming
  - analiza incidentes

- **Operador**
  - monitorea estado del sistema
  - revisa alertas en tiempo real

---

## 10. Architecture Summary

El sistema sigue un patrón:

Scheduler → Checkers → Classification → Incidents → Dashboard
---

### Extensión (Brand Monitoring):


Scheduler → BrandMonitorChecker → Trend Analysis → Dashboard

---

## 11. AI Justification

El sistema implementa un agente basado en reglas (rule-based agent) que:

- observa el estado del sistema
- analiza cambios en el tiempo
- toma decisiones determinísticas
- genera alertas

---

Esto permite aplicar principios de inteligencia artificial

---

## 12. Product Philosophy

MonitorPedidos no busca ser complejo, sino:

- claro
- accionable
- rápido de interpretar
- enfocado en operaciones reales

---

## 13. Future Enhancements

- monitoreo predictivo
- dashboards históricos
- integración con sistemas externos de alertas
- optimización visual (UI avanzada)