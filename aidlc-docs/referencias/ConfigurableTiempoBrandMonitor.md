Se requiere hacer configurable el intervalo de consulta a Salesforce en el Brand Monitor, sin necesidad de cambiar código.

Actualmente el intervalo está definido (ej: 3 minutos), pero el objetivo es que pueda modificarse dinámicamente según necesidad del negocio.

Propuesta:
1. Este valor debe ser la única fuente de verdad para:
   - Frecuencia del BrandMonitorChecker (scheduler)
   - Cálculo de "Próxima actualización" en la UI
   - Sincronización del refresco del Dashboard

2. El sistema debe permitir cambiar este valor sin redeploy:
   - Idealmente desde base de datos (regla Brand Monitor) o configuración dinámica

4. Cuando el valor cambie:
   - El scheduler debe adaptarse automáticamente
   - La UI debe reflejar el nuevo intervalo
   - No debe requerir reiniciar la aplicación

5. Ejemplo:

   pollIntervalSeconds = 180

   → El checker consulta Salesforce cada 3 minutos
   → UI muestra:
       Actualizado: 12:00 PM
       Próxima actualización: 12:03 PM

Objetivo:

Permitir ajustar fácilmente la frecuencia de consulta a Salesforce sin tocar código, manteniendo coherencia entre backend y UI.`