using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class MovePropertyManagerToPropertyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add new column PropertyId to PropertyManagers
            migrationBuilder.AddColumn<long>(
                name: "PropertyId",
                table: "PropertyManagers",
                type: "bigint",
                nullable: true);

            // 2. Data Migration: Copy existing assignments from Properties.ManagedByManagerId
            migrationBuilder.Sql(@"
                UPDATE ""PropertyManagers"" 
                SET ""PropertyId"" = ""Properties"".""Id"" 
                FROM ""Properties"" 
                WHERE ""Properties"".""ManagedByManagerId"" = ""PropertyManagers"".""Id"";
            ");

            // 3. Drop old FK and column from Properties
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_PropertyManagers_ManagedByManagerId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_ManagedByManagerId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ManagedByManagerId",
                table: "Properties");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyManagers_PropertyId",
                table: "PropertyManagers",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyManagers_Properties_PropertyId",
                table: "PropertyManagers",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyManagers_Properties_PropertyId",
                table: "PropertyManagers");

            migrationBuilder.DropIndex(
                name: "IX_PropertyManagers_PropertyId",
                table: "PropertyManagers");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "PropertyManagers");

            migrationBuilder.AddColumn<long>(
                name: "ManagedByManagerId",
                table: "Properties",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ManagedByManagerId",
                table: "Properties",
                column: "ManagedByManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_PropertyManagers_ManagedByManagerId",
                table: "Properties",
                column: "ManagedByManagerId",
                principalTable: "PropertyManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
