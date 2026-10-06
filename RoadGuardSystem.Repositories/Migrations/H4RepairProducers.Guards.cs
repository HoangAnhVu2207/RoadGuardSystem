using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class H4RepairProducers
{
    private static readonly string[] ProducerHistoryTables =
    [
        "RepairFieldTaskBindings", "RepairMeasurementAssessments", "RepairAssessmentMeasurements",
        "RepairAssessmentEvidence", "RepairExecutionStarts", "RepairExecutionFinishes",
        "RepairAttemptSubmissionLinks", "RepairAttemptReviews", "RepairItemLifecycleEvents",
        "RepairNormalSuccessors", "RepairEligibilityAssessments", "RepairEligibilityWarrantySources",
        "RepairEligibilityHandoverSources", "RepairSafetyChecks", "RepairSafetyCheckEvidence",
        "RepairDangerWarnings", "RepairDangerAcknowledgements"
    ];

    private static void InstallRepairProducerGuards(MigrationBuilder migrationBuilder)
    {
        ReplaceNativeTaskModeGuard(migrationBuilder, forward: true);
        ReplaceNativeSessionPurposeGuard(migrationBuilder, forward: true);
        foreach (var table in ProducerHistoryTables)
            migrationBuilder.Sql($"""
                CREATE TRIGGER [TR_{table}_Immutable] ON [{table}] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
        migrationBuilder.Sql("""
            CREATE TRIGGER [TR_RepairSafetyMonitoring_Scope] ON [RepairSafetyMonitoring] AFTER INSERT, UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                    THROW 51301, 'Repair monitoring cannot be deleted.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                    WHERE EXISTS (SELECT i.MeasureId,i.SafetyObligationId,i.FormalObligationId
                        EXCEPT SELECT d.MeasureId,d.SafetyObligationId,d.FormalObligationId))
                    THROW 51301, 'Repair monitoring source pins are immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN RepairTemporarySafetyMeasures m ON m.Id=i.MeasureId
                    LEFT JOIN RepairObligations s ON s.Id=i.SafetyObligationId
                    LEFT JOIN RepairObligations f ON f.Id=i.FormalObligationId
                    WHERE i.Id<>i.MeasureId OR m.Id IS NULL OR s.Id IS NULL OR f.Id IS NULL
                        OR s.Kind<>2 OR f.Kind<>1 OR s.ProjectId<>m.ProjectId OR f.ProjectId<>m.ProjectId
                        OR s.DefectId<>m.DefectId OR f.DefectId<>m.DefectId OR m.FormalRepairObligationId<>f.Id
                        OR (i.CurrentCheckId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RepairSafetyChecks c
                            WHERE c.Id=i.CurrentCheckId AND c.MeasureId=i.MeasureId)))
                    THROW 51301, 'Repair monitoring must retain the actual obligation and check scope.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER [TR_RepairObligations_ProducerHeads] ON [RepairObligations] AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted o WHERE o.CurrentRepairItemId IS NOT NULL AND NOT EXISTS
                    (SELECT 1 FROM RepairItems i WHERE i.Id=o.CurrentRepairItemId AND i.ObligationId=o.Id
                        AND i.ProjectId=o.ProjectId AND i.DefectId=o.DefectId))
                    THROW 51310, 'Current repair item must belong to the actual obligation.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                    WHERE d.OriginalCrewFirstStartId IS NOT NULL AND EXISTS
                        (SELECT i.OriginalCrewFirstStartId EXCEPT SELECT d.OriginalCrewFirstStartId))
                    THROW 51311, 'Original Crew first-start cannot be reset.', 1;
                IF EXISTS (SELECT 1 FROM inserted o WHERE o.OriginalCrewFirstStartId IS NOT NULL AND NOT EXISTS
                    (SELECT 1 FROM FieldTaskStartOrigins s JOIN RepairFieldTaskBindings b ON b.TaskId=s.TaskId
                        WHERE s.Id=o.OriginalCrewFirstStartId AND b.ObligationId=o.Id AND b.ProjectId=o.ProjectId
                            AND b.DefectId=o.DefectId AND b.AssignmentId=s.AssignmentId AND b.CrewId=s.OriginalActorId))
                    THROW 51311, 'Original Crew first-start must retain the actual task and obligation scope.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER [TR_FieldInspectionTasks_H4RepairPin] ON [FieldInspectionTasks] AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                    WHERE EXISTS (SELECT i.RepairItemId EXCEPT SELECT d.RepairItemId))
                    THROW 51321, 'Native repair task item pin is immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted t WHERE t.TaskMode IN('NORMAL','CONDITIONAL_FT') AND NOT EXISTS
                    (SELECT 1 FROM RepairItems i JOIN RepairObligations o ON o.Id=i.ObligationId
                        JOIN RepairActualScopes s ON s.ObligationId=o.Id
                        JOIN RoadSectionVersions v ON v.Id=t.RoadSectionVersionId
                        WHERE i.Id=t.RepairItemId AND i.ProjectId=t.ProjectId AND i.DefectId=t.DefectId
                            AND s.PhysicalRoadId=v.RoadSectionId AND t.LifecycleVersion=2 AND t.Purpose=4
                            AND ((t.TaskMode='NORMAL' AND i.Mode=1) OR (t.TaskMode='CONDITIONAL_FT' AND i.Mode=2))
                            AND (EXISTS (SELECT 1 FROM deleted old WHERE old.Id=t.Id) OR
                                (i.State=3 AND i.CrewId IS NOT NULL AND i.AssignedBy=t.AssignedByUserId
                                AND i.RepairPlan IS NOT NULL AND i.ChecklistVersion IS NOT NULL
                                AND i.ProposalPlanHash IS NOT NULL AND (i.Mode=2 OR i.ApprovedPlanHash=i.ProposalPlanHash)))))
                    THROW 51321, 'Native repair task must retain the actual assigned item, plan and physical scope.', 1;
            END
            """);
    }

    private static void ReplaceNativeTaskModeGuard(MigrationBuilder migrationBuilder, bool forward)
    {
        const string prior = "i.[TaskMode]<>'MEASURE_ONLY'";
        const string next = "i.[TaskMode] NOT IN ('MEASURE_ONLY','NORMAL','CONDITIONAL_FT')";
        var source = forward ? prior : next; var target = forward ? next : prior;
        // Forward correction of the actual installed H3 guard. Preserve every other
        // lineage/geometry/legacy predicate; refuse unexpected installed definitions.
        migrationBuilder.Sql($"""
            DECLARE @nativeTaskGuard nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID(N'[dbo].[TR_FieldInspectionTasks_H3Scope]'));
            IF @nativeTaskGuard IS NULL OR CHARINDEX(N'{source.Replace("'", "''", StringComparison.Ordinal)}',
                @nativeTaskGuard COLLATE Latin1_General_100_BIN2)=0
                THROW 51395, 'Installed FIELD task guard does not match the supported source version.', 1;
            SET @nativeTaskGuard=REPLACE(@nativeTaskGuard,N'{source.Replace("'", "''", StringComparison.Ordinal)}',N'{target.Replace("'", "''", StringComparison.Ordinal)}');
            SET @nativeTaskGuard=REPLACE(@nativeTaskGuard,N'CREATE OR ALTER TRIGGER',N'ALTER TRIGGER');
            IF LEFT(@nativeTaskGuard,6)=N'CREATE' SET @nativeTaskGuard=STUFF(@nativeTaskGuard,1,6,N'ALTER');
            EXEC sys.sp_executesql @nativeTaskGuard;
            """);
    }

    private static void ReplaceNativeSessionPurposeGuard(MigrationBuilder migrationBuilder, bool forward)
    {
        const string prior = "t.[Purpose]<>s.[Purpose]";
        const string next = """
            (t.[Purpose]<>s.[Purpose] AND NOT (s.[Purpose]=3 AND t.[Purpose]=4
                AND t.[TaskMode] IN('NORMAL','CONDITIONAL_FT') AND t.[RepairItemId] IS NOT NULL
                AND (EXISTS (SELECT 1 FROM deleted priorSession WHERE priorSession.Id=s.Id) OR t.[Status] IN(2,4))
                AND EXISTS (SELECT 1 FROM RepairItems item JOIN RepairFieldTaskBindings binding ON binding.Id=item.CurrentBindingId
                    JOIN FieldInspectionAssignments assignment ON assignment.Id=binding.AssignmentId
                    WHERE item.Id=t.[RepairItemId] AND binding.ItemId=item.Id AND binding.ProjectId=t.[ProjectId]
                        AND binding.DefectId=t.[DefectId] AND binding.TaskId=t.Id AND binding.CrewId=s.[InspectorUserId]
                        AND assignment.FieldInspectionTaskId=t.Id AND assignment.AssignedToUserId=binding.CrewId
                        AND (EXISTS (SELECT 1 FROM deleted priorSession WHERE priorSession.Id=s.Id)
                            OR (assignment.Status=1 AND assignment.EndedAt IS NULL)))))
            """;
        var source = forward ? prior : next; var target = forward ? next : prior;
        migrationBuilder.Sql($"""
            DECLARE @nativeSessionGuard nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID(N'[dbo].[TR_FieldInspectionSessions_Integrity]'));
            IF @nativeSessionGuard IS NULL OR CHARINDEX(N'{source.Replace("'", "''", StringComparison.Ordinal)}',
                @nativeSessionGuard COLLATE Latin1_General_100_BIN2)=0
                THROW 51395, 'Installed FIELD session guard does not match the supported source version.', 1;
            SET @nativeSessionGuard=REPLACE(@nativeSessionGuard,N'{source.Replace("'", "''", StringComparison.Ordinal)}',N'{target.Replace("'", "''", StringComparison.Ordinal)}');
            SET @nativeSessionGuard=REPLACE(@nativeSessionGuard,N'CREATE OR ALTER TRIGGER',N'ALTER TRIGGER');
            IF LEFT(@nativeSessionGuard,6)=N'CREATE' SET @nativeSessionGuard=STUFF(@nativeSessionGuard,1,6,N'ALTER');
            EXEC sys.sp_executesql @nativeSessionGuard;
            """);
    }

    private static void RefuseAndRemoveRepairProducerGuards(MigrationBuilder migrationBuilder)
    {
        foreach (var table in ProducerHistoryTables.Append("RepairSafetyMonitoring"))
            migrationBuilder.Sql($"IF EXISTS (SELECT 1 FROM [{table}]) THROW 51390, 'Cannot downgrade populated repair producer facts.', 1;");
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM FieldInspectionTasks WHERE RepairItemId IS NOT NULL OR TaskMode IN('NORMAL','CONDITIONAL_FT'))
                OR EXISTS (SELECT 1 FROM RepairObligations WHERE CurrentRepairItemId IS NOT NULL OR OriginalCrewFirstStartId IS NOT NULL)
                OR EXISTS (SELECT 1 FROM RepairPackages WHERE DefectStatusAtAnchor IS NOT NULL)
                OR EXISTS (SELECT 1 FROM RepairDecisions WHERE PreviousObligationHeadDecisionId IS NOT NULL)
                OR EXISTS (SELECT 1 FROM RepairObligationResolutionEvents WHERE PreviousHeadDecisionId IS NOT NULL)
                OR EXISTS (SELECT 1 FROM RepairTemporarySafetyMeasures WHERE CurrentResponsibilityTransferId IS NOT NULL)
                OR EXISTS (SELECT 1 FROM RepairItems WHERE CurrentBindingId IS NOT NULL OR CurrentAssessmentId IS NOT NULL
                    OR CurrentExecutionStartId IS NOT NULL OR CurrentExecutionFinishId IS NOT NULL OR CurrentAttemptId IS NOT NULL
                    OR CurrentIntakeLinkId IS NOT NULL OR EffectiveIntakeSubmissionId IS NOT NULL OR CurrentReviewId IS NOT NULL
                    OR PredecessorItemId IS NOT NULL OR SupersededByItemId IS NOT NULL)
                THROW 51390, 'Cannot downgrade populated repair producer pins.', 1;
            """);
        foreach (var table in ProducerHistoryTables)
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS [TR_{table}_Immutable];");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_RepairSafetyMonitoring_Scope];");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_RepairObligations_ProducerHeads];");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_FieldInspectionTasks_H4RepairPin];");
        ReplaceNativeTaskModeGuard(migrationBuilder, forward: false);
        ReplaceNativeSessionPurposeGuard(migrationBuilder, forward: false);
    }
}
