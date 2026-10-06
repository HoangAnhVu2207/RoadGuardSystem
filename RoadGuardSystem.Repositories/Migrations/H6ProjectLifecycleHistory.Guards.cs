using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class H6ProjectLifecycleHistory
{
    private static void InstallLifecycleHistoryGuards(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_ProjectLifecycleHistory_Immutable ON ProjectLifecycleHistory AFTER UPDATE,DELETE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM deleted) THROW 51500, 'Lifecycle source history is append-only.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_ProjectLifecycleHistory_Scope ON ProjectLifecycleHistory AFTER INSERT AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted i WHERE
                    (i.ObligationId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM RepairObligations o WHERE o.Id=i.ObligationId AND o.ProjectId=i.ProjectId))
                    OR (i.DefectId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.DefectId AND d.ProjectId=i.ProjectId))
                    OR (i.LinkedDefectId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.LinkedDefectId AND d.ProjectId=i.ProjectId AND d.Id<>i.DefectId))
                    OR (i.Kind=3 AND (i.ObligationId IS NULL OR i.ReceiverId IS NULL OR i.GrantId IS NULL OR i.GrantId<>i.Id))
                    OR (i.Kind=4 AND NOT EXISTS(SELECT 1 FROM ProjectLifecycleHistory g WHERE g.Id=i.GrantId AND g.Kind=3
                        AND g.ProjectId=i.ProjectId AND g.ObligationId=i.ObligationId AND g.ReceiverId=i.ReceiverId
                        AND g.RecordedAtUtc<=i.RecordedAtUtc))
                    OR (i.Kind NOT IN (3,4) AND (i.ObligationId IS NOT NULL OR i.GrantId IS NOT NULL OR i.ReceiverId IS NOT NULL))
                    OR (i.Kind IN (7,8) AND i.DefectId IS NULL)
                    OR (i.Kind NOT IN (7,8) AND i.DefectId IS NOT NULL)
                    OR (i.Kind=8 AND i.LinkedDefectId IS NULL)
                    OR (i.Kind<>8 AND i.LinkedDefectId IS NOT NULL)
                    OR (i.Kind=5 AND i.SourceDisposition='TARGET_CONFIRMED' AND NOT EXISTS(
                        SELECT 1 FROM ProjectLifecycleHistory c WHERE c.Id=i.OperationalClosureId AND c.ProjectId=i.ProjectId
                        AND c.Kind=2 AND c.SourceDisposition='TARGET_CONFIRMED' AND LEN(c.AuthoritySourceReference)>0
                        AND c.RecordedAtUtc<=i.RecordedAtUtc))
                    OR (i.Kind<>5 AND i.OperationalClosureId IS NOT NULL))
                    THROW 51501, 'Lifecycle source pins must retain their exact project and history relation.', 1;
            END
            """);
    }
}
