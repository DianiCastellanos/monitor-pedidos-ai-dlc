# IT1 — Migración PostgreSQL → SQL Server · Infrastructure Design

**Fecha:** 2026-05-30  
**Iteración:** IT1 — PostgreSQL → SQL Server + Dual DB  
**Estado:** ✅ Completado

---

## 1. Diagrama de despliegue

```
+---------------------------+
|  Equipo del owner         |
|  localhost:5000           |
|                           |
|  MonitorPedidos.Web       |
|  (Blazor Server)          |
|        |                  |
|        |-- EF Core -----> [172.16.0.41]
|        |                   SQL Server
|        |                   MonitorPedidosDb
|        |                   (App DB — R/W)
|        |                          |
|        |                    incidents
|        |                    rules
|        |                    rule_history
|        |                    brand_snapshots
|        |                    simulated_orders
|        |                    simulated_job_statuses
|        |
|        |-- Dapper ------> [<IP_SERVIDOR_BD>]
|                            SQL Server
|                            <NOMBRE_BD_PRODUCCION>
|                            (Prod DB — READ ONLY)
|                                   |
|                             oc_encabezado
+---------------------------+
```

---

## 2. Gestión de credenciales

**Regla:** Las credenciales NUNCA se versionan. Se leen desde `.env` (gitignored) o variables de entorno.

### `.env` (gitignored — plantilla)

```env
ConnectionStrings__DefaultConnection=Server=172.16.0.41;Database=MonitorPedidosDb;User Id=<usuario>;Password=<password>;TrustServerCertificate=True;Encrypt=True
ConnectionStrings__ProductionDb=Server=<IP_SERVIDOR_BD>;Database=<NOMBRE_BD_PRODUCCION>;User Id=<usuario_readonly>;Password=<password>;TrustServerCertificate=True;Encrypt=True
```

### `appsettings.json` (versionado — solo placeholders)

```json
"ConnectionStrings": {
    "DefaultConnection": "",
    "ProductionDb": ""
}
```

### Lectura en arranque (`Program.cs`)

ASP.NET Core fusiona `appsettings.json` + variables de entorno. Las variables de entorno del `.env` se cargan explícitamente con `DotNetEnv` o se setean en el shell antes de correr la app.

---

## 3. Permisos requeridos por base de datos

### App DB — 172.16.0.41 (MonitorPedidosDb)

El usuario necesita:
- `CREATE TABLE`, `ALTER TABLE` (para migraciones EF Core)
- `INSERT`, `UPDATE`, `DELETE`, `SELECT` (operaciones de negocio)

### Prod DB — <IP_SERVIDOR_BD> (<NOMBRE_BD_PRODUCCION>)

El usuario necesita **solo**:
- `SELECT` en tabla `oc_encabezado`

Sin permisos de escritura. Validado por convención de código — `ProductionOrderRepository` solo usa `QueryAsync` de Dapper (nunca `ExecuteAsync`).

---

## 4. Pendientes para producción

| # | Tarea | Responsable |
|---|-------|-------------|
| 1 | Crear usuario en 172.16.0.41 con permisos R/W en MonitorPedidosDb | DBA |
| 2 | Crear usuario en <IP_SERVIDOR_BD> con solo SELECT en `oc_encabezado` | DBA |
| 3 | Actualizar `.env` con credenciales reales | Owner |
| 4 | Ejecutar `dotnet ef database update` contra App DB | Owner |
| 5 | M11 Jobs — integrar con Windows Task Scheduler real | Dev |
| 6 | M3 APIs — configurar URLs y API keys reales Salesforce/Multivende | Dev |
