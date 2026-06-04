# Infrastructure Design — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Resumen de decisiones de infraestructura

| Aspecto | Decisión | Justificación |
|---------|----------|---------------|
| Compute | Kestrel embebido (`dotnet run`) | Sin IIS, sin Docker — MVP interno |
| Base de datos | SQL Server MonitorPedidosDb (172.16.0.41) | ADR-001; sin servidor separado |
| Migrations | Manual CLI (`dotnet ef database update`) | Control explícito; estándar .NET |
| Data Protection keys | File system (`keys/`) | Sesiones sobreviven reinicios |
| HTTPS | dotnet dev-certs (`https://localhost`) | SDK incluido; un comando por máquina |
| Logging | Serilog File sink (`logs/`) | NFR-U1-03; relativo al ejecutable |
| Red | Sin reverse proxy, sin load balancer | MVP localhost / red interna |
| Cloud | Ninguna | Despliegue exclusivamente local |

---

## §2 Componentes lógicos → Infraestructura

### 2.1 AppDbContext — SQL Server MonitorPedidosDb (172.16.0.41)

| Atributo | Valor |
|----------|-------|
| **Proveedor** | `Microsoft.EntityFrameworkCore.SqlServer` |
| **Instancia** | `(localdb)\mssqllocaldb` (instalada con Visual Studio / SQL Server Express) |
| **Base de datos** | `MonitorPedidosDb` |
| **Migrations** | `dotnet ef database update` — manual, antes del primer arranque |
| **Migration inicial** | `InitialCreate` — crea schema base (sin tablas de negocio; éstas vienen en U2) |

**Registro en Program.cs:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Connection string (`appsettings.json`):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MonitorPedidosDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

---

### 2.2 Data Protection Key Ring — File System

| Atributo | Valor |
|----------|-------|
| **Almacenamiento** | Directorio `keys/` relativo al ejecutable |
| **Propósito** | Firma y descifrado de cookies de sesión |
| **Comportamiento** | Las claves persisten entre reinicios — sesiones se mantienen válidas |
| **Rotación** | ASP.NET Core rota claves automáticamente cada 90 días (por defecto) |

**Registro en Program.cs:**
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("MonitorPedidos");
```

**Nota de seguridad:** El directorio `keys/` debe incluirse en `.gitignore`. Las claves son material criptográfico — no se commitean al repositorio.

---

### 2.3 HTTPS — dotnet dev-certs

| Atributo | Valor |
|----------|-------|
| **Tipo** | Certificado autofirmado de .NET SDK |
| **Comando de instalación** | `dotnet dev-certs https --trust` (una vez por máquina) |
| **URL desarrollo** | `https://localhost:7001` (configurable en `launchSettings.json`) |
| **URL HTTP fallback** | `http://localhost:5001` |
| **HSTS** | Solo activo cuando `request.IsHttps == true` (SecurityHeadersMiddleware) |
| **Cookies Secure** | `Secure=true` cuando HTTPS (CookieAuthenticationOptions) |

**Configuración `launchSettings.json`:**
```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "applicationUrl": "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

### 2.4 Serilog — File Sink

| Atributo | Valor |
|----------|-------|
| **Directorio** | `logs/` relativo al directorio de trabajo (raíz del ejecutable) |
| **Patrón de archivo** | `logs/monitor-pedidos-{Date}.log` |
| **Rotación** | Diaria (`rollingInterval: Day`) |
| **Retención** | 90 días (`retainedFileCountLimit: 90`) |
| **Nivel mínimo** | `Information` (general) / `Warning` para Microsoft.* |

**La configuración completa ya está en `appsettings.json`** (ver `tech-stack-decisions.md §5`).

---

### 2.5 Kestrel — Servidor Web Embebido

| Atributo | Valor |
|----------|-------|
| **Servidor** | Kestrel (integrado en ASP.NET Core 8) |
| **Inicio** | `dotnet run --project src/MonitorPedidos.Web` |
| **Entorno desarrollo** | `ASPNETCORE_ENVIRONMENT=Development` |
| **Entorno demo** | `ASPNETCORE_ENVIRONMENT=Production` (activa errores genéricos, sin Swagger) |
| **Sin reverse proxy** | Kestrel expuesto directamente en red interna |

---

## §3 Estructura de directorios en tiempo de ejecución

```
MonitorPedidos.Web/          (directorio del ejecutable)
+-- keys/                    (Data Protection key ring — en .gitignore)
+-- logs/                    (Serilog file sink — en .gitignore)
|   +-- monitor-pedidos-20260523.log
|   +-- monitor-pedidos-20260524.log
|   +-- ...
+-- appsettings.json
+-- appsettings.Development.json
+-- wwwroot/
```

---

## §4 Archivos de configuración por entorno

### appsettings.json (base — todos los entornos)
Contiene: connection string, Serilog config, cookie options.
Ver contenido completo en `tech-stack-decisions.md §5`.

### appsettings.Development.json
```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### Variables de entorno para demo (Production)
```
ASPNETCORE_ENVIRONMENT=Production
```
No se requieren otras variables de entorno en U1 (sin secretos externos todavía — éstos vienen en U4 para Salesforce/Multivende).

---

## §5 .gitignore — entradas requeridas por U1

```gitignore
# Data Protection keys — material criptográfico
keys/

# Logs de Serilog
logs/

# User Secrets (U4+)
appsettings.*.local.json
```

---

## §6 Trazabilidad

| Decisión de infraestructura | NFR / BR | SECURITY |
|-----------------------------|----------|----------|
| MonitorPedidosDb + connection string | RNF-06, NFR-U1 | SECURITY-01 |
| Migration manual CLI | RNF-06 | SECURITY-01 |
| Data Protection file system | RNF-14, BR-COOKIE-01 | SECURITY-12 |
| HTTPS dev-certs | RNF-07 | SECURITY-01 (parcial) |
| Serilog `logs/` | RNF-08, NFR-U1-03 | SECURITY-03, SECURITY-14 |
| `keys/` en .gitignore | RNF-14 | SECURITY-12 |
| Sin cloud / sin infra externa en U1 | MVP scope | SECURITY-02 N/A, SECURITY-07 N/A |
