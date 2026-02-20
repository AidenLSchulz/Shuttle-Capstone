using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MidStateShuttleService.Migrations
{
    /// <inheritdoc />
    public partial class DaysAndRides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Registration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "customDate",
                table: "Registration",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customDropoffLocation",
                table: "Registration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customMessage",
                table: "Registration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customPickupLocation",
                table: "Registration",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "customTime1",
                table: "Registration",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "customTime2",
                table: "Registration",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isCustom",
                table: "Registration",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RequestDay",
                columns: table => new
                {
                    RequestDayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeekDay = table.Column<int>(type: "int", nullable: false),
                    RegistrationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDay", x => x.RequestDayId);
                    table.ForeignKey(
                        name: "FK_RequestDay_Registration_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registration",
                        principalColumn: "RegistrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ride",
                columns: table => new
                {
                    RideId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDayId = table.Column<int>(type: "int", nullable: false),
                    PickUpLocationID = table.Column<int>(type: "int", nullable: false),
                    DropOffLocationID = table.Column<int>(type: "int", nullable: false),
                    DropOffTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ride", x => x.RideId);
                    table.ForeignKey(
                        name: "FK_Ride_RequestDay_RequestDayId",
                        column: x => x.RequestDayId,
                        principalTable: "RequestDay",
                        principalColumn: "RequestDayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registration_UserId",
                table: "Registration",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDay_RegistrationId",
                table: "RequestDay",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Ride_RequestDayId",
                table: "Ride",
                column: "RequestDayId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registration_Users_UserId",
                table: "Registration",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registration_Users_UserId",
                table: "Registration");

            migrationBuilder.DropTable(
                name: "Ride");

            migrationBuilder.DropTable(
                name: "RequestDay");

            migrationBuilder.DropIndex(
                name: "IX_Registration_UserId",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "customDate",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "customDropoffLocation",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "customMessage",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "customPickupLocation",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "customTime1",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "customTime2",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "isCustom",
                table: "Registration");
        }
    }
}
