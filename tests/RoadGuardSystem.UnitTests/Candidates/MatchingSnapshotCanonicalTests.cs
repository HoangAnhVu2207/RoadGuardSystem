using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.Services.Implementations.Defects;
using RoadGuardSystem.Services.Integration;
using Xunit;

namespace RoadGuardSystem.UnitTests.Candidates;

public sealed class MatchingSnapshotCanonicalTests
{
    private static readonly Guid Snapshot = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Project = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Route = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid Set = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid SourceA = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid SourceB = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly MatchingCandidateItemV1 Target = new(
        Guid.Parse("60000000-0000-0000-0000-000000000001"), "target.v1", null, Route);

    [Fact, Trait("Package", "HUY-01")]
    public void CanonicalVector_ContainsSourceIdentityAndVersion_AndMatchesIndependentOracle()
    {
        // Literal contract vector is independent of the production serializer/helper.
        const string golden = """
            {"requestedSnapshotId":"10000000-0000-0000-0000-000000000001","projectId":"20000000-0000-0000-0000-000000000001","routeVersionId":"30000000-0000-0000-0000-000000000001","segmentSetId":"40000000-0000-0000-0000-000000000001","geometryVersion":"geometry.v1","sources":[{"sourceId":"50000000-0000-0000-0000-000000000001","sourceVersion":"source.a.v1"},{"sourceId":"50000000-0000-0000-0000-000000000002","sourceVersion":"source.b.v1"}],"items":[{"DefectId":"60000000-0000-0000-0000-000000000001","Version":"target.v1","SegmentId":null,"RouteVersionId":"30000000-0000-0000-0000-000000000001"}]}
            """;
        var canonical = Encode([(SourceB, "source.b.v1"), (SourceA, "source.a.v1")]);
        Assert.Equal(golden, Encoding.UTF8.GetString(canonical));
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(golden)), SHA256.HashData(canonical));
        using var parsed = JsonDocument.Parse(canonical);
        var source = parsed.RootElement.GetProperty("sources")[0];
        Assert.Equal(SourceA, source.GetProperty("sourceId").GetGuid());
        Assert.Equal("source.a.v1", source.GetProperty("sourceVersion").GetString());
    }

    [Fact, Trait("Package", "HUY-01")]
    public void FixedScopeTargetsAndCardinality_DifferentSourceIdentityOrVersionChangesHash()
    {
        var baseline = Hash([(SourceA, "source.a.v1"), (SourceB, "source.b.v1")]);
        Assert.NotEqual(baseline, Hash([(Guid.Parse("50000000-0000-0000-0000-000000000003"), "source.a.v1"), (SourceB, "source.b.v1")]));
        Assert.NotEqual(baseline, Hash([(SourceA, "source.a.v2"), (SourceB, "source.b.v1")]));
        Assert.Equal(baseline, Hash([(SourceB, "source.b.v1"), (SourceA, "source.a.v1")]));
        Assert.Equal(baseline, Hash([(SourceA, "source.a.v1"), (SourceB, "source.b.v1")]));
    }

    private static byte[] Encode((Guid Id, string Version)[] sources)
        => MatchingCandidateSnapshotReader.SerializeCanonical(Snapshot, Project, Route, Set,
            "geometry.v1", sources, [Target]);
    private static string Hash((Guid Id, string Version)[] sources)
        => Convert.ToHexString(SHA256.HashData(Encode(sources)));
}
