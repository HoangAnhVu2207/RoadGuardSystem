using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class OwnerRoadCoverageActivation
{
    private static void InstallRoadCoverageGuards(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_RoadCoverageMappings_Immutable ON RoadCoverageMappings AFTER UPDATE,DELETE AS
            BEGIN SET NOCOUNT ON;
              IF EXISTS(SELECT 1 FROM deleted) THROW 51700, 'Road coverage confirmations are append-only.', 1;
            END
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_RoadCoverageMappings_Scope ON RoadCoverageMappings AFTER INSERT AS
            BEGIN SET NOCOUNT ON;
              IF EXISTS(SELECT 1 FROM inserted i WHERE LEN(i.SourceVersion)<>64 OR LEN(i.HandoverVersion)<>64
                OR NOT EXISTS(SELECT 1 FROM RepairObligations o JOIN RepairActualScopes s ON s.ObligationId=o.Id
                  LEFT JOIN ObligationResponsibilities r ON r.ObligationId=o.Id
                  WHERE o.Id=i.ScopeObligationId AND COALESCE(r.CurrentProjectId,o.ProjectId)=i.ProjectId
                    AND s.PhysicalRoadId=i.RoadSectionId AND s.LocationVersion=i.LocationVersion
                    AND s.[From]=i.[From] AND s.[To]=i.[To] AND s.OffsetFrom=i.OffsetFrom AND s.OffsetTo=i.OffsetTo)
                OR NOT EXISTS(SELECT 1 FROM HandoverDocuments h WHERE h.Id=i.HandoverDocumentId
                  AND h.ProjectId=i.ProjectId AND h.FileId=i.HandoverFileId AND h.AcceptedByUserId IS NOT NULL)
                OR (i.SourceKind='WARRANTY' AND NOT EXISTS(SELECT 1 FROM Warranties w WHERE w.Id=i.SourceId
                  AND w.ProjectId=i.ProjectId AND w.SourceDocumentId=i.CoverageFileId
                  AND (w.RoadSectionId IS NULL OR w.RoadSectionId=i.RoadSectionId)
                  AND (w.HandoverDocumentId IS NULL OR w.HandoverDocumentId=i.HandoverDocumentId)))
                OR (i.SourceKind='MAINTENANCE_BASIS' AND i.SourceId<>i.CoverageFileId)
                OR NOT EXISTS(SELECT 1 FROM FileScopes f WHERE f.FileId=i.HandoverFileId AND f.ProjectId=i.ProjectId)
                OR NOT EXISTS(SELECT 1 FROM FileScopes f WHERE f.FileId=i.CoverageFileId AND f.ProjectId=i.ProjectId)
                OR (i.Provenance='REAL_SOURCE' AND EXISTS(SELECT 1 FROM Files f JOIN FileScopes s ON s.FileId=f.Id
                  WHERE f.Id IN (i.HandoverFileId,i.CoverageFileId) AND s.ProjectId=i.ProjectId
                    AND (s.Purpose LIKE 'TEST_ONLY%' OR f.StorageUri LIKE 'TEST_ONLY/%')))
                OR (i.SupersedesId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM RoadCoverageMappings p
                  WHERE p.Id=i.SupersedesId AND p.ProjectId=i.ProjectId AND p.ScopeObligationId=i.ScopeObligationId
                    AND p.Provenance=i.Provenance AND p.ConfirmedAtUtc<=i.ConfirmedAtUtc))
                OR (i.Provenance='REAL_SOURCE' AND EXISTS(SELECT 1 FROM RoadCoverageMappings p
                  WHERE p.Id<>i.Id AND p.ProjectId=i.ProjectId AND p.Provenance='TEST_ONLY'
                    AND ((p.SourceKind=i.SourceKind AND p.SourceId=i.SourceId) OR p.HandoverDocumentId=i.HandoverDocumentId))))
                THROW 51701, 'Coverage requires its exact scoped sources and immutable provenance.', 1;
            END
            """);
    }
}
