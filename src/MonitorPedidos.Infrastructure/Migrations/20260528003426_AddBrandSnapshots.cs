using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brand_snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pending_count_current = table.Column<int>(type: "integer", nullable: false),
                    pending_count_previous = table.Column<int>(type: "integer", nullable: false),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brand_snapshots_Site",
                table: "brand_snapshots",
                column: "site",
                unique: true);

            // Seed: regla BrandMonitor — umbral de caída de pedidos pendientes
            migrationBuilder.InsertData(
                table: "rules",
                columns: ["Id", "Name", "Description", "AppliesTo", "condition_json", "Severity", "is_active", "created_at", "updated_at"],
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000003"),
                    "Caída de pedidos por marca",
                    "Alerta cuando el número de pedidos pendientes cae abruptamente en alguna tienda.",
                    "BrandMonitor",
                    "{\"windowHours\":null,\"minOrders\":null,\"latencyWarnMs\":null,\"latencyCriticalMs\":null,\"pendingDropThreshold\":5}",
                    "Warn",
                    true,
                    new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    (DateTimeOffset?)null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rules",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DropTable(
                name: "brand_snapshots");
        }
    }
}
