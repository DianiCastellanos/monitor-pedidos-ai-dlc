using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Infrastructure.Logging;
using MonitorPedidos.Infrastructure.Persistence;
using MonitorPedidos.Web.Components;
using MonitorPedidos.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.AddSerilogLogging();

// EF Core — SQL Server LocalDB
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
