using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class TemporarySafetyResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SafetyResolutionId",
                table: "RepairObligations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_SafetyResolutionId",
                table: "RepairObligations",
                column: "SafetyResolutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairObligations_AuditLogs_SafetyResolutionId",
                table: "RepairObligations",
                column: "SafetyResolutionId",
                principalTable: "AuditLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER TR_RepairObligations_SafetyResolution ON RepairObligations AFTER UPDATE AS
                BEGIN SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted d JOIN inserted i ON i.Id=d.Id
                        WHERE d.SafetyResolutionId IS NOT NULL AND
                            (i.SafetyResolutionId IS NULL OR i.SafetyResolutionId<>d.SafetyResolutionId))
                        THROW 51143, 'Safety resolution cannot be reopened or replaced.', 1;
                    IF EXISTS(SELECT 1 FROM inserted i LEFT JOIN AuditLogs a ON a.Id=i.SafetyResolutionId
                        WHERE i.SafetyResolutionId IS NOT NULL AND (i.Kind<>2 OR a.Id IS NULL OR
                            a.EntityType<>'RepairObligation' OR a.EntityId<>i.Id OR
                            a.EventType<>'repair_safety_resolved' OR a.Source<>'h4.repair.safety.resolve.v1'))
                        THROW 51144, 'Safety resolution requires its immutable scoped decision.', 1;
                END
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM RepairObligations WHERE SafetyResolutionId IS NOT NULL) THROW 51145, 'Cannot remove populated safety resolution history.', 1;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_RepairObligations_SafetyResolution;");
            migrationBuilder.DropForeignKey(
                name: "FK_RepairObligations_AuditLogs_SafetyResolutionId",
                table: "RepairObligations");

            migrationBuilder.DropIndex(
                name: "IX_RepairObligations_SafetyResolutionId",
                table: "RepairObligations");

            migrationBuilder.DropColumn(
                name: "SafetyResolutionId",
                table: "RepairObligations");
        }
    }
}
