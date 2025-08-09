using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemadle.Migrations
{
    /// <inheritdoc />
    public partial class UserClues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserClues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    GameId = table.Column<string>(type: "TEXT", nullable: false),
                    ClueType = table.Column<int>(type: "nvarchar(24)", nullable: false),
                    Inserted = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserClues_UserId_GameId",
                table: "UserClues",
                columns: new[] { "UserId", "GameId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserClues");
        }
    }
}
