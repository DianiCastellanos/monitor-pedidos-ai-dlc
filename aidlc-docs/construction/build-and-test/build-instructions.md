# Build Instructions — MonitorPedidos

**Fecha**: 2026-05-24
**Proyecto**: MonitorPedidos — Manufacturas Eliot
**Solución**: `MonitorPedidos.sln`

> **Estado**: Act5 (Code Generation) pendiente de activación — este documento describe los pasos para cuando el código esté generado. No ejecutar hasta que el código exista en el repositorio.

---

## §1 Prerrequisitos

Antes de clonar o construir el proyecto, verificar que el entorno tenga instalado:

| Componente | Versión mínima | Verificación |
|---|---|---|
| .NET SDK | 8.0.x | `dotnet --version` |
| SQL Server MonitorPedidosDb (172.16.0.41) | 2019 o superior | `sqllocaldb info` |
| Visual Studio 2022 o VS Code | Última versión estable | — |
| Git | 2.x | `git --version` |
| Entity Framework CLI | 8.x | `dotnet ef --version` |

**Instalar EF CLI si no está disponible**:
```bash
dotnet tool install --global dotnet-ef
```

**Verificar MonitorPedidosDb**:
```bash
sqllocaldb info
sqllocaldb start MSSQLMonitorPedidosDb
```

**Credenciales requeridas (obtenidas del equipo de integración)**:
- Token de acceso a Salesforce (ambiente interno)
- Token de acceso a Multivende
- Credenciales del usuario administrador inicial

> IMPORTANTE: el sistema opera exclusivamente en red interna / localhost. No se requiere acceso a internet ni configuración de túneles externos.

---

## §2 Clonar y Configurar

**Clonar el repositorio**:
```bash
git clone <url-repositorio-interno> MonitorPedidos
cd MonitorPedidos
```

**Restaurar dependencias NuGet**:
```bash
dotnet restore MonitorPedidos.sln
```

**Paquetes NuGet principales esperados en `MonitorPedidos.Web`**:
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Serilog.AspNetCore`
- `Microsoft.AspNetCore.SignalR`
- `Polly`
- `Microsoft.Extensions.Http.Polly`

**Paquetes esperados en `MonitorPedidos.UnitTests`**:
- `xunit`
- `Moq`
- `Microsoft.AspNetCore.Mvc.Testing`
- `coverlet.collector`

---

## §3 Configurar User Secrets

Todos los valores sensibles se gestionan exclusivamente mediante **User Secrets de .NET**. Nunca se hardcodean en `appsettings.json` ni en ningún archivo de configuración versionado.

**Inicializar User Secrets para el proyecto web** (solo una vez):
```bash
dotnet user-secrets init --project MonitorPedidos.Web
```

**Configurar cada secreto individualmente**:

```bash
# Salesforce
dotnet user-secrets set "Salesforce:BaseUrl" "https://salesforce-interno.eliot.local" --project MonitorPedidos.Web
dotnet user-secrets set "Salesforce:Token" "<token-salesforce>" --project MonitorPedidos.Web
dotnet user-secrets set "Salesforce:ClientId" "<client-id>" --project MonitorPedidos.Web

# Multivende
dotnet user-secrets set "Multivende:BaseUrl" "https://multivende-interno.eliot.local" --project MonitorPedidos.Web
dotnet user-secrets set "Multivende:Token" "<token-multivende>" --project MonitorPedidos.Web
dotnet user-secrets set "Multivende:ApiKey" "<api-key>" --project MonitorPedidos.Web

# Base de datos
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLMonitorPedidosDb;Database=MonitorPedidosDb;Trusted_Connection=True;MultipleActiveResultSets=true" --project MonitorPedidos.Web

# Credenciales de administrador inicial
dotnet user-secrets set "SeedAdmin:Email" "admin@eliot.local" --project MonitorPedidos.Web
dotnet user-secrets set "SeedAdmin:Password" "<password-admin>" --project MonitorPedidos.Web

