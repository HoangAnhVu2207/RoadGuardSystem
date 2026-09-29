using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P2DatasetProcessingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "SurveyDataVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecordedAt",
                table: "SurveyDataVersions",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SurveyDataVersions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<string>(
                name: "ScopeManifest",
                table: "SurveyDataVersions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "SurveyDataVersions");

            migrationBuilder.DropColumn(
                name: "RecordedAt",
                table: "SurveyDataVersions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SurveyDataVersions");

            migrationBuilder.DropColumn(
                name: "ScopeManifest",
                table: "SurveyDataVersions");
        }
    }
}
