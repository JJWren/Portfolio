using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessageFlagging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "ContactMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "ContactMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_IsFlagged_IsRead",
                table: "ContactMessages",
                columns: new[] { "IsFlagged", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_IsFlagged_IsRead",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "ContactMessages");
        }
    }
}
