using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialTemporarySafetyResponsibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LD06LifecycleActions_Kind",
                table: "LD06LifecycleActions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LD06LifecycleActions_Kind",
                table: "LD06LifecycleActions",
                sql: "[Kind] BETWEEN 1 AND 8 AND ISJSON([FactsJson])=1");
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER TR_LD06LifecycleActions_Scope ON LD06LifecycleActions AFTER INSERT AS
                                BEGIN SET NOCOUNT ON;
                                  IF EXISTS(SELECT 1 FROM inserted i WHERE
                                    (i.Kind=1 AND i.SourceActionId IS NOT NULL)
                                    OR (i.Kind=2 AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions s WHERE s.Id=i.SourceActionId
                                        AND s.Kind=1 AND s.ProjectId=i.ProjectId AND s.At<=i.At))
                                    OR (i.Kind IN (3,5) AND NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.DefectId AND d.ProjectId=i.ProjectId))
                                    OR (i.Kind=5 AND (i.LinkedDefectId=i.DefectId OR NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.LinkedDefectId AND d.ProjectId=i.ProjectId)
                                        OR NOT EXISTS(SELECT 1 FROM RepairDecisions d JOIN RepairObligations o ON o.Id=d.ObligationId
                                            WHERE d.Id=i.PriorRepairDecisionId AND o.DefectId=i.DefectId AND o.ProjectId=i.ProjectId
                                            AND o.EffectiveResolutionDecisionId=d.Id)))
                                    OR (i.Kind=6 AND (i.ReceivingProjectId IS NULL OR i.ReceivingProjectId=i.ProjectId OR LEN(i.ScopeHash)<>64
                                        OR NOT EXISTS(SELECT 1 FROM RepairObligations o LEFT JOIN ObligationResponsibilities r ON r.ObligationId=o.Id
                                            WHERE o.Id=i.ObligationId AND COALESCE(r.CurrentProjectId,o.ProjectId)=i.ProjectId)))
                                    OR (i.Kind=7 AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions s WHERE s.Id=i.SourceActionId AND s.Kind=6
                                        AND s.ReceivingProjectId=i.ProjectId AND s.ObligationId=i.ObligationId AND s.ScopeHash=i.ScopeHash
                                        AND s.At<=i.At))
                                    OR (i.Kind=8 AND (LEN(i.ScopeHash)<>64 OR i.ReceivingProjectId IS NOT NULL OR NOT EXISTS(
                                      SELECT 1 FROM RepairObligations safety JOIN RepairObligations formal ON formal.Id=TRY_CONVERT(uniqueidentifier,JSON_VALUE(i.FactsJson,'$.formalObligationId'))
                                      JOIN RepairItems item ON item.Id=TRY_CONVERT(uniqueidentifier,JSON_VALUE(i.FactsJson,'$.itemId'))
                                      JOIN RepairActualScopes ss ON ss.ObligationId=safety.Id JOIN RepairActualScopes fs ON fs.ObligationId=formal.Id
                                      JOIN ObligationResponsibilities owner ON owner.ObligationId=formal.Id
                                      JOIN AuditLogs audit ON audit.EntityId=safety.Id AND audit.EventType='repair_temporary_safety_added' AND audit.EntityType='RepairObligation'
                                      WHERE safety.Id=i.ObligationId AND safety.Kind=2 AND formal.Kind=1 AND safety.ProjectId=formal.ProjectId
                                        AND safety.DefectId=formal.DefectId AND i.DefectId=safety.DefectId AND safety.PackageId=formal.PackageId
                                        AND item.PackageId=formal.PackageId AND item.ObligationId=formal.Id AND formal.CurrentRepairItemId=item.Id
                                        AND item.Mode=1 AND item.DefectId=formal.DefectId AND item.ProjectId=formal.ProjectId
                                        AND safety.EffectiveResolutionDecisionId IS NULL AND formal.EffectiveResolutionDecisionId IS NULL
                                        AND owner.OriginProjectId=formal.ProjectId AND owner.CurrentProjectId=i.ProjectId AND owner.AcceptanceActionId=i.SourceActionId
                                        AND i.ProjectId<>safety.ProjectId AND audit.ActorUserId=i.ActorId AND audit.OccurredAtUtc<=i.At
                                        AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(audit.AfterSnapshot,'$.formalObligationId'))=formal.Id
                                        AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(audit.AfterSnapshot,'$.itemId'))=item.Id
                                        AND ss.PhysicalRoadId=fs.PhysicalRoadId AND ss.LocationVersion=fs.LocationVersion
                                        AND ss.[From]>=fs.[From] AND ss.[To]<=fs.[To] AND ss.OffsetFrom>=fs.OffsetFrom AND ss.OffsetTo<=fs.OffsetTo
                                        AND NOT EXISTS(SELECT 1 FROM ObligationResponsibilities prior WHERE prior.ObligationId=safety.Id)))))
                                    THROW 51601, 'Lifecycle action must retain its exact source, project and scope.', 1;
                                END
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER TR_ObligationResponsibilities_Scope ON ObligationResponsibilities AFTER INSERT,UPDATE,DELETE AS
                                BEGIN SET NOCOUNT ON;
                                  IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.ObligationId=d.ObligationId
                                      WHERE i.ObligationId IS NULL OR i.OriginProjectId<>d.OriginProjectId)
                                    THROW 51602, 'Responsibility history and original project cannot be erased.', 1;
                                  IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(
                                      SELECT 1 FROM RepairObligations o JOIN LD06LifecycleActions a ON a.ObligationId=o.Id
                                      JOIN LD06LifecycleActions s ON s.Id=a.SourceActionId
                                      WHERE o.Id=i.ObligationId AND o.ProjectId=i.OriginProjectId AND a.Id=i.AcceptanceActionId
                                        AND a.Kind=7 AND s.Kind=6 AND a.ProjectId=i.CurrentProjectId
                                        AND s.ReceivingProjectId=a.ProjectId AND a.ScopeHash=s.ScopeHash)
                                    AND NOT EXISTS(SELECT 1 FROM RepairObligations o JOIN LD06LifecycleActions a ON a.ObligationId=o.Id
                                        WHERE o.Id=i.ObligationId AND o.Kind=2 AND o.ProjectId=i.OriginProjectId AND a.Id=i.AcceptanceActionId
                                          AND a.Kind=8 AND a.ProjectId=i.CurrentProjectId))
                                    THROW 51602, 'Responsibility requires a committed exact receiving-project acceptance.', 1;
                                  IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.ObligationId=i.ObligationId
                                      LEFT JOIN LD06LifecycleActions a ON a.Id=i.AcceptanceActionId LEFT JOIN LD06LifecycleActions s ON s.Id=a.SourceActionId
                                      WHERE i.AcceptanceActionId=d.AcceptanceActionId OR a.Kind<>7 OR s.Kind<>6 OR s.ProjectId<>d.CurrentProjectId)
                                    THROW 51602, 'Responsibility changes must continue the current accepted owner.', 1;
                                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS(SELECT 1 FROM LD06LifecycleActions WHERE Kind=8) THROW 51991, 'Initial safety responsibility history prevents downgrade; restore a reviewed backup instead.', 1;
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER TR_ObligationResponsibilities_Scope ON ObligationResponsibilities AFTER INSERT,UPDATE,DELETE AS
                                BEGIN SET NOCOUNT ON;
                                  IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.ObligationId=d.ObligationId
                                      WHERE i.ObligationId IS NULL OR i.OriginProjectId<>d.OriginProjectId)
                                    THROW 51602, 'Responsibility history and original project cannot be erased.', 1;
                                  IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(
                                      SELECT 1 FROM RepairObligations o JOIN LD06LifecycleActions a ON a.ObligationId=o.Id
                                      JOIN LD06LifecycleActions s ON s.Id=a.SourceActionId
                                      WHERE o.Id=i.ObligationId AND o.ProjectId=i.OriginProjectId AND a.Id=i.AcceptanceActionId
                                        AND a.Kind=7 AND s.Kind=6 AND a.ProjectId=i.CurrentProjectId
                                        AND s.ReceivingProjectId=a.ProjectId AND a.ScopeHash=s.ScopeHash))
                                    THROW 51602, 'Responsibility requires a committed exact receiving-project acceptance.', 1;
                                  IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.ObligationId=i.ObligationId
                                      JOIN LD06LifecycleActions a ON a.Id=i.AcceptanceActionId JOIN LD06LifecycleActions s ON s.Id=a.SourceActionId
                                      WHERE i.AcceptanceActionId=d.AcceptanceActionId OR s.ProjectId<>d.CurrentProjectId)
                                    THROW 51602, 'Responsibility changes must continue the current accepted owner.', 1;
                                END
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER TR_LD06LifecycleActions_Scope ON LD06LifecycleActions AFTER INSERT AS
                                BEGIN SET NOCOUNT ON;
                                  IF EXISTS(SELECT 1 FROM inserted i WHERE
                                    (i.Kind=1 AND i.SourceActionId IS NOT NULL)
                                    OR (i.Kind=2 AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions s WHERE s.Id=i.SourceActionId
                                        AND s.Kind=1 AND s.ProjectId=i.ProjectId AND s.At<=i.At))
                                    OR (i.Kind IN (3,5) AND NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.DefectId AND d.ProjectId=i.ProjectId))
                                    OR (i.Kind=5 AND (i.LinkedDefectId=i.DefectId OR NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.LinkedDefectId AND d.ProjectId=i.ProjectId)
                                        OR NOT EXISTS(SELECT 1 FROM RepairDecisions d JOIN RepairObligations o ON o.Id=d.ObligationId
                                            WHERE d.Id=i.PriorRepairDecisionId AND o.DefectId=i.DefectId AND o.ProjectId=i.ProjectId
                                            AND o.EffectiveResolutionDecisionId=d.Id)))
                                    OR (i.Kind=6 AND (i.ReceivingProjectId IS NULL OR i.ReceivingProjectId=i.ProjectId OR LEN(i.ScopeHash)<>64
                                        OR NOT EXISTS(SELECT 1 FROM RepairObligations o LEFT JOIN ObligationResponsibilities r ON r.ObligationId=o.Id
                                            WHERE o.Id=i.ObligationId AND COALESCE(r.CurrentProjectId,o.ProjectId)=i.ProjectId)))
                                    OR (i.Kind=7 AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions s WHERE s.Id=i.SourceActionId AND s.Kind=6
                                        AND s.ReceivingProjectId=i.ProjectId AND s.ObligationId=i.ObligationId AND s.ScopeHash=i.ScopeHash
                                        AND s.At<=i.At)))
                                    THROW 51601, 'Lifecycle action must retain its exact source, project and scope.', 1;
                                END
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_LD06LifecycleActions_Kind",
                table: "LD06LifecycleActions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LD06LifecycleActions_Kind",
                table: "LD06LifecycleActions",
                sql: "[Kind] BETWEEN 1 AND 7 AND ISJSON([FactsJson])=1");
        }
    }
}
