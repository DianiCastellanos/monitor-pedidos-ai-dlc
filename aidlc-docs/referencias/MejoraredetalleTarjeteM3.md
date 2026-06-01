Se requiere mejorar el detalle mostrado en la tarjeta M3 (APIs Externas).

Actualmente ya se cuenta con información de causa en los checkers (ej: "HTTP 0", "Token inválido", "Timeout"), pero esta no se está mostrando correctamente o de forma consistente en la UI.

Propuesta:

Mostrar el estado de cada API junto con su causa real en una sola línea, por ejemplo:

Salesforce   → ✅ Disponible (OK)
Multivende   → ❌ Sin respuesta (HTTP 0)

o

Salesforce   → ❌ Error de autenticación (Token inválido)
Multivende   → ❌ Sin respuesta (Timeout)

Lineamientos:

1. Mantener el formato:
   [Nombre API] + [Estado] + [Mensaje corto de causa]

2. El mensaje debe provenir directamente del checker (detalle de error)

3. Evitar mensajes técnicos largos (stack traces, errores SQL, etc.)
   → solo mostrar causa clara y legible

4. El estado visual (color e ícono) debe seguir representando:
   - OK → verde
   - Warning → amarillo
   - Critical → rojo

5. No eliminar el detalle por API (esto es crítico para operación)

Objetivo:

Que el usuario pueda entender inmediatamente:
- qué API funciona
- cuál falla
- y POR QUÉ falla
``