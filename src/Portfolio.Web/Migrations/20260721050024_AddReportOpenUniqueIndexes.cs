using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReportOpenUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_OpenCommentReport",
                table: "Reports",
                columns: new[] { "ReporterId", "CommentId" },
                unique: true,
                filter: "\"Status\" = 0 AND \"TargetType\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_OpenUserReport",
                table: "Reports",
                columns: new[] { "ReporterId", "TargetUserId" },
                unique: true,
                filter: "\"Status\" = 0 AND \"TargetType\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_OpenCommentReport",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_OpenUserReport",
                table: "Reports");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");
        }
    }
}
