using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemadle.Migrations
{
    /// <inheritdoc />
    public partial class CustomGameUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CustomGames_Id",
                table: "CustomGames",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomGames_Id",
                table: "CustomGames");
        }
    }
}
