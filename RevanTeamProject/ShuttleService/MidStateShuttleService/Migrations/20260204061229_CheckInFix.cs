using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MidStateShuttleService.Migrations
{
    /// <inheritdoc />
    public partial class CheckInFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn");

            migrationBuilder.AlterColumn<int>(
                name: "DropOffLocationId",
                table: "CheckIn",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn",
                column: "DropOffLocationId",
                principalTable: "Location",
                principalColumn: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn");

            migrationBuilder.AlterColumn<int>(
                name: "DropOffLocationId",
                table: "CheckIn",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn",
                column: "DropOffLocationId",
                principalTable: "Location",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
