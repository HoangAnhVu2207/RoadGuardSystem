using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.DTOs.Reporting;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

[Trait("Package", "HUY-01")]
public sealed class Huy01CaseDefectConsumerCompatibilityTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ConsumerHash_UsesWebJsonAndSchemaIdentifier()
    {
        var snapshot = new CaseDefectSnapshotV1(
            "anh-huy.case-defect.v1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero),
            "",
            [],
            [],
            [],
            ["evidence_file_checksum_and_recipient_authority_not_captured"]);

        var canonicalBytes = JsonSerializer.SerializeToUtf8Bytes(snapshot with { Hash = "" }, WebJson);
        var canonicalJson = System.Text.Encoding.UTF8.GetString(canonicalBytes);
        var expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(canonicalBytes))
            .ToLowerInvariant();
        var legacyHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(snapshot with { Hash = "" })))
            .ToLowerInvariant();

        canonicalJson.Should().Contain("\"schemaVersion\"").And.NotContain("\"SchemaVersion\"");
        expected.Should().HaveLength(64);
        legacyHash.Should().NotBe(expected);
        snapshot.SchemaVersion.Should().Be("anh-huy.case-defect.v1");
    }
}
