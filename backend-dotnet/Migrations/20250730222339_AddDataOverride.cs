using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemadle.Migrations
{
    /// <inheritdoc />
    public partial class AddDataOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MovieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataOverrides_MovieId",
                table: "DataOverrides",
                column: "MovieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataOverrides");
        }
    }
}
