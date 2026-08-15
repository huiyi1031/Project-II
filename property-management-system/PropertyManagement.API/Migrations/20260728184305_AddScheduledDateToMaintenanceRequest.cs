using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledDateToMaintenanceRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "MaintenanceRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "MaintenanceRequests");
        }
    }
}
