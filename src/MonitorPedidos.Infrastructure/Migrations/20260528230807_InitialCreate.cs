using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brand_snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    site = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    pending_count_current = table.Column<int>(type: "int", nullable: false),
                    pending_count_previous = table.Column<int>(type: "int", nullable: true),
                    checked_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brand_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cause = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Alert_QuePaso = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Alert_Cuando = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Alert_Donde = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Alert_SeveridadTexto = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Alert_CausaProbable = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Alert_AccionSugerida = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CloseType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ClosedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ComentarioResolucion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsCandidatoReglaNueva = table.Column<bool>(type: "bit", nullable: false),
                    retry_metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AppliesTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    condition_json = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "simulated_job_statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    job_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_run_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulated_job_statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "simulated_orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_failure = table.Column<bool>(type: "bit", nullable: false),
                    site = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulated_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rule_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rule_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    change_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    author_user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    snapshot_before = table.Column<string>(type: "text", nullable: true),
                    snapshot_after = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                name: "IX_brand_snapshots_Site_CheckedAt",
                table: "brand_snapshots",
                columns: new[] { "site", "checked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_incidents_Module_ClosedAt",
                table: "incidents",
                columns: new[] { "Module", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_incidents_Module_Open",
                table: "incidents",
                column: "Module",
                unique: true,
                filter: "[ClosedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_OpenedAt",
                table: "incidents",
                column: "OpenedAt");

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

            migrationBuilder.CreateIndex(
                name: "UX_simulated_job_statuses_job_name",
                table: "simulated_job_statuses",
                column: "job_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_simulated_orders_created_at",
                table: "simulated_orders",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brand_snapshots");

            migrationBuilder.DropTable(
                name: "incidents");

            migrationBuilder.DropTable(
                name: "rule_history");

            migrationBuilder.DropTable(
                name: "simulated_job_statuses");

            migrationBuilder.DropTable(
                name: "simulated_orders");

            migrationBuilder.DropTable(
                name: "rules");
        }
    }
}
