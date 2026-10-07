using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Implementations.Defects;

public sealed class MatchingCandidateSnapshotReader(
    ICaseWorkflowRepository cases, ICandidateDecisionRepository candidates,
    IProjectScopeGuard scope, IAnhHuyProducerService geometry,
    IAiCandidateFactsReader ai, TimeProvider clock) : IMatchingCandidateSnapshotReader
{
    public async Task<MatchingCandidateSnapshotV1?> CaptureAsync(Guid actorId, UserRoleCode role,
        Guid projectId, Guid routeVersionId, Guid segmentSetId, string geometryVersion,
        Guid[] detectionIds, Guid requestedSnapshotId, CancellationToken cancellationToken = default)
    {
        if (role != UserRoleCode.ProjectManager || projectId == Guid.Empty || routeVersionId == Guid.Empty ||
            segmentSetId == Guid.Empty || requestedSnapshotId == Guid.Empty || string.IsNullOrWhiteSpace(geometryVersion) ||
            detectionIds is null || detectionIds.Any(id => id == Guid.Empty) ||
            detectionIds.Distinct().Count() != detectionIds.Length)
            return null;

        return await candidates.ReadSnapshotConsistentlyAsync(token => CaptureCoreAsync(actorId, role,
            projectId, routeVersionId, segmentSetId, geometryVersion, detectionIds,
            requestedSnapshotId, token), cancellationToken);
    }

    private async Task<MatchingCandidateSnapshotV1?> CaptureCoreAsync(Guid actorId, UserRoleCode role,
        Guid projectId, Guid routeVersionId, Guid segmentSetId, string geometryVersion,
        Guid[] detectionIds, Guid requestedSnapshotId, CancellationToken cancellationToken)
    {
        try
        {
            await cases.GuardAsync(actorId, role, [], projectId,
                async (id, token) => await scope.AuthorizeAsync(actorId, role, id, token) is not null,
                cancellationToken);
            var currentGeometry = await geometry.ResolveGeometryAsync(actorId, role, projectId,
                routeVersionId, segmentSetId, geometryVersion, true, cancellationToken);
            if (currentGeometry.Status != AnhHuyProducerStatus.Ready) return null;

            var sources = new List<(Guid Id, string Version)>();
            foreach (var detection in detectionIds.Order())
            {
                await candidates.LockAiSourceAsync(detection, cancellationToken);
                var result = await ai.ResolveAsync(actorId, role, projectId, detection,
                    expectedGeometryVersion: geometryVersion, cancellationToken: cancellationToken);
                if (result.Status != AnhHuyProducerStatus.Ready || result.Facts!.RouteVersionId != routeVersionId ||
                    result.Facts.SegmentSetId != segmentSetId) return null;
                sources.Add((detection, result.Facts.SourceVersion));
            }

            var segments = currentGeometry.Facts!.Segments.Select(item => item.Id).ToHashSet();
            var items = (await candidates.MatchTargetsAsync(projectId, cancellationToken))
                .Where(target => target.RouteVersionId == routeVersionId &&
                    (target.SegmentId is null || segments.Contains(target.SegmentId.Value)))
                .OrderBy(target => target.DefectId)
                .Select(target => new MatchingCandidateItemV1(target.DefectId, target.Version,
                    target.SegmentId, routeVersionId)).ToArray();
            var canonical = SerializeCanonical(requestedSnapshotId, projectId, routeVersionId,
                segmentSetId, geometryVersion, sources, items);
            var hash = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
            return new(requestedSnapshotId, hash, projectId, routeVersionId, segmentSetId,
                geometryVersion, clock.GetUtcNow(), items);
        }
        catch (CaseWorkflowException) { return null; }
    }

    // Tuple fields are not serializable properties under the default JSON options.
    // Project them explicitly so the hash binds each detection and its version.
    public static byte[] SerializeCanonical(Guid requestedSnapshotId, Guid projectId,
        Guid routeVersionId, Guid segmentSetId, string geometryVersion,
        IEnumerable<(Guid Id, string Version)> sources, IEnumerable<MatchingCandidateItemV1> items)
        => JsonSerializer.SerializeToUtf8Bytes(new
        {
            requestedSnapshotId,
            projectId,
            routeVersionId,
            segmentSetId,
            geometryVersion,
            sources = sources.OrderBy(source => source.Id)
                .Select(source => new { sourceId = source.Id, sourceVersion = source.Version }),
            items = items.OrderBy(item => item.DefectId)
        });
}
