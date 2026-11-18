using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class WebPmCoordinationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Organization",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RepairPackages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "RepairPackages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlannedDueAt",
                table: "RepairPackages",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlannedMaterials",
                table: "RepairItems",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedQuantity",
                table: "RepairItems",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlannedUnit",
                table: "RepairItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locality",
                table: "Projects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RepairItems_PlannedQuantity",
                table: "RepairItems",
                sql: "([PlannedQuantity] IS NULL AND [PlannedUnit] IS NULL) OR ([PlannedQuantity] IS NOT NULL AND [PlannedQuantity] > 0 AND [PlannedUnit] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RepairItems_PlannedQuantity",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "Organization",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RepairPackages");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "RepairPackages");

            migrationBuilder.DropColumn(
                name: "PlannedDueAt",
                table: "RepairPackages");

            migrationBuilder.DropColumn(
                name: "PlannedMaterials",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "PlannedQuantity",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "PlannedUnit",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "Locality",
                table: "Projects");
        }
    }
}
