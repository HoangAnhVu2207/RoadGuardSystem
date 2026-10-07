using Microsoft.EntityFrameworkCore.Migrations;
namespace RoadGuardSystem.cRepositories.Migrations;

public partial class OwnerDefectStatisticsActivation
{
    private static void InstallStatisticsGuards(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_DefectStatisticsSources_Immutable ON DefectStatisticsSources AFTER UPDATE,DELETE AS
            BEGIN SET NOCOUNT ON;
              IF EXISTS(SELECT 1 FROM deleted) THROW 51800, 'Statistics source confirmations are append-only.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_DefectStatisticsSources_Scope ON DefectStatisticsSources AFTER INSERT AS
            BEGIN SET NOCOUNT ON;
              IF EXISTS(SELECT 1 FROM inserted i WHERE
                NOT EXISTS(SELECT 1 FROM RepairObligations o JOIN RepairActualScopes s ON s.ObligationId=o.Id
                  JOIN Defects d ON d.Id=o.DefectId LEFT JOIN ObligationResponsibilities r ON r.ObligationId=o.Id
                  WHERE o.Id=i.ObligationId AND d.Id=i.DefectId AND d.ProjectId=i.ProjectId AND o.ProjectId=i.ProjectId
                    AND COALESCE(r.CurrentProjectId,o.ProjectId)=i.ProjectId AND d.RoadSectionVersionId=i.RouteVersionId
                    AND s.PhysicalRoadId=i.RoadSectionId AND s.LocationVersion=i.LocationVersion
                    AND s.[From]=i.[From] AND s.[To]=i.[To] AND s.OffsetFrom=i.OffsetFrom AND s.OffsetTo=i.OffsetTo)
                OR NOT EXISTS(SELECT 1 FROM OPENJSON(i.SegmentIdsJson))
                OR EXISTS(SELECT 1 FROM OPENJSON(i.SegmentIdsJson) j WHERE NOT EXISTS(SELECT 1 FROM RoadSegments s
                  JOIN RoadSegmentSets ss ON ss.Id=s.SegmentSetId WHERE s.Id=TRY_CONVERT(uniqueidentifier,j.[value])
                    AND s.RoadSectionVersionId=i.RouteVersionId AND ss.RoadSectionVersionId=i.RouteVersionId AND ss.Status='PUBLISHED'))
                OR EXISTS(SELECT 1 FROM DefectStatisticsSources p WHERE p.Id<>i.Id AND p.SharedPartId=i.SharedPartId
                  AND (p.ProjectId<>i.ProjectId OR p.RoadSectionId<>i.RoadSectionId OR p.LocationVersion<>i.LocationVersion
                    OR p.[From]<>i.[From] OR p.[To]<>i.[To] OR p.OffsetFrom<>i.OffsetFrom OR p.OffsetTo<>i.OffsetTo OR p.Provenance<>i.Provenance))
                OR (i.SupersedesId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM DefectStatisticsSources p WHERE p.Id=i.SupersedesId
                  AND p.ProjectId=i.ProjectId AND p.ObligationId=i.ObligationId AND p.SharedPartId=i.SharedPartId AND p.Provenance=i.Provenance))
                OR EXISTS(SELECT 1 FROM OPENJSON(i.QuantitiesJson) WITH(measurementId uniqueidentifier,segmentId uniqueidentifier) q
                  WHERE NOT EXISTS(SELECT 1 FROM GroundTruthMeasurements m WHERE m.Id=q.measurementId AND m.RoadSectionVersionId=i.RouteVersionId
                    AND (m.DefectId=i.DefectId OR EXISTS(SELECT 1 FROM DefectStatisticsSources p WHERE p.ProjectId=i.ProjectId
                      AND p.SharedPartId=i.SharedPartId AND p.DefectId=m.DefectId)))
                    OR (q.segmentId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM OPENJSON(i.SegmentIdsJson) j WHERE TRY_CONVERT(uniqueidentifier,j.[value])=q.segmentId)))
                OR (i.Provenance='REAL_SOURCE' AND EXISTS(SELECT 1 FROM OPENJSON(i.QuantitiesJson) WITH(measurementId uniqueidentifier) q
                  JOIN GroundTruthMeasurements m ON m.Id=q.measurementId WHERE m.Notes LIKE 'TEST_ONLY%')))
                THROW 51801, 'Statistics require exact current scope, segments, measurement sources and immutable identity.', 1;
            END
            """);
    }
}
