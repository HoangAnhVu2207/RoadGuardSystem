using System.Text;
using RoadGuardSystem.Services.Processing.Anh02;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class Anh02ManifestPreparationTests
{
    private static readonly Guid Video = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid Telemetry = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid Segment = Guid.Parse("00000000-0000-0000-0000-000000000030");

    [Fact]
    public void Set_order_and_timezone_do_not_change_canonical_bytes()
    {
        var input = Manifest();
        var equivalent = input with
        {
            SegmentIds = input.SegmentIds.Reverse().ToArray(),
            SourceFiles = input.SourceFiles.Reverse().ToArray(),
            CreatedAt = input.CreatedAt.ToOffset(TimeSpan.FromHours(7))
        };
        Assert.Equal(AiManifestCanonicalizer.Encode(input), AiManifestCanonicalizer.Encode(equivalent));
    }

    [Fact]
    public void Canonical_bytes_preserve_int64_and_utc_and_have_no_hash_or_transport()
    {
        var json = Encoding.UTF8.GetString(AiManifestCanonicalizer.Encode(Manifest()));
        Assert.Contains("\"sizeBytes\":8589934592", json);
        Assert.Contains("\"createdAt\":\"2026-10-02T00:00:00.0000000Z\"", json);
        Assert.StartsWith("{\"schemaVersion\":\"anh02.ai.v1\",\"runId\":", json);
        Assert.DoesNotContain("manifestHash", json);
        Assert.DoesNotContain("storageUri", json);
    }

    [Fact]
    public void Source_version_change_changes_hash()
    {
        var input = Manifest();
        var changed = input with { SourceFiles = [input.SourceFiles[0] with { FileVersion = "v2" }, input.SourceFiles[1]] };
        Assert.NotEqual(AiManifestCanonicalizer.Hash(AiManifestCanonicalizer.Encode(input)),
            AiManifestCanonicalizer.Hash(AiManifestCanonicalizer.Encode(changed)));
    }

    [Theory]
    [InlineData("segments")]
    [InlineData("files")]
    [InlineData("pairs")]
    public void Ambiguous_set_identities_are_rejected(string collection)
    {
        var input = Manifest();
        input = collection switch
        {
            "segments" => input with { SegmentIds = [Segment, Segment] },
            "files" => input with { SourceFiles = [input.SourceFiles[0], input.SourceFiles[0]] },
            _ => input with { Pairs = [input.Pairs[0], input.Pairs[0]] }
        };
        Assert.Throws<ArgumentException>(() => AiManifestCanonicalizer.Encode(input));
    }

    [Fact]
    public void Pair_outside_manifest_is_rejected()
    {
        var input = Manifest() with { Pairs = [new(Video, Guid.NewGuid())] };
        Assert.Throws<ArgumentException>(() => AiManifestCanonicalizer.Encode(input));
    }

    [Fact]
    public void Matching_without_immutable_analysis_and_candidate_snapshot_is_rejected()
    {
        var input = Manifest() with { Stage = "DUPLICATE_MATCHING" };
        Assert.Throws<ArgumentException>(() => AiManifestCanonicalizer.Encode(input));
    }

    [Fact]
    public void Analysis_cannot_smuggle_matching_references()
    {
        var input = Manifest() with { AnalysisResultId = Guid.NewGuid() };
        Assert.Throws<ArgumentException>(() => AiManifestCanonicalizer.Encode(input));
    }

    [Fact]
    public void Hash_checks_original_bytes_without_reserializing()
    {
        // Published SHA-256 vector for UTF-8 "abc", independent of our serializer.
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            AiManifestCanonicalizer.Hash("abc"u8));
        Assert.NotEqual(AiManifestCanonicalizer.Hash("{}"u8), AiManifestCanonicalizer.Hash("{ }"u8));
    }

    [Fact]
    public void Missing_telemetry_remains_explicit_without_fabricated_pair()
    {
        var input = Manifest();
        input = input with { SourceFiles = [input.SourceFiles[0]], Pairs = [], TelemetryStatus = "UNKNOWN" };
        var json = Encoding.UTF8.GetString(AiManifestCanonicalizer.Encode(input));
        Assert.Contains("\"pairs\":[]", json);
        Assert.Contains("\"telemetryStatus\":\"UNKNOWN\"", json);
    }

    [Fact]
    public void Matching_item_order_does_not_change_hash()
    {
        var input = Manifest() with
        {
            Stage = "DUPLICATE_MATCHING", AnalysisResultId = Guid.NewGuid(), CandidateSnapshotId = Guid.NewGuid(),
            CandidateSnapshotHash = new string('d', 64)
        };
        input = input with { Items = [new(Guid.NewGuid(), "d1", Segment, input.RouteVersionId),
            new(Guid.NewGuid(), "d2", null, input.RouteVersionId)] };
        var reversed = input with { Items = input.Items.Reverse().ToArray() };
        Assert.Equal(AiManifestCanonicalizer.Encode(input), AiManifestCanonicalizer.Encode(reversed));
    }

    [Fact]
    public void Wrong_pair_purpose_is_rejected()
    {
        var input = Manifest();
        input = input with { SourceFiles = [input.SourceFiles[0], input.SourceFiles[1] with { Purpose = "REPORT_PHOTO" }] };
        Assert.Throws<ArgumentException>(() => AiManifestCanonicalizer.Encode(input));
    }

    [Fact]
    public void Non_video_source_cannot_be_an_analysis_manifest()
    {
        var input = Manifest();
        input = input with { SourceFiles = [input.SourceFiles[1]], Pairs = [], TelemetryStatus = "UNKNOWN" };
        Assert.Throws<ArgumentException>(() => AiManifestCanonicalizer.Encode(input));
    }

    private static AiManifestDraft Manifest() => new(
        Guid.NewGuid(), "VIDEO_ANALYSIS", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new string('a', 64), Guid.NewGuid(), Guid.NewGuid(), "geometry-v1",
        [Segment, Guid.Parse("00000000-0000-0000-0000-000000000031")], "SURFACE",
        Guid.NewGuid(), "pre-v1", "config-v1", "synthetic-v1", Guid.NewGuid(),
        DateTimeOffset.Parse("2026-10-02T00:00:00Z"),
        [new(Video, "v1", new string('b', 64), 8_589_934_592, "video/mp4", "SURVEY_VIDEO"),
         new(Telemetry, "v1", new string('c', 64), 10, "text/plain", "TELEMETRY")],
        [new(Video, Telemetry)], "PRESENT");
}
