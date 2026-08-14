using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsRollups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyEventStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Target = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyEventStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyReferrerStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    ReferrerHost = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReferrerStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyRouteStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    Visitors = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRouteStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailySiteStats",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    Visitors = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySiteStats", x => x.Day);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyEventStats_Day_Name",
                table: "DailyEventStats",
                columns: new[] { "Day", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyReferrerStats_Day_ReferrerHost",
                table: "DailyReferrerStats",
                columns: new[] { "Day", "ReferrerHost" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyRouteStats_Day_Path",
                table: "DailyRouteStats",
                columns: new[] { "Day", "Path" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyEventStats");

            migrationBuilder.DropTable(
                name: "DailyReferrerStats");

            migrationBuilder.DropTable(
                name: "DailyRouteStats");

            migrationBuilder.DropTable(
                name: "DailySiteStats");
        }
    }
}
