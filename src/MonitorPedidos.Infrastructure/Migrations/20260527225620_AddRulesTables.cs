using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRulesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AppliesTo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    condition_json = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rule_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    author_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    snapshot_before = table.Column<string>(type: "text", nullable: true),
                    snapshot_after = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rule_history_rules_rule_id",
                        column: x => x.rule_id,
                        principalTable: "rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rule_history_ChangedAt",
                table: "rule_history",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "IX_rule_history_RuleId",
                table: "rule_history",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_rules_Module_Active",
                table: "rules",
                columns: new[] { "AppliesTo", "is_active" });

            // BR-SEED-01, BR-SEED-02: reglas iniciales que replican valores U3
            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "Id", "Name", "Description", "AppliesTo", "condition_json", "Severity", "is_active", "created_at", "updated_at" },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000001"),
                    "Ventana de pedidos",
                    "Se esperan al menos 1 pedido en las últimas 2 horas en la base de datos.",
                    "DbOrderChecker",
                    "{\"windowHours\":2,\"minOrders\":1,\"latencyWarnMs\":null,\"latencyCriticalMs\":null,\"pendingDropThreshold\":null}",
                    "Critical",
                    true,
                    new DateTimeOffset(2026, 5, 27, 0, 0, 0, TimeSpan.Zero),
                    (DateTimeOffset?)null
                });

            migrationBuilder.InsertData(
                table: "rules",
                columns: new[] { "Id", "Name", "Description", "AppliesTo", "condition_json", "Severity", "is_active", "created_at", "updated_at" },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000002"),
                    "Latencia de base de datos",
                    "WARN si latencia > 500ms. CRITICAL si latencia > 2000ms.",
                    "DbHealthChecker",
                    "{\"windowHours\":null,\"minOrders\":null,\"latencyWarnMs\":500,\"latencyCriticalMs\":2000,\"pendingDropThreshold\":null}",
                    "Warn",
                    true,
                    new DateTimeOffset(2026, 5, 27, 0, 0, 0, TimeSpan.Zero),
                    (DateTimeOffset?)null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rule_history");

            migrationBuilder.DropTable(
                name: "rules");
        }
    }
}
