using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

internal static class RoadCoverageResolver
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, Json))).ToLowerInvariant();
    internal sealed record SourcePin(RoadCoverageSourceFact View, object Facts, bool Synthetic = false);
    internal static async Task<SourcePin?> File(RoadGuardDbContext db, Guid project, Guid id, CancellationToken token)
    {
        var row = await (from file in db.Files.AsNoTracking()
                         join scope in db.FileScopes.AsNoTracking() on file.Id equals scope.FileId
                         join upload in db.UploadSessions.AsNoTracking() on file.Id equals upload.FileId
                         where file.Id == id && scope.ProjectId == project && upload.Status == UploadSessionStatus.Verified
                         select new
                         {
                             file.Id,
                             file.Checksum,
                             file.SizeBytes,
                             file.MimeType,
                             file.UploadedAt,
                             file.UploadedByUserId,
                             file.StorageUri,
                             scope.Purpose,
                             Version = upload.RowVersion
                         }).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var synthetic = row.Purpose.StartsWith("TEST_ONLY", StringComparison.Ordinal) || row.StorageUri.StartsWith("TEST_ONLY/", StringComparison.Ordinal);
        return new(new("MAINTENANCE_BASIS", id, id, Hash(row), Provenance: synthetic ? "TEST_ONLY" : "SOURCE_NOT_EXTERNALLY_VERIFIED"), row, synthetic);
    }
    internal static async Task<SourcePin?> Source(RoadGuardDbContext db, Guid project, string kind, Guid id, CancellationToken token)
    {
        if (kind == "MAINTENANCE_BASIS") return await File(db, project, id, token);
        if (kind == "HANDOVER")
        {
            var document = await db.HandoverDocuments.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id && row.ProjectId == project, token);
            if (document?.FileId is not Guid fileId || document.AcceptedByUserId is null) return null;
            var file = await File(db, project, fileId, token); if (file is null) return null;
            var facts = new
            {
                document.Id,
                document.ProjectId,
                document.DocumentNo,
                document.HandoverDate,
                document.AcceptedByUserId,
                document.FileId,
                Version = Convert.ToBase64String(document.RowVersion),
                file = file.Facts
            };
            return new(new(kind, id, fileId, Hash(facts), document.HandoverDate, Provenance: file.View.Provenance), facts, file.Synthetic);
        }
        if (kind != "WARRANTY") return null;
        var warranty = await db.Warranties.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id && row.ProjectId == project, token);
        if (warranty?.SourceDocumentId is not Guid evidence || warranty.Status != WarrantyStatus.Active) return null;
        var pin = await File(db, project, evidence, token); if (pin is null) return null;
        var source = new
        {
            warranty.Id,
            warranty.ProjectId,
            warranty.RoadSectionId,
            warranty.HandoverDocumentId,
            warranty.HandoverDate,
            warranty.WarrantyStartDate,
            warranty.WarrantyEndDate,
            warranty.Scope,
            warranty.Status,
            warranty.SourceDocumentId,
            warranty.Terms,
            file = pin.Facts
        };
        return new(new(kind, id, evidence, Hash(source), warranty.WarrantyStartDate, warranty.WarrantyEndDate, pin.View.Provenance), source, pin.Synthetic);
    }
    internal static async Task<bool> CurrentSources(RoadGuardDbContext db, RoadCoverageMapping mapping, CancellationToken token)
    {
        var handover = await Source(db, mapping.ProjectId, "HANDOVER", mapping.HandoverDocumentId, token);
        var coverage = await Source(db, mapping.ProjectId, mapping.SourceKind, mapping.SourceId, token);
        if (mapping.Provenance == "REAL_SOURCE" && (handover?.Synthetic == true || coverage?.Synthetic == true)) return false;
        return handover?.View.Version == mapping.HandoverVersion && coverage?.View.Version == mapping.SourceVersion &&
            handover.View.FileId == mapping.HandoverFileId && coverage.View.FileId == mapping.CoverageFileId;
    }
    internal static async Task<RoadCoverageResolution> Resolve(RoadGuardDbContext db, Guid project, RepairActualScope scope,
        DateTimeOffset at, CancellationToken token)
    {
        var candidates = await db.Set<RoadCoverageMapping>().AsNoTracking().Where(row => row.ProjectId == project &&
            row.RoadSectionId == scope.PhysicalRoadId && row.LocationVersion == scope.LocationVersion &&
            !db.Set<RoadCoverageMapping>().Any(next => next.SupersedesId == row.Id)).ToArrayAsync(token);
        if (candidates.Length == 0) return new("UNKNOWN_OWNER_MAPPING");
        var whole = candidates.Where(row => row.From <= scope.From && row.To >= scope.To &&
            row.OffsetFrom <= scope.OffsetFrom && row.OffsetTo >= scope.OffsetTo &&
            row.ApplicableFromUtc <= at && at < row.ApplicableToUtc).ToArray();
        if (whole.Length == 0) return new("COVERAGE_PARTIAL_OR_MISMATCH");
        if (whole.Length != 1) return new("COVERAGE_AMBIGUOUS");
        var current = whole[0];
        if (!await CurrentSources(db, current, token)) return new("SOURCE_STALE");
        return new(current.Provenance == "TEST_ONLY" ? "TEST_ONLY_MAPPING" : "CONFIRMED", current);
    }
}
