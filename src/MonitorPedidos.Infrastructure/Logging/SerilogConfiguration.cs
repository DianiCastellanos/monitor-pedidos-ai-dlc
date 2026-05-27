using Microsoft.AspNetCore.Builder;
using Serilog;

namespace MonitorPedidos.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Destructure.With<SensitiveDataDestructuringPolicy>();
        });

        return builder;
    }
}
