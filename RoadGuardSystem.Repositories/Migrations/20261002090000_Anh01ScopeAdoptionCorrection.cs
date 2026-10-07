using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.Repositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261002090000_Anh01ScopeAdoptionCorrection")]
public sealed class Anh01ScopeAdoptionCorrection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The prior migration has already run in isolated fixtures. Preserve its history,
        // audit both representations and remove unsafe adoption without rewriting source JSON.
        foreach (var kind in new[] { "Plan", "Request" })
        {
            var snapshot = kind == "Plan" ? "r.OutputRequirements"
                : "JSON_QUERY(CASE WHEN ISJSON(r.OutputRequirements)=1 THEN r.OutputRequirements ELSE N'{}' END,'$.scope')";
            migrationBuilder.Sql($$"""
                ;WITH Bodies AS (
                  SELECT r.Id, CASE WHEN ISJSON(b.body)=1 AND LEFT(LTRIM(b.body),1)='[' THEN b.body ELSE N'[]' END body
                  FROM Survey{{kind}}s r CROSS APPLY (SELECT {{snapshot}} body) b
                ), Snapshots AS (
                  SELECT b.Id, a.[key] entryKey, TRY_CONVERT(uniqueidentifier,j.routeVersionId) routeId,
                    TRY_CONVERT(uniqueidentifier,j.segmentSetId) setId,j.targetBand band,
                    CASE WHEN LEFT(LTRIM(j.segmentIds),1)='[' THEN j.segmentIds ELSE N'[]' END ids,
                    CASE WHEN a.[type]=5
                      AND (SELECT COUNT(*) FROM OPENJSON(obj.body))=4
                      AND (SELECT COUNT(*) FROM OPENJSON(obj.body) WHERE [key]='routeVersionId' AND [type]=1)=1
                      AND (SELECT COUNT(*) FROM OPENJSON(obj.body) WHERE [key]='segmentSetId' AND [type]=1)=1
                      AND (SELECT COUNT(*) FROM OPENJSON(obj.body) WHERE [key]='targetBand' AND [type]=1)=1
                      AND (SELECT COUNT(*) FROM OPENJSON(obj.body) WHERE [key]='segmentIds' AND [type]=4)=1
                      AND TRY_CONVERT(uniqueidentifier,j.routeVersionId) IS NOT NULL
                      AND TRY_CONVERT(uniqueidentifier,j.segmentSetId) IS NOT NULL
                      AND DATALENGTH(j.routeVersionId)=72 AND DATALENGTH(j.segmentSetId)=72
                      AND j.targetBand COLLATE Latin1_General_100_BIN2 IN ('SURFACE','LEFT_EDGE','RIGHT_EDGE')
                      AND DATALENGTH(j.targetBand)=2*LEN(j.targetBand)
                    THEN 1 ELSE 0 END validShape
                  FROM Bodies b CROSS APPLY OPENJSON(b.body) a
                  CROSS APPLY (SELECT CASE WHEN a.[type]=5 THEN a.value ELSE N'{}' END body) obj
                  CROSS APPLY OPENJSON(obj.body) WITH (
                    routeVersionId nvarchar(max), segmentSetId nvarchar(max), targetBand nvarchar(max),segmentIds nvarchar(max) AS JSON) j
                ), Relations AS (
                  SELECT s.Survey{{kind}}Id Id,s.Id scopeId,s.RouteSectionVersionId routeId,s.SegmentSetId setId,s.TargetBand band,
                    CASE WHEN ISJSON(s.SegmentIdsJson)=1 AND LEFT(LTRIM(s.SegmentIdsJson),1)='[' THEN s.SegmentIdsJson ELSE N'[]' END ids
                  FROM Survey{{kind}}Scopes s
                ), Matches AS (
                  SELECT s.Id,s.entryKey,r.scopeId FROM Snapshots s JOIN Relations r ON r.Id=s.Id
                    AND r.routeId=s.routeId AND r.setId=s.setId AND r.band COLLATE Latin1_General_100_BIN2=s.band COLLATE Latin1_General_100_BIN2
                  WHERE NOT EXISTS (SELECT TRY_CONVERT(uniqueidentifier,value) FROM OPENJSON(s.ids)
                    EXCEPT SELECT TRY_CONVERT(uniqueidentifier,value) FROM OPENJSON(r.ids))
                    AND NOT EXISTS (SELECT TRY_CONVERT(uniqueidentifier,value) FROM OPENJSON(r.ids)
                    EXCEPT SELECT TRY_CONVERT(uniqueidentifier,value) FROM OPENJSON(s.ids))
                ), ValidOwners AS (
                  SELECT b.Id FROM Bodies b
                  WHERE EXISTS (SELECT 1 FROM Relations r WHERE r.Id=b.Id)
                    AND (SELECT COUNT(*) FROM Snapshots s WHERE s.Id=b.Id)=(SELECT COUNT(*) FROM Relations r WHERE r.Id=b.Id)
                    AND NOT EXISTS (SELECT 1 FROM Snapshots s WHERE s.Id=b.Id AND (
                      s.validShape=0 OR NOT EXISTS (SELECT 1 FROM OPENJSON(s.ids))
                      OR EXISTS (SELECT 1 FROM OPENJSON(s.ids) WHERE [type]<>1 OR DATALENGTH(value)<>72 OR TRY_CONVERT(uniqueidentifier,value) IS NULL)
                      OR EXISTS (SELECT 1 FROM OPENJSON(s.ids) GROUP BY TRY_CONVERT(uniqueidentifier,value) HAVING COUNT(*)>1)))
                    AND NOT EXISTS (SELECT 1 FROM Snapshots s WHERE s.Id=b.Id GROUP BY s.routeId,s.setId,s.band COLLATE Latin1_General_100_BIN2 HAVING COUNT(*)>1)
                    AND NOT EXISTS (SELECT 1 FROM Relations r WHERE r.Id=b.Id AND (
                      r.band COLLATE Latin1_General_100_BIN2 NOT IN ('SURFACE','LEFT_EDGE','RIGHT_EDGE')
                      OR DATALENGTH(r.band)<>2*LEN(r.band)
                      OR NOT EXISTS (SELECT 1 FROM OPENJSON(r.ids))
                      OR EXISTS (SELECT 1 FROM OPENJSON(r.ids) WHERE [type]<>1 OR DATALENGTH(value)<>72 OR TRY_CONVERT(uniqueidentifier,value) IS NULL)
                      OR EXISTS (SELECT 1 FROM OPENJSON(r.ids) GROUP BY TRY_CONVERT(uniqueidentifier,value) HAVING COUNT(*)>1)
                      OR NOT EXISTS (SELECT 1 FROM RoadSegmentSets ss JOIN RoadSectionVersions v ON v.Id=ss.RoadSectionVersionId
                        JOIN RoadSections road ON road.Id=v.RoadSectionId JOIN Survey{{kind}}s owner ON owner.Id=b.Id
                        WHERE ss.Id=r.setId AND v.Id=r.routeId AND road.ProjectId=owner.ProjectId)
                      OR EXISTS (SELECT 1 FROM OPENJSON(r.ids) i WHERE NOT EXISTS (SELECT 1 FROM RoadSegments segment
                        WHERE segment.Id=TRY_CONVERT(uniqueidentifier,i.value) AND segment.SegmentSetId=r.setId AND segment.RoadSectionVersionId=r.routeId))))
                    AND NOT EXISTS (SELECT 1 FROM Snapshots s WHERE s.Id=b.Id AND NOT EXISTS (SELECT 1 FROM Matches m WHERE m.Id=s.Id AND m.entryKey=s.entryKey))
                    AND NOT EXISTS (SELECT 1 FROM Relations r WHERE r.Id=b.Id AND NOT EXISTS (SELECT 1 FROM Matches m WHERE m.Id=r.Id AND m.scopeId=r.scopeId))
                )
                UPDATE r SET ScopeFormatVersion=CASE WHEN v.Id IS NOT NULL THEN 'BAND_V1' ELSE NULL END
                FROM Survey{{kind}}s r LEFT JOIN ValidOwners v ON v.Id=r.Id
                WHERE (r.ScopeFormatVersion IS NULL AND v.Id IS NOT NULL)
                  OR (r.ScopeFormatVersion='BAND_V1' AND v.Id IS NULL);
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Re-adopting ambiguous rows would reopen writes on incompatible legacy data.
        // This data-only correction is forward-only; an owner-reviewed recovery is required.
        migrationBuilder.Sql("THROW 51026, 'Scope adoption correction cannot be undone without an owner-reviewed data preservation plan.', 1;");
    }
}
