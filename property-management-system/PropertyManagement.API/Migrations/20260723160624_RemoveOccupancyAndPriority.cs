using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOccupancyAndPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentOccupants",
                table: "PropertyUnits");

            migrationBuilder.DropColumn(
                name: "MaxOccupants",
                table: "PropertyUnits");

            migrationBuilder.DropColumn(
                name: "PriorityLevel",
                table: "MaintenanceRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentOccupants",
                table: "PropertyUnits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxOccupants",
                table: "PropertyUnits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriorityLevel",
                table: "MaintenanceRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
