using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MonitorPedidos.IntegrationTests;

public class MonitorPedidosWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Pasar connection string desde env var (igual que en desarrollo)
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=MonitorPedidosDb;Username=postgres;Password=postgres";

        builder.UseSetting("ConnectionStrings:DefaultConnection", connStr);

        builder.ConfigureServices(services =>
        {
            // Deshabilitar BackgroundServices para que no interfieran con los tests
            RemoveHostedService<MonitorPedidos.Web.BackgroundServices.MonitoringSchedulerService>(services);
            RemoveHostedService<MonitorPedidos.Web.BackgroundServices.IncidentMaintenanceService>(services);
            RemoveHostedService<MonitorPedidos.Web.BackgroundServices.OrdersSimulatorService>(services);
        });
    }

    private static void RemoveHostedService<T>(IServiceCollection services) where T : class, IHostedService
    {
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(T));
        if (descriptor is not null) services.Remove(descriptor);
    }
}
