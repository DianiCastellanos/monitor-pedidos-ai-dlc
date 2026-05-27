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
                name: "incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Cause = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Alert_QuePaso = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Alert_Cuando = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Alert_Donde = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Alert_SeveridadTexto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Alert_CausaProbable = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Alert_AccionSugerida = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloseType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ClosedByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ComentarioResolucion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsCandidatoReglaNueva = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incidents_Module_ClosedAt",
                table: "incidents",
                columns: new[] { "Module", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_incidents_Module_Open",
                table: "incidents",
                column: "Module",
                unique: true,
                filter: "\"ClosedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_OpenedAt",
                table: "incidents",
                column: "OpenedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incidents");
        }
    }
}
