Quiero ajustar el comportamiento del Brand Monitor para simplificar la lógica y hacerla más coherente con el negocio.

Actualmente hay varios timers (UI y backend) que generan confusión, como:

- "hace X segundos"
- refresh frecuente desde el Dashboard
- doble consulta a Salesforce

La propuesta es simplificar el flujo y dejar una sola fuente de verdad:

---

Nuevo comportamiento esperado:

1. El BrandMonitorChecker es el único responsable de consultar Salesforce

- Ejecutar cada 3 minutos (para no sobrecargar las APIs)
- En cada ejecución:
  - Consultar pedidos pendientes en Salesforce
  - Guardar snapshot en base de datos
  - Registrar timestamp real de la consulta (CheckedAt)

---

2. El Dashboard NO debe consultar Salesforce directamente

- Solo debe leer los datos ya existentes (snapshot o LastCheckStore)
- No debe tener timers que disparen lógica de negocio

---

3. Mostrar en UI solo tiempos reales (no del frontend)

Reemplazar mensajes como:

- "hace X segundos"
- timers del refresh visual

Por:

- "Actualizado: 12:00 PM"
- "Próxima actualización: 12:03 PM"

Donde:

- "Actualizado" = cuándo el checker consultó Salesforce
- "Próxima actualización" = basado en el intervalo del checker

---

4. Comparación de datos

- Cada ejecución del checker debe comparar contra el snapshot anterior
- Esto permite calcular tendencia (sube / baja / estable)
- Si la BD no está disponible:
  - seguir mostrando datos actuales
  - pero sin comparación histórica

---

Objetivo:

Eliminar timers innecesarios en la UI y asegurar que:

- Existe una única fuente de verdad (el checker)
- La UI solo visualiza datos (no ejecuta lógica)
- Los timestamps representan cuándo se consultó Salesforce realmente
- El sistema es más claro para el usuario y más eficiente técnicamente