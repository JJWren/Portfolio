using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReAddReporterIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports");
        }
    }
}
