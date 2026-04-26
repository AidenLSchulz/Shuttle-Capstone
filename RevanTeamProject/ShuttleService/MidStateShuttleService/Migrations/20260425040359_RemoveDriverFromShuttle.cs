using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MidStateShuttleService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDriverFromShuttle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bus_Driver_DriverId",
                table: "Bus");

            migrationBuilder.DropIndex(
                name: "IX_Bus_DriverId",
                table: "Bus");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Bus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "Bus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bus_DriverId",
                table: "Bus",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bus_Driver_DriverId",
                table: "Bus",
                column: "DriverId",
                principalTable: "Driver",
                principalColumn: "DriverId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
