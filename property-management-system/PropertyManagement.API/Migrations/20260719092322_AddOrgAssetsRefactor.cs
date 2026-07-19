using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PropertyManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgAssetsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyUnits_PropertyId",
                table: "PropertyUnits");

            migrationBuilder.DropIndex(
                name: "IX_PropertyUnits_UnitNumber",
                table: "PropertyUnits");

            migrationBuilder.DropColumn(
                name: "CriticalityLevel",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsHighRisk",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RiskScore",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DowntimeDuration",
                table: "AssetMaintenanceHistories");

            migrationBuilder.DropColumn(
                name: "FailureType",
                table: "AssetMaintenanceHistories");

            migrationBuilder.AddColumn<long>(
                name: "ManagedByManagerId",
                table: "Properties",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrganisationId",
                table: "Properties",
                type: "bigint",
                nullable: true);

            // ParentOccupantId already exists in Supabase from a previous migration — skip.

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextMaintenanceDueDate",
                table: "Assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "Assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiryDate",
                table: "Assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedBy",
                table: "AssetMaintenanceHistories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganisationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RegistrationNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyUnit_PropertyId_UnitNumber",
                table: "PropertyUnits",
                columns: new[] { "PropertyId", "UnitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ManagedByManagerId",
                table: "Properties",
                column: "ManagedByManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_OrganisationId",
                table: "Properties",
                column: "OrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Organisations_OrganisationId",
                table: "Properties",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_PropertyManagers_ManagedByManagerId",
                table: "Properties",
                column: "ManagedByManagerId",
                principalTable: "PropertyManagers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Organisations_OrganisationId",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_PropertyManagers_ManagedByManagerId",
                table: "Properties");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropIndex(
                name: "IX_PropertyUnit_PropertyId_UnitNumber",
                table: "PropertyUnits");

            migrationBuilder.DropIndex(
                name: "IX_Properties_ManagedByManagerId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_OrganisationId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ManagedByManagerId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "OrganisationId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ParentOccupantId",
                table: "Occupants");

            migrationBuilder.DropColumn(
                name: "NextMaintenanceDueDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiryDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PerformedBy",
                table: "AssetMaintenanceHistories");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Assets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriticalityLevel",
                table: "Assets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsHighRisk",
                table: "Assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "Assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskScore",
                table: "Assets",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DowntimeDuration",
                table: "AssetMaintenanceHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureType",
                table: "AssetMaintenanceHistories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyUnits_PropertyId",
                table: "PropertyUnits",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyUnits_UnitNumber",
                table: "PropertyUnits",
                column: "UnitNumber",
                unique: true);
        }
    }
}
