using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBrandMonitorRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "Id", "condition_json", "created_at", "Description", "is_active", "AppliesTo", "Name", "Severity", "updated_at" },
                values: new object[]
                {
                    new Guid("b1c2d3e4-f5a6-7890-bcde-f12345678901"),
                    "{\"pendingDropThreshold\":20,\"pollIntervalSeconds\":120,\"snapshotMinIntervalSeconds\":60,\"comparisonWindowSeconds\":600}",
                    new DateTimeOffset(new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                    "Controla cada cuánto se consulta Salesforce (pollIntervalSeconds), cada cuánto se guarda un snapshot para mantener el historial aun sin cambios (snapshotMinIntervalSeconds), la ventana de tiempo para comparar la evolución del backlog (comparisonWindowSeconds), y el umbral de variación de pendientes que determina cuándo generar una alerta (pendingDropThreshold).",
                    true,
                    "BrandMonitor",
                    "Brand Monitor — Pedidos Pendientes por Marca",
                    "Warn",
                    null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "Id",
                keyValue: new Guid("b1c2d3e4-f5a6-7890-bcde-f12345678901"));
        }
    }
}
