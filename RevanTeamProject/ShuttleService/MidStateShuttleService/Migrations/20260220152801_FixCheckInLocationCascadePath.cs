using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MidStateShuttleService.Migrations
{
    /// <inheritdoc />
    public partial class FixCheckInLocationCascadePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckIn_Location_LocationId",
                table: "CheckIn");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn",
                column: "DropOffLocationId",
                principalTable: "Location",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIn_Location_LocationId",
                table: "CheckIn",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckIn_Location_LocationId",
                table: "CheckIn");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIn_Location_DropOffLocationId",
                table: "CheckIn",
                column: "DropOffLocationId",
                principalTable: "Location",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIn_Location_LocationId",
                table: "CheckIn",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.NoAction);
        }
    }
}
