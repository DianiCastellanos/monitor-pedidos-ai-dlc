using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeRulesConfigurable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "Id", "condition_json", "created_at", "Description", "is_active", "AppliesTo", "Name", "Severity", "updated_at" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "{\"WindowMinutes\":10,\"MinOrders\":1,\"Channels\":[\"SALESFORCE\",\"MULTIVENDE\"]}", new DateTimeOffset(new DateTime(2026, 5, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Alerta si no hay pedidos en la ventana esperada para Salesforce o Multivende", true, "DbOrderChecker", "Ventana de pedidos — Salesforce/Multivende", "Critical", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
        }
    }
}
