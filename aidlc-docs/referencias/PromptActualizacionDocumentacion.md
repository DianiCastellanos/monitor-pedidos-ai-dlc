Necesito actualizar la documentación completa del proyecto siguiendo el modelo AI-DLC, incorporando todos los cambios, decisiones y aprendizajes recientes.

Objetivo:

Organizar, consolidar y ajustar toda la información de forma clara y estructurada, evitando fragmentación por tareas individuales y reflejando el sistema como un todo coherente basado en sus módulos.

---

Requerimientos:

1. Actualizar todas las fases del AI-DLC:

- Discovery
- Design
- Build
- Validate
- Deploy/Operate

Cada fase debe reflejar:
- decisiones tomadas
- cambios realizados
- comportamiento actual del sistema
- consideraciones técnicas y de negocio

---

2. Organización por módulos (no por tareas individuales)

Reestructurar la información agrupándola por los módulos del sistema:

- M2 — Ingreso de Pedidos
- M3 — APIs Externas
- M4 — Estado de Base de Datos
- M11 — Procesos / Jobs
- Brand Monitor — Pedidos pendientes por descargar

Para cada módulo documentar:

- Propósito (qué mide)
- Fuente de datos (BD, APIs, etc.)
- Comportamiento en condiciones normales
- Comportamiento en modo degradado
- Estados posibles (OK, Warning, Critical, Sin datos)
- Reglas de interpretación (qué significa cada estado)
- Dependencias
- Consideraciones de UX (qué se muestra al usuario)
- Criterios de tiempo, de forma clara
---

3. Incluir decisiones clave de arquitectura

Documentar claramente las siguientes decisiones:

- Separación de responsabilidades:
  - Checker = lógica y consulta
  - UI = visualización

- Eliminación de lógica de negocio en la UI
- Uso de LastCheckStore como fuente de verdad en tiempo real
- Uso de snapshots en BD como histórico (no crítico para visualización)
- Eliminación de llamadas duplicadas a Salesforce
- Uso de polling configurable (pollIntervalSeconds)
- Eliminación del fallback live desde la UI (solo usar datos del checker)

---

4. Manejo de fallo (graceful degradation)

Documentar cómo se comporta el sistema cuando:

- BD no está disponible
- APIs externas fallan
- Jobs no están activos

Incluir:

- Qué se sigue mostrando
- Qué se pierde temporalmente (ej: histórico)
- Qué mensajes se muestran al usuario
- Cómo se mantiene visibilidad del negocio

---

5. Brand Monitor (sección especial)

Documentar:

- Consulta a Salesforce mediante checker
- Frecuencia configurable (pollIntervalSeconds)
- Uso de snapshots para comparación histórica
- Comportamiento sin BD:
  - Mostrar últimos datos válidos
  - Indicar “sin comparación histórica”
- Eliminación de timers innecesarios en UI
- Uso de timestamps reales:
  - cuándo se consultó Salesforce
  - próxima ejecución del checker

---

6. Consistencia UI / NOC / Dashboard

Asegurar que la documentación refleje:

- Estados consistentes entre NOC y Dashboard
- Detalle por integración en M3
- Estado global basado en el peor componente
- Evitar estados engañosos (ej: success cuando hay fallos)

---

7. Resultado esperado

Un documento claro, organizado por módulos y fases del AI-DLC, que:

- Explique el sistema de forma completa
- Sea entendible para técnicos y negocio
- Evite duplicidad o contradicciones
- Refleje el estado actual real del sistema
- Sirva como base para mantenimiento y evolución futura
``