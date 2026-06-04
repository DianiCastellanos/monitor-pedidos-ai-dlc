using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Notifications;
using MonitorPedidos.Web.Features.ApiChecks;
using MonitorPedidos.Infrastructure.Incidents;
using MonitorPedidos.Infrastructure.JobMonitoring;
using MonitorPedidos.Infrastructure.Logging;
// NullNotificationService reemplazado por NotificationService en U6
using MonitorPedidos.Infrastructure.Persistence;
using MonitorPedidos.Web.BackgroundServices;
using MonitorPedidos.Web.Components;
using MonitorPedidos.Web.Features.Monitoring;
using MonitorPedidos.Web.Middleware;
using MonitorPedidos.Web.Services;
using MonitorPedidos.Infrastructure.Rules;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Infrastructure.Dashboard;
using MonitorPedidos.Web.Hubs;
using MonitorPedidos.Domain.Simulation;
using MonitorPedidos.Infrastructure.Simulation;

// Load .env for local development (overrides appsettings.json values via env vars)
if (File.Exists(".env"))
{
    foreach (var line in File.ReadAllLines(".env"))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
        var idx = trimmed.IndexOf('=');
        if (idx > 0)
            Environment.SetEnvironmentVariable(trimmed[..idx].Trim(), trimmed[(idx + 1)..].Trim());
    }
}

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.AddSerilogLogging();

// EF Core — SQL Server (Docker)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Data Protection — persiste claves entre reinicios
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("MonitorPedidos");

// Cookie authentication — sin ASP.NET Core Identity
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath         = "/Identity/Select";
        options.AccessDeniedPath  = "/Identity/AccessDenied";
        options.ExpireTimeSpan    = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly   = true;
        options.Cookie.SameSite   = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Authorization — deny-by-default (RF-26, BR-AUTHZ-01)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Razor Pages (Identity: Select, Logout, AccessDenied)
builder.Services.AddRazorPages();

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── U6: SignalR + Real-Time ───────────────────────────────────────────────
builder.Services.AddSignalR(opts =>
{
    if (builder.Environment.IsDevelopment()) opts.EnableDetailedErrors = true;
});
builder.Services.AddSingleton<AlertBroadcaster>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<LastCheckStore>();
builder.Services.AddScoped<IBrandSnapshotRepository, BrandSnapshotRepository>();
builder.Services.AddScoped<BrandMonitorChecker>();
builder.Services.AddScoped<ICheckExecutor>(sp => sp.GetRequiredService<BrandMonitorChecker>());
builder.Services.AddScoped<IBrandMonitorService, BrandMonitorService>();
builder.Services.AddScoped<ITechnicalLogReader, TechnicalLogReader>();
// ─────────────────────────────────────────────────────────────────────────

// ── U2: Persistence & Incidents ──────────────────────────────────────────
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddHostedService<IncidentMaintenanceService>();
builder.Services.AddHostedService<OrderSyncService>();     // Sync oc_encabezado vtainternet_qa → MonitorPedidosDb
// ─────────────────────────────────────────────────────────────────────────

// ── U3: Detection & Classification ───────────────────────────────────────
// ADR-U3-02: IEnumerable<ICheckExecutor> resuelve los 3 automáticamente
builder.Services.AddScoped<ICheckExecutor, DbOrderChecker>();
builder.Services.AddScoped<ICheckExecutor, DbHealthChecker>();
builder.Services.AddScoped<ICheckExecutor, JobsChecker>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddHostedService<MonitoringSchedulerService>();
// ─────────────────────────────────────────────────────────────────────────

// ── U7: Simulation & Red-Teaming ─────────────────────────────────────────
builder.Services.Configure<SimulationOptions>(
    builder.Configuration.GetSection(SimulationOptions.Section));

// M2 — IOrderSource: production (read-only) or simulated fallback
var prodConn = builder.Configuration.GetConnectionString("ProductionDb");
if (!string.IsNullOrEmpty(prodConn))
    builder.Services.AddScoped<IOrderSource>(_ => new ProductionOrderRepository(prodConn));
else
    builder.Services.AddScoped<IOrderSource, SimulatedOrderRepository>();

builder.Services.AddScoped<ISimulatedOrderRepository,     SimulatedOrderRepository>();
// JobsMonitor — real desde Task Scheduler, con fallback a simulación
builder.Services.Configure<JobsMonitorOptions>(
    builder.Configuration.GetSection(JobsMonitorOptions.Section));

if (!string.IsNullOrEmpty(builder.Configuration.GetSection(JobsMonitorOptions.Section)["Server"]))
    builder.Services.AddScoped<IJobStatusSource, SchtasksJobStatusSource>();
else
    builder.Services.AddScoped<IJobStatusSource, SimulatedJobStatusRepository>();

builder.Services.AddScoped<ISimulatedJobStatusRepository, SimulatedJobStatusRepository>();
builder.Services.AddHostedService<OrdersSimulatorService>();
// ─────────────────────────────────────────────────────────────────────────

// ── U5: Rules Management ──────────────────────────────────────────────────
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IRuleHistoryRepository, RuleHistoryRepository>();
builder.Services.AddScoped<IRuleManagementService, RuleManagementService>();
// ─────────────────────────────────────────────────────────────────────────

// ── U4: External Integrations ─────────────────────────────────────────────
builder.Services.AddScoped<ICheckExecutor, SalesforceApiChecker>();
builder.Services.AddScoped<ICheckExecutor, MultivendeApiChecker>();

// Salesforce OAuth2 — singleton para caché de token + handler para inyección automática
builder.Services.AddSingleton<SalesforceTokenCache>();
builder.Services.AddTransient<SalesforceAuthHandler>();

// HttpClient dedicado para el endpoint de token OAuth2 (host raíz, sin site path)
builder.Services.AddHttpClient("SalesforceAuth", c => { c.Timeout = TimeSpan.FromSeconds(10); });

// ADR-U4-02: Typed HTTP clients con política Polly compartida por instancia de request
builder.Services
    .AddHttpClient<ISalesforceClient, SalesforceClient>(c =>
    {
        // Sin BaseAddress — SalesforceClient construye URLs absolutas por site (Salesforce:Sites[])
        c.Timeout = TimeSpan.FromSeconds(15);
        // Bearer token inyectado por SalesforceAuthHandler
    })
    .AddHttpMessageHandler<SalesforceAuthHandler>()
    .AddPolicyHandler((services, _) =>
        ApiRetryPolicy.Create(services.GetRequiredService<ILoggerFactory>().CreateLogger("ApiRetryPolicy")));

builder.Services
    .AddHttpClient<IMultivendeClient, MultivendeClient>(c =>
    {
        var baseUrl = builder.Configuration["Multivende:BaseUrl"];
        if (!string.IsNullOrEmpty(baseUrl))
            c.BaseAddress = new Uri(baseUrl);
        c.Timeout = TimeSpan.FromSeconds(10);
        var apiKey = builder.Configuration["Multivende:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
            c.DefaultRequestHeaders.Authorization = new("Bearer", apiKey);
    })
    .AddPolicyHandler((services, _) =>
        ApiRetryPolicy.Create(services.GetRequiredService<ILoggerFactory>().CreateLogger("ApiRetryPolicy")));
// ─────────────────────────────────────────────────────────────────────────

// Exception handler (NFR §3)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Pipeline de seguridad (orden crítico — nfr-design-patterns.md §1)
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseSecurityHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<AlertsHub>("/hubs/alerts");

app.Run();

// Requerido para WebApplicationFactory en tests de integración
public partial class Program { }
