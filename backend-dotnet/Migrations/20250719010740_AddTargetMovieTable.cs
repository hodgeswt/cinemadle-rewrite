using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemadle.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetMovieTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TargetMovies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    GameId = table.Column<string>(type: "TEXT", nullable: false),
                    SequenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetMovieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Inserted = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetMovies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TargetMovies_GameId",
                table: "TargetMovies",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TargetMovies");
        }
    }
}
