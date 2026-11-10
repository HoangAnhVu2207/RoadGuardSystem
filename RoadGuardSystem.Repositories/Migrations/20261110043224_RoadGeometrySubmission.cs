using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class RoadGeometrySubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "RoadGeometryDrafts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedBy",
                table: "RoadGeometryDrafts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedContentVersion",
                table: "RoadGeometryDrafts",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_RoadGeometryDrafts_ProjectSubmission",
                table: "RoadGeometryDrafts",
                column: "ProjectId",
                unique: true,
                filter: "[SubmittedAt] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoadGeometryDrafts_Submission",
                table: "RoadGeometryDrafts",
                sql: "([SubmittedAt] IS NULL AND [SubmittedBy] IS NULL AND [SubmittedContentVersion] IS NULL AND [Status] <> 'SUBMITTED') OR ([SubmittedAt] IS NOT NULL AND [SubmittedBy] IS NOT NULL AND [SubmittedContentVersion] IS NOT NULL AND [Status] IN ('SUBMITTED', 'CONFIRMED'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [RoadGeometryDrafts] WHERE [SubmittedAt] IS NOT NULL) THROW 51000, 'Cannot downgrade with submitted road geometry dossiers.', 1;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoadGeometryDrafts_Submission",
                table: "RoadGeometryDrafts");

            migrationBuilder.DropIndex(
                name: "UX_RoadGeometryDrafts_ProjectSubmission",
                table: "RoadGeometryDrafts");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "RoadGeometryDrafts");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "RoadGeometryDrafts");

            migrationBuilder.DropColumn(
                name: "SubmittedContentVersion",
                table: "RoadGeometryDrafts");
        }
    }
}
