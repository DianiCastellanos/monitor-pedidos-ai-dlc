using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitorPedidos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BrandSnapshotsAppendOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_brand_snapshots_Site",
                table: "brand_snapshots");

            migrationBuilder.CreateIndex(
                name: "IX_brand_snapshots_Site_CheckedAt",
                table: "brand_snapshots",
                columns: new[] { "site", "checked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_brand_snapshots_Site_CheckedAt",
                table: "brand_snapshots");

            migrationBuilder.CreateIndex(
                name: "IX_brand_snapshots_Site",
                table: "brand_snapshots",
                column: "site",
                unique: true);
        }
    }
}
