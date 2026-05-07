using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MidStateShuttleService.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableToRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Enabled",
                table: "Routes",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Routes");
        }
    }
}
