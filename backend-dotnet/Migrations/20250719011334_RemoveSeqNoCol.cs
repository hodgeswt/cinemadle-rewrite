using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemadle.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeqNoCol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequenceId",
                table: "TargetMovies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SequenceId",
                table: "TargetMovies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
