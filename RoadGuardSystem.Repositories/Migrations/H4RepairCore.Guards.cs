using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class H4RepairCore
{
    private static readonly string[] RepairCoreHistoryTables =
    [
        "RepairPolicyRevisions", "RepairPolicyMeasurementRules", "RepairPolicyRevocations",
        "RepairAttemptEvidence", "RepairAttempts", "RepairCorrectionEvidence", "RepairDecisions",
        "RepairObligationResolutionEvents", "RepairReviewRequests", "RepairWorkHandovers",
        "RepairSafetyResponsibilityTransfers", "RepairPolicyDraftChanges"
    ];
    private static readonly string[] RepairCoreMutableTables =
    [
        "RepairPackages", "RepairObligations", "RepairItems", "RepairPolicyDrafts",
        "RepairExecutionAuthorizations", "RepairTemporarySafetyMeasures", "RepairActualScopes"
    ];

    private static void RefuseRepairCoreLoss(MigrationBuilder migrationBuilder)
    {
        // A downgrade is permitted only for an empty new module; never erase published history.
        foreach (var table in RepairCoreHistoryTables.Concat(RepairCoreMutableTables))
            migrationBuilder.Sql($"IF EXISTS (SELECT 1 FROM [{table}]) THROW 51290, 'Cannot downgrade populated repair core.', 1;");
    }

    private static void InstallRepairCoreGuards(MigrationBuilder migrationBuilder)
    {
        foreach (var table in RepairCoreHistoryTables)
            migrationBuilder.Sql($"""
                CREATE TRIGGER [TR_{table}_Immutable] ON [{table}] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    {RepairHistoryInsertGuard(table)}
                END
                """);
        foreach (var table in RepairCoreMutableTables)
            migrationBuilder.Sql($"""
                CREATE TRIGGER [TR_{table}_Scope] ON [{table}] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    {RepairMutableGuard(table)}
                END
                """);
        migrationBuilder.Sql("CREATE UNIQUE INDEX [UX_RepairDecisions_Initial] ON [RepairDecisions] ([ItemId]) WHERE [SupersedesDecisionId] IS NULL;");
    }

    private static string RepairHistoryInsertGuard(string table) => table switch
    {
        "RepairDecisions" => """
            IF EXISTS (SELECT 1 FROM inserted d JOIN RepairItems i ON i.Id=d.ItemId
                WHERE d.ObligationId<>i.ObligationId OR d.DefectId<>i.DefectId OR d.Mode<>i.Mode
                OR d.Result NOT IN (1,2,3) OR (d.Mode=1 AND d.Role<>1) OR (d.Mode=2 AND d.Role<>2)
                OR d.Mode NOT IN (1,2) OR LEN(LTRIM(RTRIM(d.Reason)))=0
                OR (d.SupersedesDecisionId IS NOT NULL AND (i.EffectiveDecisionId IS NULL OR i.EffectiveDecisionId<>d.SupersedesDecisionId OR LEN(LTRIM(RTRIM(ISNULL(d.Basis_Text,''))))=0)))
                THROW 51202, 'Repair decision scope, role or correction basis is invalid.', 1;
            IF EXISTS (SELECT 1 FROM inserted d JOIN RepairDecisions p ON p.Id=d.SupersedesDecisionId
                WHERE p.ItemId<>d.ItemId OR p.ObligationId<>d.ObligationId OR p.DefectId<>d.DefectId OR p.Mode<>d.Mode OR d.At<p.At)
                THROW 51203, 'Repair correction must supersede its own chronological decision head.', 1;
            """,
        "RepairAttempts" => """
            IF EXISTS (SELECT 1 FROM inserted a JOIN RepairItems i ON i.Id=a.ItemId
                WHERE a.ProjectId<>i.ProjectId OR a.DefectId<>i.DefectId OR a.ObligationId<>i.ObligationId
                OR i.CrewId IS NULL OR a.CrewId<>i.CrewId OR LEN(a.PayloadHash)<>64)
                THROW 51204, 'Repair attempt source scope is invalid.', 1;
            """,
        "RepairReviewRequests" => """
            IF EXISTS (SELECT 1 FROM inserted r JOIN RepairItems i ON i.Id=r.ItemId JOIN RepairDecisions d ON d.Id=r.DecisionId
                WHERE r.ProjectId<>i.ProjectId OR d.ItemId<>i.Id OR r.Role NOT IN (2,4) OR LEN(LTRIM(RTRIM(r.Reason)))=0)
                THROW 51205, 'Repair review request source scope is invalid.', 1;
            """,
        "RepairObligationResolutionEvents" => """
            IF EXISTS (SELECT 1 FROM inserted e JOIN RepairDecisions d ON d.Id=e.DecisionId
                WHERE e.ObligationId<>d.ObligationId OR e.At<>d.At
                OR (e.Accepted=1 AND d.Result<>3) OR (e.Accepted=0 AND d.Result=3)
                OR EXISTS (SELECT e.SupersedesDecisionId EXCEPT SELECT d.SupersedesDecisionId))
                THROW 51206, 'Repair resolution history must match its exact decision.', 1;
            """,
        "RepairCorrectionEvidence" => """
            IF EXISTS (SELECT 1 FROM inserted e JOIN RepairDecisions d ON d.Id=e.DecisionId
                WHERE LEN(LTRIM(RTRIM(ISNULL(d.Basis_Text,''))))=0
                    OR EXISTS (SELECT 1 FROM RepairItems i WHERE i.EffectiveDecisionId=d.Id)
                    OR EXISTS (SELECT 1 FROM RepairObligationResolutionEvents h WHERE h.DecisionId=d.Id)
                    OR EXISTS (SELECT 1 FROM RepairDecisions c WHERE c.SupersedesDecisionId=d.Id))
                THROW 51208, 'Repair decision evidence must be staged before publication and cannot alter published history.', 1;
            """,
        "RepairPolicyMeasurementRules" => """
            IF EXISTS (SELECT 1 FROM inserted WHERE Minimum>Maximum OR LEN(LTRIM(RTRIM(Code)))=0 OR LEN(LTRIM(RTRIM(Unit)))=0)
                THROW 51207, 'Repair policy measurement rule is invalid.', 1;
            IF EXISTS (SELECT 1 FROM inserted r WHERE EXISTS (SELECT 1 FROM RepairPolicyDrafts d WHERE d.PublishedRevisionId=r.PolicyRevisionId)
                OR EXISTS (SELECT 1 FROM RepairExecutionAuthorizations a WHERE a.PolicyRevisionId=r.PolicyRevisionId))
                THROW 51209, 'Published or used repair policy measurement rules cannot be extended.', 1;
            """,
        _ => ""
    };

    private static string RepairMutableGuard(string table) => table switch
    {
        "RepairPackages" => """
            IF EXISTS (SELECT 1 FROM inserted i JOIN Defects d ON d.Id=i.DefectId WHERE d.ProjectId IS NULL OR i.ProjectId<>d.ProjectId)
                THROW 51210, 'Repair package project must match its defect.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.MutationRevision<=d.MutationRevision)
                THROW 51211, 'Repair package identity is immutable and mutation revision must advance.', 1;
            """,
        "RepairObligations" => """
            IF EXISTS (SELECT 1 FROM inserted i JOIN Defects d ON d.Id=i.DefectId WHERE d.ProjectId IS NULL OR i.ProjectId<>d.ProjectId OR i.Kind NOT IN (1,2))
                THROW 51212, 'Repair obligation defect scope is invalid.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPackages p ON p.Id=i.PackageId WHERE p.ProjectId<>i.ProjectId OR p.DefectId<>i.DefectId)
                THROW 51212, 'Repair obligation package scope is invalid.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.Kind<>d.Kind OR i.Mandatory<>d.Mandatory
                OR EXISTS (SELECT i.PackageId EXCEPT SELECT d.PackageId))
                THROW 51213, 'Repair obligation identity is immutable.', 1;
            IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN RepairDecisions h ON h.Id=i.EffectiveResolutionHeadDecisionId
                WHERE (i.EffectiveResolutionHeadDecisionId IS NULL AND i.EffectiveResolutionDecisionId IS NOT NULL)
                OR (h.Id IS NOT NULL AND (h.ObligationId<>i.Id OR h.DefectId<>i.DefectId
                    OR (h.Result=3 AND (i.EffectiveResolutionDecisionId IS NULL OR i.EffectiveResolutionDecisionId<>h.Id))
                    OR (h.Result<>3 AND i.EffectiveResolutionDecisionId IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM RepairDecisions c WHERE c.SupersedesDecisionId=h.Id))))
                THROW 51214, 'Repair obligation effective resolution must follow its terminal decision.', 1;
            """,
        "RepairItems" => """
            IF EXISTS (SELECT 1 FROM inserted i JOIN RepairObligations o ON o.Id=i.ObligationId
                WHERE i.ProjectId<>o.ProjectId OR i.DefectId<>o.DefectId OR i.Mode NOT IN (1,2)
                OR EXISTS (SELECT i.PackageId EXCEPT SELECT o.PackageId))
                THROW 51215, 'Repair item obligation scope is invalid.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.ObligationId<>d.ObligationId OR i.Mode<>d.Mode
                OR EXISTS (SELECT i.PackageId,i.RepairPlan,i.ChecklistVersion,i.ProposalPlanHash EXCEPT SELECT d.PackageId,d.RepairPlan,d.ChecklistVersion,d.ProposalPlanHash))
                THROW 51216, 'Repair item identity and proposed plan are immutable.', 1;
            IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN RepairDecisions h ON h.Id=i.EffectiveDecisionId
                WHERE (i.EffectiveDecisionId IS NULL AND i.State IN (7,9)) OR
                (h.Id IS NOT NULL AND (h.ItemId<>i.Id OR h.ObligationId<>i.ObligationId OR h.DefectId<>i.DefectId OR h.Mode<>i.Mode
                    OR (h.Result=3 AND i.State<>7) OR (h.Result<>3 AND i.State<>9)
                    OR EXISTS (SELECT 1 FROM RepairDecisions c WHERE c.SupersedesDecisionId=h.Id))))
                THROW 51217, 'Repair item state must reflect its terminal effective decision.', 1;
            """,
        "RepairActualScopes" => """
            IF EXISTS (SELECT 1 FROM deleted) THROW 51218, 'Repair actual scope is immutable.', 1;
            IF EXISTS (SELECT 1 FROM inserted s JOIN RepairObligations o ON o.Id=s.ObligationId
                JOIN Defects d ON d.Id=o.DefectId LEFT JOIN RoadSectionVersions r ON r.Id=d.RoadSectionVersionId
                WHERE r.Id IS NULL OR s.PhysicalRoadId<>r.RoadSectionId OR s.[From]<0 OR s.[From]>=s.[To] OR s.OffsetFrom>=s.OffsetTo OR LEN(LTRIM(RTRIM(s.LocationVersion)))=0)
                THROW 51219, 'Repair scope must use the defect physical road and valid bounds.', 1;
            """,
        "RepairPolicyDrafts" => """
            IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ProjectId<>d.ProjectId
                OR (d.PublishedRevisionId IS NOT NULL AND EXISTS (SELECT i.CurrentChangeId,i.PublishedRevisionId EXCEPT SELECT d.CurrentChangeId,d.PublishedRevisionId)))
                THROW 51220, 'Published repair draft and project are immutable.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId WHERE c.DraftId IS NULL OR c.DraftId<>i.Id)
                THROW 51221, 'Repair draft head must reference its own change.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyRevisions r ON r.Id=i.PublishedRevisionId
                LEFT JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId
                WHERE i.ProjectId<>r.ProjectId OR c.Id IS NULL OR c.DefectTypeCode<>r.DefectTypeCode OR c.ChecklistVersion<>r.ChecklistVersion OR c.StopConditions<>r.StopConditions)
                THROW 51222, 'Repair publication must pin its own project and draft source.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId
                WHERE i.PublishedRevisionId IS NOT NULL AND (ISJSON(c.Measurements)<>1 OR LEFT(LTRIM(c.Measurements),1)<>'['))
                THROW 51222, 'Repair publication measurement source must be a valid array.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId
                WHERE i.PublishedRevisionId IS NOT NULL AND (
                    EXISTS (SELECT j.Code COLLATE Latin1_General_100_BIN2,j.Unit COLLATE Latin1_General_100_BIN2,j.Minimum,j.Maximum
                        FROM OPENJSON(c.Measurements) WITH (Code nvarchar(200) '$.Code',Unit nvarchar(80) '$.Unit',Minimum decimal(20,6) '$.Minimum',Maximum decimal(20,6) '$.Maximum') j
                        EXCEPT SELECT r.Code COLLATE Latin1_General_100_BIN2,r.Unit COLLATE Latin1_General_100_BIN2,r.Minimum,r.Maximum
                        FROM RepairPolicyMeasurementRules r WHERE r.PolicyRevisionId=i.PublishedRevisionId)
                    OR EXISTS (SELECT r.Code COLLATE Latin1_General_100_BIN2,r.Unit COLLATE Latin1_General_100_BIN2,r.Minimum,r.Maximum
                        FROM RepairPolicyMeasurementRules r WHERE r.PolicyRevisionId=i.PublishedRevisionId
                        EXCEPT SELECT j.Code COLLATE Latin1_General_100_BIN2,j.Unit COLLATE Latin1_General_100_BIN2,j.Minimum,j.Maximum
                        FROM OPENJSON(c.Measurements) WITH (Code nvarchar(200) '$.Code',Unit nvarchar(80) '$.Unit',Minimum decimal(20,6) '$.Minimum',Maximum decimal(20,6) '$.Maximum') j)))
                THROW 51222, 'Repair publication measurement rules must equal its immutable draft head.', 1;
            """,
        "RepairExecutionAuthorizations" => """
            IF EXISTS (SELECT 1 FROM inserted i JOIN Defects d ON d.Id=i.DefectId JOIN RepairPolicyRevisions p ON p.Id=i.PolicyRevisionId
                WHERE d.ProjectId IS NULL OR i.ProjectId<>d.ProjectId OR i.ProjectId<>p.ProjectId OR i.Permission NOT IN (1,2))
                THROW 51223, 'Repair execution authorization source scope is invalid.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.TaskId<>d.TaskId OR i.AssignmentId<>d.AssignmentId OR i.CrewId<>d.CrewId
                OR i.IssuedBy<>d.IssuedBy OR i.IssuedAt<>d.IssuedAt OR i.PolicyRevisionId<>d.PolicyRevisionId OR i.Permission<>d.Permission OR i.LocationVersion<>d.LocationVersion
                OR (d.FirstStartOriginId IS NOT NULL AND EXISTS (SELECT i.FirstStartOriginId,i.FirstStartPayloadHash,i.FirstStartedAt,i.FirstServerReceivedAt EXCEPT SELECT d.FirstStartOriginId,d.FirstStartPayloadHash,d.FirstStartedAt,d.FirstServerReceivedAt))
                OR (d.VerifiedStartedAt IS NOT NULL AND EXISTS (SELECT i.VerifiedStartedAt,i.ExpiresAt EXCEPT SELECT d.VerifiedStartedAt,d.ExpiresAt)))
                THROW 51224, 'Repair authorization identity and first clock origins are immutable.', 1;
            IF EXISTS (SELECT 1 FROM inserted WHERE (VerifiedStartedAt IS NULL AND ExpiresAt IS NOT NULL)
                OR (VerifiedStartedAt IS NOT NULL AND (ExpiresAt IS NULL OR ExpiresAt<>DATEADD(hour,24,VerifiedStartedAt))))
                THROW 51225, 'Repair execution due must remain first verified start plus 24 hours.', 1;
            """,
        "RepairTemporarySafetyMeasures" => """
            IF EXISTS (SELECT 1 FROM inserted i JOIN RepairObligations o ON o.Id=i.FormalRepairObligationId
                WHERE i.ProjectId<>o.ProjectId OR i.DefectId<>o.DefectId OR o.Kind<>1)
                THROW 51226, 'Temporary safety must retain its own formal repair obligation.', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.FormalRepairObligationId<>d.FormalRepairObligationId
                OR (d.InstalledAt IS NOT NULL AND EXISTS (SELECT i.InstallationEventId,i.InstalledBy,i.InstalledAt,i.FirstCheckDueAt EXCEPT SELECT d.InstallationEventId,d.InstalledBy,d.InstalledAt,d.FirstCheckDueAt)))
                THROW 51227, 'Temporary safety scope and original first-check clock are immutable.', 1;
            IF EXISTS (SELECT 1 FROM inserted WHERE InstalledAt IS NOT NULL AND (FirstCheckDueAt IS NULL OR FirstCheckDueAt<InstalledAt OR FirstCheckDueAt>DATEADD(hour,24,InstalledAt)))
                THROW 51228, 'Temporary safety first check must be within 24 hours of installation.', 1;
            """,
        _ => throw new InvalidOperationException("Unregistered repair core guard table.")
    };
}
