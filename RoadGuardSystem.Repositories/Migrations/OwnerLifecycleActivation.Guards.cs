using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class OwnerLifecycleActivation
{
    private static void InstallOwnerLifecycleGuards(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_ProjectLifecycleHistory_Production ON ProjectLifecycleHistory AFTER INSERT AS
            BEGIN SET NOCOUNT ON;
              IF EXISTS(SELECT 1 FROM inserted i WHERE i.AuthoritySourceReference LIKE 'LD06_PRODUCTION_ACTION:%'
                AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions a WHERE a.Id=i.Id AND a.ProjectId=i.ProjectId
                  AND a.Kind=4 AND i.Kind=2 AND a.ActorId=i.ActorId AND a.At=i.RecordedAtUtc
                  AND i.SourceDisposition='TARGET_CONFIRMED'
                  AND i.AuthoritySourceReference='LD06_PRODUCTION_ACTION:'+CONVERT(nvarchar(36),a.Id)))
                THROW 51604, 'Production closure history requires its exact committed action.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_LD06LifecycleActions_Immutable ON LD06LifecycleActions AFTER UPDATE,DELETE AS
            BEGIN SET NOCOUNT ON;
              IF EXISTS(SELECT 1 FROM deleted) THROW 51600, 'Confirmed lifecycle actions are append-only.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_LD06LifecycleActions_Scope ON LD06LifecycleActions AFTER INSERT AS
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
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_ObligationResponsibilities_Scope ON ObligationResponsibilities AFTER INSERT,UPDATE,DELETE AS
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
            CREATE TRIGGER TR_LD06ActionEvidence_Immutable ON LD06ActionEvidence AFTER UPDATE,DELETE AS
            BEGIN SET NOCOUNT ON;
              IF EXISTS(SELECT 1 FROM deleted) THROW 51603, 'Lifecycle evidence pins are append-only.', 1;
            END
            """);
    }
}
