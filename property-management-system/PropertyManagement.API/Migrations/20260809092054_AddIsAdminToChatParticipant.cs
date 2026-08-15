using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAdminToChatParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "ChatParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "ChatParticipants");
        }
    }
}
