using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBjjLandingCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BeltCaption",
                table: "SiteContents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BeltDegrees",
                table: "SiteContents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "GamePlan",
                table: "SiteContents",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroEyebrow",
                table: "SiteContents",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Principles",
                table: "SiteContents",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeltCaption",
                table: "SiteContents");

            migrationBuilder.DropColumn(
                name: "BeltDegrees",
                table: "SiteContents");

            migrationBuilder.DropColumn(
                name: "GamePlan",
                table: "SiteContents");

            migrationBuilder.DropColumn(
                name: "HeroEyebrow",
                table: "SiteContents");

            migrationBuilder.DropColumn(
                name: "Principles",
                table: "SiteContents");
        }
    }
}
