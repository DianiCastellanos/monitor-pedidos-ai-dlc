using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulatedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "simulated_job_statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulated_job_statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "simulated_orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_failure = table.Column<bool>(type: "boolean", nullable: false),
                    site = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulated_orders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_simulated_job_statuses_job_name",
                table: "simulated_job_statuses",
                column: "job_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_simulated_orders_created_at",
                table: "simulated_orders",
                column: "created_at");

            // Seed: 2 jobs en estado Completed (baseline normal para red-teaming)
            migrationBuilder.InsertData(
                table: "simulated_job_statuses",
                columns: ["Id", "job_name", "last_run_at", "status", "error_message"],
                values: new object[] { 1, "SalesforceDownload",  new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc), "Completed", null });

            migrationBuilder.InsertData(
                table: "simulated_job_statuses",
                columns: ["Id", "job_name", "last_run_at", "status", "error_message"],
                values: new object[] { 2, "MultivendeDownload", new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc), "Completed", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("simulated_job_statuses", "Id", 1);
            migrationBuilder.DeleteData("simulated_job_statuses", "Id", 2);

            migrationBuilder.DropTable(name: "simulated_job_statuses");
            migrationBuilder.DropTable(name: "simulated_orders");
        }
    }
}
