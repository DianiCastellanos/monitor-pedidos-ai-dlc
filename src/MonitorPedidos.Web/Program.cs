using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Notifications;
using MonitorPedidos.Infrastructure.Incidents;
using MonitorPedidos.Infrastructure.Logging;
using MonitorPedidos.Infrastructure.Notifications;
using MonitorPedidos.Infrastructure.Persistence;
using MonitorPedidos.Web.BackgroundServices;
using MonitorPedidos.Web.Components;
using MonitorPedidos.Web.Features.Monitoring;
using MonitorPedidos.Web.Middleware;
using MonitorPedidos.Web.Services;

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

// EF Core — PostgreSQL (Docker)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// ── U2: Persistence & Incidents ──────────────────────────────────────────
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddSingleton<INotificationService, NullNotificationService>();
builder.Services.AddHostedService<IncidentMaintenanceService>();
// ─────────────────────────────────────────────────────────────────────────

// ── U3: Detection & Classification ───────────────────────────────────────
// ADR-U3-02: IEnumerable<ICheckExecutor> resuelve los 3 automáticamente
builder.Services.AddScoped<ICheckExecutor, DbOrderChecker>();
builder.Services.AddScoped<ICheckExecutor, DbHealthChecker>();
builder.Services.AddScoped<ICheckExecutor, JobsChecker>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddHostedService<MonitoringSchedulerService>();
// ─────────────────────────────────────────────────────────────────────────

// ── U7 stubs (temporales — serán reemplazados por implementaciones reales) ─
builder.Services.AddScoped<IOrderSource,     SimulatedOrderRepository>();
builder.Services.AddScoped<IJobStatusSource, SimulatedJobStatusRepository>();
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

app.Run();
