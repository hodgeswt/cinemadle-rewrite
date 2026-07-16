using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemadle.Migrations
{
    /// <inheritdoc />
    public partial class OneTimeJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OneTimeJobs",
                columns: table => new
                {
                    JobName = table.Column<string>(type: "TEXT", nullable: false),
                    Completed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeJobs", x => x.JobName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimeJobs");
        }
    }
}
