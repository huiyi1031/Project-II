using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PropertyManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageTypeToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The columns already exist in the database manually.
            // Leaving this empty to sync the EF Core Snapshot without executing SQL.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");
        }
    }
}
