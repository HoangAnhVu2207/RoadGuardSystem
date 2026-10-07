using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Reporting;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Implementations.Integration;

// Huy-owned producer preserves incomplete source and geometry facts.
public sealed class CaseDefectReadReader(IReportingRepository repository) : ICaseDefectReadReader
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    public static string WireStatus(IncidentCaseStatus status) => CaseWorkflowRepository.WireStatus(status);
    public static byte[] SerializeCanonical(CaseDefectSnapshotV1 snapshot) => JsonSerializer.SerializeToUtf8Bytes(snapshot, WebJson);
    public static string ComputeHash(CaseDefectSnapshotV1 snapshot) => Convert.ToHexString(SHA256.HashData(SerializeCanonical(snapshot with { Hash = "" }))).ToLowerInvariant();
    public async Task<CaseDefectSnapshotV1?> CaptureAsync(Guid actorId, UserRoleCode role, Guid projectId, ReportingFiltersDto filters, CancellationToken cancellationToken = default)
    {
        CaseDefectSnapshotV1? snapshot = await repository.CaptureCaseDefectAsync(actorId, role, projectId, filters, cancellationToken);
        return snapshot is null ? null : snapshot with { Hash = ComputeHash(snapshot) };
    }
}
