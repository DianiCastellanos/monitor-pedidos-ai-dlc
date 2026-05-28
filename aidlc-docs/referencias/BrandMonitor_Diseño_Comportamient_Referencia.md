# Brand Monitor - Diseño y Comportamiento (Fuente de Verdad)

Este documento define el comportamiento esperado del módulo Brand Monitor.

Todas las implementaciones deben seguir estrictamente este documento.

⚠️ Regla crítica:
NO se deben usar datos simulados, valores fijos o valores por defecto para representar el historial.
El sistema debe comportarse SIEMPRE como si trabajara con datos reales, incluso si los datos son simulados.

---

## Problema actual

El Brand Monitor presenta un comportamiento incorrecto debido a lo siguiente:

- Solo calcula el valor actual de pedidos pendientes
- No está guardando histórico real en el tiempo
- El valor "Hace 10 min" es simulado o fijo
- Cada actualización incrementa artificialmente los pendientes
- Se generan aumentos falsos (ej: +30 constante)

### Consecuencias

- Todas las marcas aparecen como "CRÍTICO"
- El sistema genera alertas falsas
- El dashboard pierde confiabilidad

---

## Objetivo

Rediseñar el Brand Monitor para basarse en datos históricos reales:

- Cada medición debe registrarse en el tiempo
- Las comparaciones (ej: hace 10 min) deben ser reales
- El estado debe reflejar cambios reales
- El sistema debe ser confiable

---

## Regla conceptual clave

Si no existe dato histórico:

> No hay histórico → No se puede calcular cambio

Nunca asumir:
- 0 como valor anterior
- valores fijos
- datos simulados como histórico

---

## Requerimientos funcionales

### 1. Persistencia de histórico

El sistema debe:

- Registrar periódicamente (en cada ejecución del job) los pedidos pendientes por marca
- Guardar cada medición como un snapshot en el tiempo
- No sobrescribir datos anteriores
- Guardar incluso si no hay cambios

### Importante

Aunque los datos sean simulados:

✅ Deben guardarse como si fueran mediciones reales  
✅ Deben evolucionar en el tiempo  
❌ No deben reiniciarse ni recalcularse artificialmente  

---

### 2. Cálculo de "Hace 10 min"

Debe:

- Buscar el valor real más cercano en el histórico
- Basarse únicamente en datos almacenados
- No usar valores simulados ni por defecto

---

### 3. Cálculo de cambio

El cambio solo debe calcularse cuando:

- Existe un valor actual
- Existe un valor histórico válido

Si no:

- No se calcula
- No se generan alertas

---

### 4. Manejo de ausencia de datos

Cuando no exista histórico suficiente:

El sistema debe mostrar:

- Hace 10 min → "-"
- Cambio → "-"
- Estado → "Sin datos"

⚠️ Nunca marcar como crítico en este caso

---

### 5. Evaluación de estado

El estado (OK, Warning, Crítico) debe basarse en:

- Diferencias reales entre datos históricos y actuales
- No en valores simulados

---

### 6. Correcciones obligatorias

Se debe eliminar completamente:

- Uso de valores fijos para el pasado
- Inicialización de histórico en 0
- Simulación de crecimiento artificial
- Comparaciones contra datos inexistentes

---

## Problema actual detectado

El sistema actualmente:

- Aumenta los pendientes artificialmente en cada actualización
- No guarda el valor anterior real
- Compara contra valores incorrectos
- Genera incrementos falsos constantes

Esto produce alertas incorrectas y comportamiento engañoso.

---

## Resultado esperado

Después del ajuste:

- Los datos reflejan evolución real en el tiempo
- "Hace 10 min" muestra datos reales del historial
- Se eliminan los falsos positivos
- El estado refleja condiciones reales
- El dashboard se vuelve confiable

---

## Enfoque de implementación (alto nivel)

- Introducir almacenamiento de snapshots históricos
- Modificar el job de monitoreo para guardar cada ejecución
- Consultar el histórico para calcular comparaciones
- Ajustar la UI para manejar ausencia de datos

---

## Regla final (CRÍTICA)

El sistema debe dejar de simular valores históricos.

Toda comparación debe basarse en datos persistidos en el tiempo.

Incluso en modo simulado, el comportamiento debe imitar un sistema real con evolución histórica.
