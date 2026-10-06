using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class H3FieldLifecycleAndIntake
{
    private static void RefuseLossAndRemoveGuards(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM [FieldInspectionTasks] WHERE [LifecycleVersion]<>1 OR [SurveyId] IS NULL OR [Status]=8
                OR [Purpose]<>1 OR [SourceKind]<>'SURVEY' OR [TaskMode]<>'MEASURE_ONLY'
                OR [SegmentSetId] IS NOT NULL OR [LayoutRevisionId] IS NOT NULL OR [MapPublicationId] IS NOT NULL
                OR [CrsProfileRevisionId] IS NOT NULL OR [SlabId] IS NOT NULL)
                OR EXISTS (SELECT 1 FROM [FieldInspectionSessions] WHERE [Purpose] IN (3,4,5))
                OR EXISTS (SELECT 1 FROM [GroundTruthMeasurements] WHERE [Value] IS NULL OR [Location] IS NULL
                    OR [ValueState]<>'KNOWN' OR [LocationState]<>'CAPTURED' OR [Dimension]<>'LENGTH' OR [MeasurementType]=4
                    OR [UnknownReason] IS NOT NULL OR [LocationReason] IS NOT NULL)
                THROW 51134, 'Cannot downgrade populated FIELD lifecycle or unknown measurement facts.', 1;
            """);
        foreach (var table in HistoryTables)
            migrationBuilder.Sql($"IF EXISTS (SELECT 1 FROM [{table}]) THROW 51135, 'Cannot downgrade immutable FIELD history.', 1;");
        foreach (var table in HistoryTables)
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS [TR_{table}_Scope];");
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS [TR_{table}_Immutable];");
        }
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_FieldInspectionTasks_H3Scope];");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_FieldInspectionSessions_Integrity];");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_GroundTruthMeasurements_Integrity];");
    }

    private static void InstallFieldScopeGuards(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER [TR_FieldInspectionTasks_H3Scope] ON [dbo].[FieldInspectionTasks] AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.[Id]=i.[Id]
                    WHERE EXISTS (SELECT i.[ProjectId],i.[DefectId],i.[SurveyId],i.[LifecycleVersion],i.[SourceKind],i.[TaskMode],i.[Purpose],
                        i.[RoadSectionVersionId],i.[SegmentSetId],i.[LayoutRevisionId],i.[MapPublicationId],i.[CrsProfileRevisionId],i.[SlabId],
                        i.[RequiredMeasurementType],i.[MeasurementScope]
                        EXCEPT SELECT d.[ProjectId],d.[DefectId],d.[SurveyId],d.[LifecycleVersion],d.[SourceKind],d.[TaskMode],d.[Purpose],
                        d.[RoadSectionVersionId],d.[SegmentSetId],d.[LayoutRevisionId],d.[MapPublicationId],d.[CrsProfileRevisionId],d.[SlabId],
                        d.[RequiredMeasurementType],d.[MeasurementScope]))
                    THROW 51132, 'FIELD task source, mode and location pins are write-once.', 1;
                IF EXISTS (SELECT 1 FROM inserted i
                    LEFT JOIN [Defects] d ON d.[Id]=i.[DefectId]
                    LEFT JOIN [RoadSectionVersions] v ON v.[Id]=i.[RoadSectionVersionId]
                    LEFT JOIN [RoadSections] r ON r.[Id]=v.[RoadSectionId]
                    WHERE i.[LifecycleVersion]=2 AND
                        (i.[TaskMode]<>'MEASURE_ONLY' OR i.[RequiredMeasurementType] NOT IN (1,2,3,4)
                        OR d.[Id] IS NULL OR d.[ProjectId]<>i.[ProjectId] OR d.[RoadSectionVersionId]<>i.[RoadSectionVersionId]
                        OR r.[ProjectId]<>i.[ProjectId]
                        OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=i.[Id]) AND d.[Status]<>1)
                        OR (i.[SegmentSetId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [RoadSegmentSets] s
                            WHERE s.[Id]=i.[SegmentSetId] AND s.[RoadSectionVersionId]=i.[RoadSectionVersionId]))
                        OR (i.[LayoutRevisionId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [PavementLayoutRevisions] l
                            WHERE l.[Id]=i.[LayoutRevisionId] AND l.[ProjectId]=i.[ProjectId]
                              AND l.[RouteVersionId]=i.[RoadSectionVersionId] AND l.[SegmentSetId]=i.[SegmentSetId]))
                        OR (i.[MapPublicationId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [GeometryMapPublications] m
                            WHERE m.[Id]=i.[MapPublicationId] AND m.[ProjectId]=i.[ProjectId]
                              AND m.[RouteVersionId]=i.[RoadSectionVersionId] AND m.[SegmentSetId]=i.[SegmentSetId]
                              AND m.[LayoutRevisionId]=i.[LayoutRevisionId]
                              AND NOT EXISTS (SELECT m.[CrsProfileRevisionId] EXCEPT SELECT i.[CrsProfileRevisionId])))
                        OR EXISTS (SELECT v.[CrsProfileRevisionId] EXCEPT SELECT i.[CrsProfileRevisionId])
                        OR (i.[SlabId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [PavementLayoutRevisions] layout
                            CROSS APPLY OPENJSON(layout.[SnapshotJson],'$.slabs') WITH ([Key] nvarchar(160) '$.key') slab
                            WHERE layout.[Id]=i.[LayoutRevisionId] AND slab.[Key]=i.[SlabId]))
                        OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=i.[Id]) AND i.[SourceKind]='REPORTER'
                            AND NOT EXISTS (SELECT 1 FROM [DefectSourceLinks] link JOIN [SourceDecisions] decision ON decision.[Id]=link.[DecisionId]
                                WHERE link.[DefectId]=i.[DefectId] AND link.[ProjectId]=i.[ProjectId]
                                  AND link.[SourceKind]=1 AND link.[EndedAt] IS NULL AND decision.[Decision]=1))
                        OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=i.[Id]) AND i.[SourceKind]='SURVEY'
                            AND (NOT EXISTS (SELECT 1 FROM [AIDetections] a JOIN [ProcessingJobs] j ON j.[Id]=a.[ProcessingJobId]
                                    JOIN [ProcessingBlocks] b ON b.[Id]=j.[ProcessingBlockId]
                                    JOIN [SurveyDataVersions] data ON data.[Id]=b.[SurveyDataVersionId]
                                    WHERE a.[Id]=d.[SourceAIDetectionId] AND data.[SurveyId]=i.[SurveyId])
                                OR NOT EXISTS (SELECT 1 FROM [DefectSourceLinks] link JOIN [SourceDecisions] decision ON decision.[Id]=link.[DecisionId]
                                    WHERE link.[DefectId]=i.[DefectId] AND link.[ProjectId]=i.[ProjectId]
                                      AND link.[SourceKind]=2 AND link.[SourceId]=d.[SourceAIDetectionId]
                                      AND link.[EndedAt] IS NULL AND decision.[Decision]=1)))))
                    THROW 51133, 'FIELD task source or geometry scope is invalid.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_FieldInspectionSessions_Integrity] ON [dbo].[FieldInspectionSessions] AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted s LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=s.[FieldInspectionTaskId]
                    WHERE (s.[Purpose] IN (1,3,4,5) AND
                        (t.[Id] IS NULL OR t.[ProjectId]<>s.[ProjectId] OR t.[RoadSectionVersionId]<>s.[RoadSectionVersionId]
                         OR EXISTS (SELECT t.[SurveyId] EXCEPT SELECT s.[SurveyId])
                         OR (s.[Purpose]=1 AND (t.[LifecycleVersion]<>1 OR t.[Purpose]<>1))
                         OR (s.[Purpose] IN (3,4,5) AND (t.[LifecycleVersion]<>2 OR t.[Purpose]<>s.[Purpose]))
                         OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=s.[Id]) AND NOT EXISTS
                            (SELECT 1 FROM [FieldInspectionAssignments] a WHERE a.[FieldInspectionTaskId]=t.[Id]
                                AND a.[AssignedToUserId]=s.[InspectorUserId] AND a.[Status]=1 AND a.[EndedAt] IS NULL))))
                       OR (s.[Purpose]=2 AND s.[FieldInspectionTaskId] IS NOT NULL))
                    THROW 51040, 'Field inspection session scope or active assignment is invalid.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_GroundTruthMeasurements_Integrity] ON [dbo].[GroundTruthMeasurements] AFTER INSERT, UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted m JOIN [FieldInspectionSessions] s ON s.[Id]=m.[FieldInspectionSessionId]
                    LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=s.[FieldInspectionTaskId]
                    LEFT JOIN [Defects] d ON d.[Id]=m.[DefectId]
                    WHERE m.[RoadSectionVersionId]<>s.[RoadSectionVersionId]
                        OR (s.[Purpose] IN (1,3,4,5) AND
                            (m.[DefectId] IS NULL OR m.[DefectId]<>t.[DefectId] OR d.[ProjectId]<>s.[ProjectId]
                             OR d.[RoadSectionVersionId]<>s.[RoadSectionVersionId]
                             OR EXISTS (SELECT m.[SurveyId] EXCEPT SELECT s.[SurveyId])
                             OR (s.[Purpose]=1 AND (m.[SurveyId] IS NULL OR d.[Status]<>1))))
                        OR (s.[Purpose]=2 AND (m.[DefectId] IS NOT NULL OR m.[SurveyId] IS NOT NULL)))
                    THROW 51042, 'Ground truth measurement purpose or scope is invalid.', 1;
            END
            """);
    }

    private static readonly string[] HistoryTables =
    [
        "FieldInspectionOperationOrigins", "FieldTaskStartOrigins", "FieldInspectionSubmissions",
        "FieldInspectionReviews", "FieldInspectionEvidenceLinks", "FieldInspectionEvidenceReuseDecisions",
        "FieldInspectionTaskEvents", "FieldInspectionLocationProofs"
    ];

    private static void InstallHistoryGuards(MigrationBuilder migrationBuilder)
    {
        foreach (var table in HistoryTables)
        {
            migrationBuilder.Sql($"""
                CREATE TRIGGER [TR_{table}_Immutable] ON [dbo].[{table}]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            var extra = table switch
            {
                "FieldInspectionOperationOrigins" => """
                    OR i.[Id]<>i.[EffectId] OR i.[SchemaVersion]<>1
                    OR i.[Kind] NOT IN ('FIELD_START','FIELD_SUBMISSION')
                    OR LEN(i.[ContentHash])<>64
                    OR i.[ContentHash] COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%'
                    """,
                "FieldTaskStartOrigins" => """
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionAssignments] a
                        WHERE a.[Id]=i.[AssignmentId] AND a.[FieldInspectionTaskId]=i.[TaskId]
                          AND a.[AssignedToUserId]=i.[OriginalActorId])
                    OR EXISTS (SELECT i.[RouteVersionId],i.[SegmentSetId],i.[LayoutRevisionId],i.[MapPublicationId],i.[CrsProfileRevisionId],i.[SlabId]
                        EXCEPT SELECT t.[RoadSectionVersionId],t.[SegmentSetId],t.[LayoutRevisionId],t.[MapPublicationId],t.[CrsProfileRevisionId],t.[SlabId])
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionOperationOrigins] o
                        WHERE o.[Id]=i.[OperationOriginId] AND o.[EffectId]=i.[Id]
                          AND o.[ProjectId]=i.[ProjectId] AND o.[TaskId]=i.[TaskId]
                          AND o.[OriginId]=i.[OriginId] AND o.[Kind]='FIELD_START'
                          AND o.[ContentHash]=i.[ContentHash] AND o.[OriginalActorId]=i.[OriginalActorId]
                          AND NOT EXISTS (SELECT o.[DeviceId],o.[ServerReceivedAt] EXCEPT SELECT i.[DeviceId],i.[ServerReceivedAt]))
                    OR ISJSON(i.[ClaimEvidenceJson])<>1 OR LEFT(LTRIM(i.[ClaimEvidenceJson]),1)<>'{'
                    OR i.[OperationKind]<>'FIELD_START'
                    OR i.[TimeProvenance] NOT IN ('SERVER_ONLINE','CLAIMED_OFFLINE')
                    OR (i.[TimeProvenance]='SERVER_ONLINE' AND (i.[VerifiedOriginalAt] IS NULL OR i.[VerifiedOriginalAt]<>i.[ServerReceivedAt]))
                    OR (i.[TimeProvenance]='CLAIMED_OFFLINE' AND i.[VerifiedOriginalAt] IS NOT NULL)
                    """,
                "FieldInspectionSubmissions" => """
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionAssignments] a
                        WHERE a.[Id]=i.[AssignmentId] AND a.[FieldInspectionTaskId]=i.[TaskId]
                          AND a.[AssignedToUserId]=i.[OriginalActorId])
                    OR NOT EXISTS (SELECT 1 FROM [FieldTaskStartOrigins] s
                        WHERE s.[Id]=i.[StartOriginId] AND s.[TaskId]=i.[TaskId] AND s.[ProjectId]=i.[ProjectId])
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionSessions] s
                        WHERE s.[Id]=i.[SessionId] AND s.[FieldInspectionTaskId]=i.[TaskId]
                          AND s.[ProjectId]=i.[ProjectId] AND s.[RoadSectionVersionId]=t.[RoadSectionVersionId]
                          AND s.[InspectorUserId]=i.[OriginalActorId] AND s.[Purpose]=t.[Purpose])
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionOperationOrigins] o
                        WHERE o.[Id]=i.[OperationOriginId] AND o.[EffectId]=i.[Id]
                          AND o.[ProjectId]=i.[ProjectId] AND o.[TaskId]=i.[TaskId]
                          AND o.[OriginId]=i.[OriginId] AND o.[Kind]='FIELD_SUBMISSION'
                          AND o.[ContentHash]=i.[ContentHash] AND o.[OriginalActorId]=i.[OriginalActorId]
                          AND o.[ServerReceivedAt]=i.[ServerReceivedAt])
                    OR NOT ((i.[Revision]=1 AND i.[ParentId] IS NULL AND i.[RootId]=i.[Id])
                        OR (i.[Revision]>1 AND EXISTS (SELECT 1 FROM [FieldInspectionSubmissions] p
                            WHERE p.[Id]=i.[ParentId] AND p.[TaskId]=i.[TaskId] AND p.[ProjectId]=i.[ProjectId]
                              AND p.[RootId]=i.[RootId] AND p.[Revision]+1=i.[Revision])))
                    OR ISJSON(i.[PayloadJson])<>1 OR LEFT(LTRIM(i.[PayloadJson]),1)<>'{'
                    OR ISJSON(i.[MissingReasonsJson])<>1 OR LEFT(LTRIM(i.[MissingReasonsJson]),1)<>'['
                    OR i.[Readiness] NOT IN ('READY','INCOMPLETE')
                    """,
                "FieldInspectionEvidenceLinks" => """
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionSubmissions] s
                        WHERE s.[Id]=i.[SubmissionId] AND s.[TaskId]=i.[TaskId] AND s.[ProjectId]=i.[ProjectId]
                          AND s.[AssignmentId]=i.[AssignmentId])
                    OR i.[Purpose] NOT IN ('BEFORE','AFTER','MEASUREMENT')
                    OR ISJSON(i.[CaptureFactsJson])<>1 OR LEFT(LTRIM(i.[CaptureFactsJson]),1)<>'{'
                    OR (i.[FileId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Files] f
                        WHERE f.[Id]=i.[FileId] AND f.[Checksum]=i.[DeclaredChecksum]
                          AND (EXISTS (SELECT 1 FROM [FileScopes] fs WHERE fs.[FileId]=f.[Id]
                              AND fs.[ProjectId]=i.[ProjectId] AND fs.[TargetId]=i.[TaskId] AND fs.[Purpose]=i.[Purpose])
                            OR (i.[Purpose]='BEFORE' AND EXISTS (SELECT 1 FROM [FieldInspectionEvidenceReuseDecisions] r
                                WHERE r.[TaskId]=i.[TaskId] AND r.[ProjectId]=i.[ProjectId]
                                  AND r.[FileId]=i.[FileId] AND r.[FileChecksum]=i.[DeclaredChecksum])))))
                    """,
                "FieldInspectionEvidenceReuseDecisions" => """
                    OR i.[SourceKind] NOT IN ('REPORTER','DRONE') OR LEN(LTRIM(RTRIM(i.[Reason])))=0
                    OR ISJSON(i.[ProvenanceJson])<>1
                    OR NOT EXISTS (SELECT 1 FROM [Files] f WHERE f.[Id]=i.[FileId] AND f.[Checksum]=i.[FileChecksum])
                    OR (i.[SourceKind]='REPORTER' AND NOT EXISTS
                        (SELECT 1 FROM [DefectSourceLinks] link JOIN [FileScopes] fs ON fs.[FileId]=i.[FileId]
                         WHERE link.[DefectId]=t.[DefectId] AND link.[ProjectId]=i.[ProjectId]
                           AND link.[SourceKind]=1 AND link.[EndedAt] IS NULL
                           AND fs.[ProjectId] IS NULL AND fs.[TargetId] IS NULL AND fs.[Purpose]='REPORT_PHOTO'
                           AND (EXISTS (SELECT 1 FROM [ReportOriginalEvidence] e WHERE e.[Id]=i.[SourceEvidenceId]
                                AND e.[ReportId]=link.[ReportSourceId] AND e.[FileId]=i.[FileId] AND e.[OwnerUserId]=fs.[OwnerUserId])
                            OR EXISTS (SELECT 1 FROM [ReportSupplementEvidence] e WHERE e.[Id]=i.[SourceEvidenceId]
                                AND e.[ReportId]=link.[ReportSourceId] AND e.[FileId]=i.[FileId] AND e.[OwnerUserId]=fs.[OwnerUserId]))))
                    OR (i.[SourceKind]='DRONE' AND NOT EXISTS
                        (SELECT 1 FROM [SurveyFiles] sf JOIN [FileScopes] fs ON fs.[FileId]=sf.[FileId]
                         WHERE sf.[Id]=i.[SourceEvidenceId] AND sf.[FileId]=i.[FileId] AND sf.[SurveyId]=t.[SurveyId]
                           AND fs.[ProjectId]=i.[ProjectId] AND fs.[Purpose] IN ('SURVEY_VIDEO','TELEMETRY')))
                    """,
                "FieldInspectionTaskEvents" => """
                    OR (i.[AssignmentId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [FieldInspectionAssignments] a
                        WHERE a.[Id]=i.[AssignmentId] AND a.[FieldInspectionTaskId]=i.[TaskId]))
                    OR ISJSON(i.[FactsJson])<>1 OR LEFT(LTRIM(i.[FactsJson]),1)<>'{'
                    OR (i.[LocationImpactId] IS NULL AND i.[LocationImpactDecisionId] IS NOT NULL)
                    OR (i.[LocationImpactId] IS NOT NULL AND (i.[LocationImpactDecisionId] IS NULL OR NOT EXISTS
                        (SELECT 1 FROM [GeometryLocationImpacts] impact JOIN [GeometryLocationImpactDecisions] decision
                            ON decision.[ImpactId]=impact.[Id]
                         WHERE impact.[Id]=i.[LocationImpactId] AND impact.[ProjectId]=i.[ProjectId]
                           AND impact.[PreviousRouteVersionId]=t.[RoadSectionVersionId]
                           AND decision.[Id]=i.[LocationImpactDecisionId] AND decision.[TaskId]=i.[TaskId]
                           AND decision.[ActorId]=i.[ActorId])))
                    """,
                "FieldInspectionReviews" => """
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionSubmissions] s
                        WHERE s.[Id]=i.[SubmissionId] AND s.[TaskId]=i.[TaskId] AND s.[ProjectId]=i.[ProjectId])
                    OR i.[Decision] NOT IN ('CONFIRM','NO_DEFECT','SUPPLEMENT') OR LEN(LTRIM(RTRIM(i.[Reason])))=0
                    OR (i.[Decision]='SUPPLEMENT' AND i.[ReceiptActivation]<>'AWAITING_OWNER_RECEIPT_PROTOCOL')
                    """,
                "FieldInspectionLocationProofs" => """
                    OR NOT EXISTS (SELECT 1 FROM [FieldInspectionSubmissions] s
                        WHERE s.[Id]=i.[SubmissionId] AND s.[TaskId]=i.[TaskId] AND s.[ProjectId]=i.[ProjectId])
                    OR i.[Kind] NOT IN ('GPS_CAPTURE','POSITION_CHECKLIST','UNKNOWN')
                    OR ISJSON(i.[FactsJson])<>1 OR i.[VerificationState]<>'CLAIMED'
                    """,
                _ => throw new System.InvalidOperationException("Unregistered FIELD history table.")
            };
            migrationBuilder.Sql($"""
                CREATE TRIGGER [TR_{table}_Scope] ON [dbo].[{table}] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=i.[TaskId]
                        WHERE t.[Id] IS NULL OR t.[ProjectId]<>i.[ProjectId] OR t.[LifecycleVersion]<>2 {extra})
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
        }
    }

    private static void RestoreLegacyIntegrity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER [TR_FieldInspectionSessions_Integrity]
            ON [dbo].[FieldInspectionSessions]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS
                (
                    SELECT 1
                    FROM inserted AS session
                    LEFT JOIN [dbo].[FieldInspectionTasks] AS task
                        ON task.[Id] = session.[FieldInspectionTaskId]
                    WHERE (session.[Purpose] = 1 AND
                           (task.[Id] IS NULL OR
                            task.[ProjectId] <> session.[ProjectId] OR
                            task.[RoadSectionVersionId] <> session.[RoadSectionVersionId] OR
                            task.[SurveyId] <> session.[SurveyId] OR
                            NOT EXISTS
                            (
                                SELECT 1
                                FROM [dbo].[FieldInspectionAssignments] AS assignment
                                WHERE assignment.[FieldInspectionTaskId] = task.[Id]
                                  AND assignment.[AssignedToUserId] = session.[InspectorUserId]
                                  AND assignment.[Status] = 1
                            )))
                       OR (session.[Purpose] = 2 AND session.[FieldInspectionTaskId] IS NOT NULL)
                )
                BEGIN
                    THROW 51040, 'Field inspection session scope or active assignment is invalid.', 1;
                END
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER [TR_GroundTruthMeasurements_Integrity]
            ON [dbo].[GroundTruthMeasurements]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS
                (
                    SELECT 1
                    FROM inserted AS measurement
                    INNER JOIN [dbo].[FieldInspectionSessions] AS session
                        ON session.[Id] = measurement.[FieldInspectionSessionId]
                    LEFT JOIN [dbo].[FieldInspectionTasks] AS task
                        ON task.[Id] = session.[FieldInspectionTaskId]
                    LEFT JOIN [dbo].[Defects] AS defect
                        ON defect.[Id] = measurement.[DefectId]
                    WHERE measurement.[RoadSectionVersionId] <> session.[RoadSectionVersionId]
                       OR (session.[Purpose] = 1 AND
                           (measurement.[DefectId] IS NULL OR
                            measurement.[SurveyId] IS NULL OR
                            measurement.[SurveyId] <> session.[SurveyId] OR
                            measurement.[DefectId] <> task.[DefectId] OR
                            defect.[ProjectId] <> session.[ProjectId] OR
                            defect.[RoadSectionVersionId] <> session.[RoadSectionVersionId] OR
                            defect.[Status] <> 1))
                       OR (session.[Purpose] = 2 AND
                           (measurement.[DefectId] IS NOT NULL OR measurement.[SurveyId] IS NOT NULL))
                )
                BEGIN
                    THROW 51042, 'Ground truth measurement purpose or scope is invalid.', 1;
                END
            END
            """);
    }
}
