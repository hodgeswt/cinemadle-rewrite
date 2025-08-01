using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemadle.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Guesses",
                table: "Guesses");

            migrationBuilder.RenameTable(
                name: "Guesses",
                newName: "UserGuess");

            migrationBuilder.RenameIndex(
                name: "IX_Guesses_UserId_GameId",
                table: "UserGuess",
                newName: "IX_UserGuess_UserId_GameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserGuess",
                table: "UserGuess",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AnonUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonUsers_UserId",
                table: "AnonUsers",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnonUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserGuess",
                table: "UserGuess");

            migrationBuilder.RenameTable(
                name: "UserGuess",
                newName: "Guesses");

            migrationBuilder.RenameIndex(
                name: "IX_UserGuess_UserId_GameId",
                table: "Guesses",
                newName: "IX_Guesses_UserId_GameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Guesses",
                table: "Guesses",
                column: "Id");
        }
    }
}
