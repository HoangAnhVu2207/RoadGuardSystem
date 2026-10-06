using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H4RepairContinuationSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceCancellationEventId",
                table: "RepairNormalSuccessors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceHandoverEventId",
                table: "RepairNormalSuccessors",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_SourceCancellationEventId",
                table: "RepairNormalSuccessors",
                column: "SourceCancellationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_SourceHandoverEventId",
                table: "RepairNormalSuccessors",
                column: "SourceHandoverEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairNormalSuccessors_FieldInspectionTaskEvents_SourceHandoverEventId",
                table: "RepairNormalSuccessors",
                column: "SourceHandoverEventId",
                principalTable: "FieldInspectionTaskEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairNormalSuccessors_RepairItemLifecycleEvents_SourceCancellationEventId",
                table: "RepairNormalSuccessors",
                column: "SourceCancellationEventId",
                principalTable: "RepairItemLifecycleEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            ReplaceCanonicalKinds(migrationBuilder, forward: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM FieldInspectionOperationOrigins WHERE Kind NOT IN ('FIELD_START','FIELD_SUBMISSION')) THROW 51392, 'Populated canonical repair or accept history requires a data-preserving migration.', 1;");
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM RepairNormalSuccessors WHERE SourceCancellationEventId IS NOT NULL OR SourceHandoverEventId IS NOT NULL) THROW 51391, 'Populated continuation source history requires a data-preserving migration.', 1;");
            ReplaceCanonicalKinds(migrationBuilder, forward: false);
            migrationBuilder.DropForeignKey(
                name: "FK_RepairNormalSuccessors_FieldInspectionTaskEvents_SourceHandoverEventId",
                table: "RepairNormalSuccessors");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairNormalSuccessors_RepairItemLifecycleEvents_SourceCancellationEventId",
                table: "RepairNormalSuccessors");

            migrationBuilder.DropIndex(
                name: "IX_RepairNormalSuccessors_SourceCancellationEventId",
                table: "RepairNormalSuccessors");

            migrationBuilder.DropIndex(
                name: "IX_RepairNormalSuccessors_SourceHandoverEventId",
                table: "RepairNormalSuccessors");

            migrationBuilder.DropColumn(
                name: "SourceCancellationEventId",
                table: "RepairNormalSuccessors");

            migrationBuilder.DropColumn(
                name: "SourceHandoverEventId",
                table: "RepairNormalSuccessors");
        }
    }
}
