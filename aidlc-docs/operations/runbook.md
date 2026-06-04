# Runbook de Operaciones — MonitorPedidos AI

## 1. Arquitectura

| Componente | Tecnología |
|------------|-----------|
| Frontend/Backend | Blazor Server (.NET 8) |
| Base de datos | SQL Server |
| Logs | Serilog (archivos rotativos diarios) |
| Autenticación | Cookie sin contraseña (selección de rol) |

## 2. Prerrequisitos

- .NET 8 SDK
- SQL Server (MonitorPedidosDb para dev, SQL Server Express/Standard para prod)
- Node.js 18+ (solo para tests Playwright)

## 3. Build & Deploy

### Desarrollo
```bash
# Restaurar
dotnet restore src/MonitorPedidos.Web/MonitorPedidos.Web.csproj

# Build
dotnet build src/MonitorPedidos.Web/MonitorPedidos.Web.csproj

# Run
dotnet run --project src/MonitorPedidos.Web
```

### Publicación
```bash
dotnet publish src/MonitorPedidos.Web/MonitorPedidos.Web.csproj -c Release -o ./publish
```

### Windows Service
```powershell
# Crear servicio
New-Service -Name "MonitorPedidos" `
  -BinaryPathName "C:\ruta\publish\MonitorPedidos.Web.exe --content-root=C:\ruta\publish" `
  -StartupType Automatic `
  -Description "Monitor de pedidos para Manufacturas Eliot"

# Iniciar
Start-Service MonitorPedidos

# Ver logs
Get-Content "C:\ruta\publish\logs\monitor-pedidos-$(Get-Date -Format yyyyMMdd).log" -Tail 50
```

## 4. Configuración

### appsettings.json (valores por defecto)
| Clave | Descripción | Default |
|-------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | BD principal (incidentes, reglas, snapshots) | — |
| `ConnectionStrings:ProductionDb` | BD solo lectura (oc_encabezado) | — |
| `Monitoring:CheckerIntervalMinutes` | Intervalo checkers dominio | 5 |
| `Monitoring:ApiCheckerIntervalMinutes` | Intervalo checkers API | 5 |
| `JobsMonitor:Server` | Servidor de tareas programadas | SR-SDEV02CO |
| `JobsMonitor:TaskName` | Nombre del job | OC_PATPRIMO |
| `BrandMonitor:SnapshotRetentionHours` | Retención snapshots | 48 |
| `Incidents:RetentionDays` | Retención incidentes | 90 |
| `Salesforce:CriticalThreshold` | Umbral crítico pedidos pendientes | 50 |

### .env (sobrescribe appsettings.json, no committear)
```env
ConnectionStrings__DefaultConnection=Server=SERVIDOR;Database=DB;User Id=USUARIO;Password=CLAVE;TrustServerCertificate=True;Encrypt=True
ConnectionStrings__ProductionDb=Server=SERVIDOR;Database=vtainternet_qa;User Id=USUARIO;Password=CLAVE;TrustServerCertificate=True;Encrypt=True
Salesforce__ClientId=xxxxxxxxxx
Salesforce__ClientPassword=xxxxxxxxxx
Salesforce__OAuthTokenUrl=https://account.demandware.com/dwsso/oauth2/access_token
Salesforce__Sites__0__Id=PatPrimo
Salesforce__Sites__0__OcapiHost=xxxxx.dx.commercecloud.salesforce.com
Salesforce__Sites__0__OcapiSite=PatPrimo
Salesforce__Sites__1__Id=SevenSeven
...
Multivende__BaseUrl=https://api.multivende.com
```

## 5. Base de Datos

### Migraciones
```bash
# Aplicar migraciones
dotnet ef database update --project src/MonitorPedidos.Infrastructure --startup-project src/MonitorPedidos.Web

# Nueva migración
dotnet ef migrations add NombreMigracion --project src/MonitorPedidos.Infrastructure --startup-project src/MonitorPedidos.Web
```

### Tablas principales
| Tabla | Propósito |
|-------|-----------|
| `Incidents` | Alertas/incidentes del sistema |
| `Rules` | Reglas configuradas por módulo |
| `RuleConditions` | Condiciones de cada regla |
| `RuleHistory` | Historial de cambios en reglas |
| `BrandSnapshots` | Snapshots Brand Monitor (append-only) |
| `__EFMigrationsHistory` | Control de migraciones aplicadas |

### Reglas configuradas (5)
| Módulo | Regla | Condición |
|--------|-------|-----------|
| BrandMonitor | Warn | pendingDropThreshold:20 |
| DbOrderChecker | Critical | — |
| SalesforceApi | Critical | pendingDropThreshold:50 |
| DbHealthChecker | Critical | latencyWarnMs:1000, latencyCriticalMs:5000 |
| JobsMonitor | Warn | — |

### Limpieza programada
```sql
-- BrandMonitor: limpiar snapshots > 48h (ejecutar diariamente)
DELETE FROM BrandSnapshots WHERE CheckedAt < DATEADD(HOUR, -48, GETUTCDATE());

-- Incidents: limpiar > 90 días
DELETE FROM Incidents WHERE CreatedAt < DATEADD(DAY, -90, GETUTCDATE());
```

## 6. Monitoreo

### Páginas clave
| URL | Descripción |
|-----|-------------|
| `/dashboard` | Dashboard principal (4 módulos + Brand Monitor) |
| `/noc` | Vista NOC TV-friendly (4 cards grandes) |
| `/incidents` | Historial de incidentes |
| `/rules` | Gestión de reglas |
| `/logs` | Logs técnicos (rol Técnico) |
| `/discrepancies` | Panel de discrepancias UC6 |

### Estados de módulos
- **Ok** (verde) → funcionando normal
- **Warn** (amarillo) → advertencia (ej: muchos pedidos pendientes)
- **Critical** (rojo) → fallo (ej: BD caída, token inválido)
- **Unknown** (gris) → checker no ha corrido aún

### Brand Monitor
- **Green** (≤0) → sin pendientes nuevos
- **Yellow** (1–20) → pocos pendientes
- **Red** (>20) → muchos pendientes (configurable por regla)

## 7. Logs

- **Ubicación**: `logs/monitor-pedidos-YYYYMMDD.log`
- **Rotación**: diaria, retención 90 días
- **Formato**: `[timestamp LEVEL] mensaje | rid=RequestId`
- **Exportación**: desde `/logs` (CSV o JSON, máx 500 líneas)

## 8. Respaldo

### Base de datos
```bash
# Exportar
sqlcmd -S SERVIDOR -d MonitorPedidosDb -E -o "backup_$(Get-Date -Format yyyyMMdd).sql"

# O con herramienta DBA
# Backup completo diario + transaction log cada hora
```

### Configuración
- `appsettings.json`: respaldar con código (en repo)
- `.env`: respaldar por separado (NUNCA en repo)

## 9. Troubleshooting

| Síntoma | Causa probable | Solución |
|---------|---------------|----------|
| App no arranca | BD no accesible | Verificar `DefaultConnection` en `.env` |
| M3 Critical | Token Salesforce expirado | Verificar `ClientId`/`ClientPassword` |
| M11 Critical | Job OC_PATPRIMO deshabilitado | Habilitar en Task Scheduler de SR-SDEV02CO |
| Brand Monitor sin datos | Checker no ha corrido | Esperar ≤ 3 min (BrandMonitorPollIntervalMinutes) |
| Logs no cargan | Archivo bloqueado por otro proceso | Verificar permisos `logs/` |
| Playwright falla | App no corriendo en :5000 | `dotnet run` primero |
