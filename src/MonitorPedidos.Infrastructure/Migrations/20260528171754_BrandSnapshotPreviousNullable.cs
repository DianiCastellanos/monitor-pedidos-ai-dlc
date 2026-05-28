using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BrandSnapshotPreviousNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "pending_count_previous",
                table: "brand_snapshots",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "pending_count_previous",
                table: "brand_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
