Se identifican regresiones en el Dashboard después de los últimos cambios.

1. Brand Monitor — pérdida de datos al refrescar

Actualmente, cuando el timer llega a 0:
- La tabla desaparece
- Se muestra "Cargando datos de marcas..."
- Y en algunos casos no vuelve a renderizar correctamente

Esto es incorrecto.

Comportamiento esperado:
- NO limpiar los datos actuales al iniciar refresh
- Mantener la tabla visible siempre
- Mostrar un indicador de actualización (sin borrar contenido)
- Reemplazar los datos solo cuando la nueva información esté disponible

El Brand Monitor NUNCA debe desaparecer visualmente.

---

2. Brand Monitor — fallback en modo degradado

Se definió correctamente que:

- Si Salesforce responde → mostrar pedidos pendientes
- Aunque la BD esté caída

Actualmente esto no está funcionando y se queda en estado de carga.

Revisar que el fallback a GetLiveCountsAsync():
- Se esté ejecutando cuando falla BD
- No quede bloqueado en "loading"
- Y sí alimente el render correctamente

---

3. M3 (APIs Externas) — estado global incorrecto

Se agregó fallback:

    "secondary"

Esto está generando inconsistencias.

Regla correcta:

- Si alguna API está en Critical → M3 = "danger"
- Si ninguna está en Critical pero alguna en Warning → M3 = "warning"
- Solo si todas están OK → "success"
- Si no hay datos en ninguna → "secondary"

IMPORTANTE:
NUNCA debe mostrarse "secondary" si existe al menos una API con estado conocido.

Ejemplo:
Salesforce ✅ + Multivende ❌ → M3 debe ser 🔴 (NO gris)

---

4. M3 (APIs Externas) — detalle por integración

Actualmente se perdió o no se está mostrando correctamente el detalle por API.

Esto es crítico y ya estaba previamente definido.

El comportamiento esperado es que la tarjeta M3 muestre SIEMPRE el detalle por integración, por ejemplo:

Salesforce → ✅ Disponible  
Multivende → ❌ Sin respuesta  

El indicador global (color o punto) debe coexistir con el detalle, pero no reemplazarlo.

NO volver a una vista agregada sin detalle.

---

5. Timer / Refresh — comportamiento destructivo

El refresh actual está limpiando el estado antes de recibir nuevos datos.

Esto genera:
- parpadeo
- pérdida visual de información
- mala experiencia

Solución requerida:

- Convertir refresh en "non-destructive update"
- Mantener estado actual en memoria
- Actualizar solo cuando la nueva data esté lista

---

Objetivo final:

El dashboard debe ser estable, no parpadear, no perder información,
mostrar siempre el estado real del sistema y reflejar claramente:

- El peor estado en el indicador global
- El detalle por integración en M3
- Los datos de negocio (Brand Monitor) incluso en modo degradado
``