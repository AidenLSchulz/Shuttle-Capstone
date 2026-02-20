using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MidStateShuttleService.Migrations
{
    /// <inheritdoc />
    public partial class RemovedPersonalInfoFromRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Registration");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Registration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Registration",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Registration",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Registration",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Registration",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
