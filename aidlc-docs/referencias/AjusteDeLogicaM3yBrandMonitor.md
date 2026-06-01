Necesito corregir la forma en que el sistema maneja datos de Salesforce (M3) y Brand Monitor.

Problema actual:

Los datos mostrados en el Dashboard y NOC NO coinciden con lo que devuelve Salesforce en tiempo real.

Ejemplo:
Postman muestra 13 pedidos pendientes para Patprimo,
pero la aplicación muestra 4.

Esto indica que la UI está usando datos antiguos o de base de datos en lugar de la respuesta actual del API.

---

Requerimientos (MUY IMPORTANTE):

1. En cada ejecución del checker (cada ~3 minutos):

   - Llamar al API de Salesforce (order_search)
   - Obtener los pedidos pendientes por marca en tiempo real

2. Con esa respuesta:

   a) USAR esos datos directamente para mostrar en:
      - Dashboard
      - NOC
      - Brand Monitor

   b) Guardar esos mismos datos en base de datos (brand_snapshots)
      - solo para histórico
      - no para visualización principal

3. Importante:

   - La UI NUNCA debe mostrar primero datos desde base de datos
   - Siempre debe usar la respuesta más reciente del API
   - La base de datos solo se usa para:
     - histórico
     - comparaciones (ej: hace 10 min)

4. El flujo correcto debe ser:

   API (Salesforce) → UI (mostrar) → BD (guardar)

   NO:
   BD → UI

5. Asegurar que:

   - Los valores mostrados coincidan EXACTAMENTE con Postman
   - No haya discrepancias entre API y UI

6. Brand Monitor:

   - Debe usar los datos recién obtenidos del API
   - NO usar brand_snapshots como fuente principal

7. NOC:

   - Debe usar los mismos datos en tiempo real
   - No duplicar lógica ni usar datos antiguos

---

Objetivo final:

El sistema debe mostrar los datos reales de Salesforce en cada ciclo de ejecución, y al mismo tiempo almacenarlos para histórico, sin que la base de datos afecte la visualización actual.