# Configuración de marca (Brand Monitor)
dotnet user-secrets set "BrandMonitor:IntervalMinutes" "10" --project MonitorPedidos.Web
dotnet user-secrets set "BrandMonitor:AlertThresholdMinutes" "20" --project MonitorPedidos.Web
```

**Verificar que los secretos quedaron registrados**:
```bash
dotnet user-secrets list --project MonitorPedidos.Web
```

> Los User Secrets se almacenan en `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json` en Windows. Este archivo NO es versionado en Git.

---

## §4 Aplicar Migrations

Las migrations deben aplicarse en el orden correcto. Cada migration corresponde a una unidad de construcción del proyecto.

**Orden obligatorio de aplicación**:

| # | Nombre de Migration | Unidad | Descripción |
|---|---|---|---|
| 1 | `InitialCreate` | U1 Foundation | Schema de Identity (usuarios, roles, claims) |
| 2 | `AddIncidentSchema` | U2 Persistence | Tabla `incidents` con estados y relaciones |
| 3 | `AddRulesSchema` | U5 Rules Management | Tablas `rules` y `rule_conditions` |
| 4 | `AddBrandSnapshots` | U6 Dashboard & Real-Time | Tabla `brand_snapshots` + seed de 4 reglas iniciales |
| 5 | `AddSimulationSchema` | U7 Simulation | Tablas `simulated_orders` y `simulated_job_statuses` |

**Comando para aplicar todas las migrations a la vez**:
```bash
dotnet ef database update --project MonitorPedidos.Web
```

**Aplicar hasta una migration específica** (útil para testing parcial):
```bash
dotnet ef database update InitialCreate --project MonitorPedidos.Web
dotnet ef database update AddIncidentSchema --project MonitorPedidos.Web
dotnet ef database update AddRulesSchema --project MonitorPedidos.Web
dotnet ef database update AddBrandSnapshots --project MonitorPedidos.Web
dotnet ef database update AddSimulationSchema --project MonitorPedidos.Web
```

**Verificar estado de migrations**:
```bash
dotnet ef migrations list --project MonitorPedidos.Web
```

**Verificar que la base de datos se creó en MonitorPedidosDb**:
```bash
sqllocaldb info MSSQLMonitorPedidosDb
```

> La migration `AddBrandSnapshots` incluye datos seed para las 4 reglas de monitoreo inicial (Patprimo, SevenSeven, Atmos, Ostu). Si la seed falla, verificar que las tablas `rules` y `rule_conditions` existen antes de aplicar U6.

---

## §5 Build

**Compilar la solución completa**:
```bash
dotnet build MonitorPedidos.sln
```

**Compilar en modo Release** (para verificación pre-deploy):
```bash
dotnet build MonitorPedidos.sln --configuration Release
```

**Compilar solo el proyecto web**:
```bash
dotnet build MonitorPedidos.Web
```

**Compilar solo los tests**:
```bash
dotnet build MonitorPedidos.UnitTests
```

La compilación debe completar con **0 errores** y **0 warnings** (warnings elevados a errores en configuración de producción).

---

## §6 Run

**Iniciar la aplicación en modo Development**:
```bash
dotnet run --project MonitorPedidos.Web
```

**Iniciar con perfil específico de launchSettings**:
```bash
dotnet run --project MonitorPedidos.Web --launch-profile https
```

**Iniciar en modo Watch** (recarga automática durante desarrollo):
```bash
dotnet watch run --project MonitorPedidos.Web
```

Al iniciar, la aplicación:
1. Aplica migrations pendientes automáticamente (si está configurado en `Program.cs`)
2. Ejecuta seed del usuario administrador inicial
3. Inicia el scheduler de `BrandMonitorChecker` (intervalo de 10 minutos)
4. Inicia el hub de SignalR en `/alertsHub`

---

## §7 HTTPS

**Confiar en el certificado de desarrollo de .NET** (una sola vez por máquina):
```bash
dotnet dev-certs https --trust
```

Si el certificado ya existe pero no es de confianza:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

**Verificar que el certificado está instalado**:
```bash
dotnet dev-certs https --check --trust
```

> Este paso es necesario para evitar advertencias de seguridad en el navegador al acceder por HTTPS en localhost.

---

## §8 Verificación

Una vez que la aplicación esté corriendo, verificar acceso en:

| Entorno | URL | Notas |
|---|---|---|
| HTTPS local | `https://localhost:7xxx` | Puerto exacto definido en `launchSettings.json` |
| HTTP local | `http://localhost:5xxx` | Redirige automáticamente a HTTPS |
| SignalR Hub | `https://localhost:7xxx/alertsHub` | Endpoint de SignalR |

**Credenciales de acceso inicial**:
- Usuario: valor configurado en `SeedAdmin:Email` (User Secrets)
- Contraseña: valor configurado en `SeedAdmin:Password` (User Secrets)

**Verificación funcional básica**:
1. Acceder a `https://localhost:7xxx` — debe redirigir a página de login
2. Iniciar sesión con credenciales de administrador
3. Verificar que el dashboard carga correctamente
4. Verificar que el rol del usuario aparece en la barra de navegación
5. Acceder a `/HistorialIncidentes` — debe mostrar tabla (vacía en primera ejecución)
6. Acceder a `/ReglasMonitoreo` — debe mostrar las 4 reglas seeded

**Roles disponibles**:
- `Administrador`: acceso completo (dashboard, historial, reglas, simulación)
- `Supervisor`: dashboard + historial (sin gestión de reglas)
- `Operador`: solo lectura del dashboard (sin historial ni reglas)

---

## Restricciones de Despliegue

> **IMPORTANTE**: MonitorPedidos es una aplicación de uso exclusivo en red interna / localhost.
>
> - NO se usa ngrok ni ningún servicio de tunneling
> - NO se expone a internet bajo ninguna circunstancia
> - NO se despliega en servicios cloud públicos
> - El acceso desde la red local de la empresa requiere configuración de firewall interno

