using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261009043010_AllowVerifiedDefectPostRepairTasks")]
public partial class AllowVerifiedDefectPostRepairTasks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_FieldInspectionTasks_H3Scope] ON [dbo].[FieldInspectionTasks] AFTER INSERT, UPDATE AS
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
                                        (i.[TaskMode] NOT IN ('MEASURE_ONLY','NORMAL','CONDITIONAL_FT') OR i.[RequiredMeasurementType] NOT IN (1,2,3,4)
                                        OR d.[Id] IS NULL OR d.[ProjectId]<>i.[ProjectId] OR d.[RoadSectionVersionId]<>i.[RoadSectionVersionId]
                                        OR r.[ProjectId]<>i.[ProjectId]
                                        OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=i.[Id]) AND
                            (d.[Status] NOT IN (1,2) OR (d.[Status]=2 AND
                    (i.[Purpose]<>4 OR i.[TaskMode] NOT IN ('NORMAL','CONDITIONAL_FT') OR i.[RepairItemId] IS NULL))))
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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_FieldInspectionTasks_H3Scope] ON [dbo].[FieldInspectionTasks] AFTER INSERT, UPDATE AS
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
                                        (i.[TaskMode] NOT IN ('MEASURE_ONLY','NORMAL','CONDITIONAL_FT') OR i.[RequiredMeasurementType] NOT IN (1,2,3,4)
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
    }
}
