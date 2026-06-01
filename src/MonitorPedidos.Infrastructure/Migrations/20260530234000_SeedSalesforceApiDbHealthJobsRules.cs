using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSalesforceApiDbHealthJobsRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "Id", "condition_json", "created_at", "Description", "is_active", "AppliesTo", "Name", "Severity", "updated_at" },
                values: new object[,]
                {
                    {
                        new Guid("a2b3c4d5-e6f7-8901-abcd-ef12345678901"),
                        "{\"pendingDropThreshold\":50}",
                        new DateTimeOffset(new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                        "Define el máximo de pedidos pendientes en Salesforce antes de considerar el servicio como Crítico. Si la cantidad supera el umbral, se dispara una alerta.",
                        true,
                        "SalesforceApi",
                        "APIs Externas — Umbral de pedidos pendientes en Salesforce",
                        "Critical",
                        null
                    },
                    {
                        new Guid("a3b4c5d6-e7f8-9012-abcd-ef12345678902"),
                        "{\"latencyWarnMs\":1000,\"latencyCriticalMs\":5000}",
                        new DateTimeOffset(new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                        "Controla los tiempos de respuesta de la base de datos. Si la latencia supera latencyWarnMs se marca como Advertencia; si supera latencyCriticalMs se marca como Crítico.",
                        true,
                        "DbHealthChecker",
                        "BD Salud — Latencia de conexión",
                        "Critical",
                        null
                    },
                    {
                        new Guid("a4b5c6d7-e8f9-0123-abcd-ef12345678903"),
                        "{}",
                        new DateTimeOffset(new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                        "Regla base para el monitoreo de trabajos programados vía Windows Task Scheduler.",
                        true,
                        "JobsMonitor",
                        "Jobs — Estado del Task Scheduler",
                        "Warn",
                        null
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "Id",
                keyValue: new Guid("a2b3c4d5-e6f7-8901-abcd-ef12345678901"));

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "Id",
                keyValue: new Guid("a3b4c5d6-e7f8-9012-abcd-ef12345678902"));

            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "Id",
                keyValue: new Guid("a4b5c6d7-e8f9-0123-abcd-ef12345678903"));
        }
    }
}
