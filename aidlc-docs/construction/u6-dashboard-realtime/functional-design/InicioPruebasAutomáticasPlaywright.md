Quiero iniciar la fase de VALIDATE del AI-DLC implementando pruebas automáticas con Playwright para el sistema de monitoreo NOC.

Contexto del sistema:

El sistema está compuesto por módulos:
- M2 — Ingreso de Pedidos
- M3 — APIs Externas
- M4 — Estado de Base de Datos
- M11 — Procesos / Jobs
- Brand Monitor — pedidos pendientes desde Salesforce

El comportamiento está definido por reglas (BR-BRAND-XX) y decisiones clave:
- Checker = fuente de verdad
- UI = solo visualización (no lógica de negocio)
- LastCheckStore = estado en tiempo real
- BD = histórico (no crítico para visualizar)
- No fallback live automático desde UI
- Refresco no destructivo (la UI nunca pierde datos)
- Estado global M3 = peor estado de sus APIs
- Siempre mostrar causa del error en APIs

---

Objetivo:

Configurar Playwright y crear los primeros tests que validen el comportamiento esperado del sistema, especialmente las reglas críticas.

---

Requerimientos:

1. Configuración inicial

- Crear configuración básica de Playwright para el proyecto
- Definir estructura de tests (carpeta /tests)
- Configurar baseURL (http://localhost:5000)
- Evitar dependencias innecesarias

---

2. Crear primeros tests clave (mínimo 5)

Tests enfocados en reglas críticas del sistema:

✅ Test 1 — NOC carga correctamente
- Navegar a /noc
- Verificar que la pantalla carga sin errores

✅ Test 2 — M3 muestra detalle por API
- Deben ser visibles:
  Salesforce
  Multivende
- Debe mostrar estado + mensaje (Disponible / Sin respuesta, etc.)

✅ Test 3 — M3 respeta peor estado
- Si alguna API está en error → la card debe reflejar estado crítico

✅ Test 4 — Brand Monitor siempre visible
- La tabla no debe desaparecer durante refresh
- Debe existir siempre el contenedor de datos de marcas

✅ Test 5 — M4 muestra estado crítico sin BD
- Cuando no hay conexión a BD, debe mostrar:
  "No hay conexión a la base de datos"

---

3. Buenas prácticas

- Usar selectores robustos (no dependientes de texto dinámico frágil)
- Preferir data-testid si aplica
- Evitar timeouts largos innecesarios
- Tests deben ser claros y mantenibles

---

4. Objetivo final

Tener una base inicial de pruebas automatizadas que:

- Validen reglas críticas del sistema
- Eviten regresiones (ej: tabla que desaparece, estados incorrectos)
- Representen el comportamiento esperado del NOC

---

Antes de ejecutar, mostrar estructura de archivos y ejemplos de tests generados.