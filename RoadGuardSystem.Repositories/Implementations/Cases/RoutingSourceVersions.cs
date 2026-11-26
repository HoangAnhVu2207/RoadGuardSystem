using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

internal sealed class RoutingSourceVersions(RoadGuardDbContext db, IAnhHuyFactsRepository files)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, Json))).ToLowerInvariant();
    private static void Fail(int status, string code) => throw new CaseWorkflowException(status, code);
    internal async Task<string> RegistryVersionAsync(CancellationToken token)
    {
        // Hash SQL-side source snapshots; geometry bytes never travel in an unbounded application collection.
        // Table names and key names come exclusively from the model, not request input.
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "Projects", "RoadSections", "RoadGeometryMetadata", "NativeRouteVersionFacts", "CrsProfileRevisions",
            "RoadSegmentSets", "RoadCoverageMappings", "HandoverDocuments", "Warranties",
            "RepairObligations", "RepairActualScopes", "RepairPolicyRevisions",
            "RepairPolicyRevocations", "ObligationResponsibilities", "LD06LifecycleActions"
        };
        foreach (var entity in db.Model.GetEntityTypes().Where(e => names.Contains(e.GetTableName() ?? ""))
                     .DistinctBy(e => e.GetTableName()).OrderBy(e => e.GetTableName(), StringComparer.Ordinal))
        {
            var table = entity.GetTableName()!;
            var keys = string.Join(",", entity.FindPrimaryKey()!.Properties.Select(p => "[" + p.GetColumnName() + "]"));
            var sql = $"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT * FROM [{table}] ORDER BY {keys} FOR JSON PATH,INCLUDE_NULL_VALUES))),2) AS [Value]";
            var pin = await db.Database.SqlQueryRaw<string>(sql).SingleAsync(token);
            digest.AppendData(Encoding.UTF8.GetBytes(table + ":" + pin + ";"));
        }
        // The existing TR_RoadSectionVersions_Immutable forbids geometry/CRS/version edits and deletes.
        // Snapshot immutable identities + current flags rather than scanning/serializing every geometry.
        // Geometry bytes/hashes are read only for the bounded page; metadata hashes above remain pinned.
        var geometryPin = await db.Database.SqlQuery<string>($"""
            SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(
                SELECT [Id],[RoadSectionId],[IsCurrent],[VersionNo],[EffectiveFrom],[CrsProfileRevisionId]
                FROM [RoadSectionVersions] ORDER BY [Id] FOR JSON PATH,INCLUDE_NULL_VALUES))),2) AS [Value]
            """).SingleAsync(token);
        digest.AppendData(Encoding.UTF8.GetBytes(geometryPin ?? "EMPTY_GEOMETRY_REGISTRY"));
        // Defect lineage determines which immutable route version an obligation actually pins.
        // Hash scalar identity/version only; SQL geometry never enters FOR JSON or app memory here.
        var defectPin = await db.Database.SqlQuery<string>($"""
            SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(
                SELECT [Id],[ProjectId],[RoadSectionVersionId],[RowVersion]
                FROM [Defects] ORDER BY [Id] FOR JSON PATH,INCLUDE_NULL_VALUES))),2) AS [Value]
            """).SingleAsync(token);
        digest.AppendData(Encoding.UTF8.GetBytes(defectPin ?? "EMPTY_DEFECT_REGISTRY"));
        foreach (var (table, column) in new[] { ("Files", "Id"), ("FileScopes", "FileId"), ("UploadSessions", "FileId") })
        {
            var sql = $"""
                SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(
                    SELECT f.* FROM [{table}] f WHERE
                        EXISTS(SELECT 1 FROM [HandoverDocuments] h WHERE h.[FileId]=f.[{column}]) OR
                        EXISTS(SELECT 1 FROM [Warranties] w WHERE w.[SourceDocumentId]=f.[{column}]) OR
                        EXISTS(SELECT 1 FROM [RoadCoverageMappings] m WHERE m.[CoverageFileId]=f.[{column}] OR m.[HandoverFileId]=f.[{column}])
                    ORDER BY f.[Id] FOR JSON PATH,INCLUDE_NULL_VALUES))),2) AS [Value]
                """;
            var sourcePin = await db.Database.SqlQueryRaw<string>(sql).SingleAsync(token);
            digest.AppendData(Encoding.UTF8.GetBytes(table + ":" + (sourcePin ?? "EMPTY_SOURCE_SET") + ";"));
        }
        return Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant();
    }

    internal async Task<string> CaseSourceVersionAsync(Guid caseId, CancellationToken token)
    {
        var heads = await (from link in db.Set<HuyCaseReportLink>().AsNoTracking()
                           join report in db.Reports.AsNoTracking() on link.ReportId equals report.Id
                           where link.CaseId == caseId && link.EndedAt == null
                           orderby link.ReportId
                           select new { link.Id, link.ReportId, link.RowVersion, ReportVersion = EF.Property<byte[]>(report, "RowVersion") })
            .ToArrayAsync(token);
        var active = (await db.IncidentCases.AsNoTracking().SingleAsync(c => c.Id == caseId, token)).ActiveReportIds;
        if (!active.Order().SequenceEqual(heads.Select(h => h.ReportId).Order())) Fail(409, "reference_scope_conflict");
        var currentFiles = new List<object>();
        foreach (var head in heads)
        {
            var source = await files.GetReportSourceAsync(head.ReportId, token);
            if (source is null) Fail(409, "routing_source_stale");
            foreach (var evidence in source!.Evidence.OrderBy(e => e.EvidenceId))
                currentFiles.Add(new { evidence, currentFile = await files.GetFileAsync(evidence.FileId, token) });
        }
        return Hash(new { heads, currentFiles });
    }

}
