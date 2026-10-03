using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Services.Implementations.Integration;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

[Trait("Package", "HUY-01")]
public sealed class Huy01CaseDefectConsumerCompatibilityTests
{
    [Theory]
    [InlineData(IncidentCaseStatus.Unassigned, "UNASSIGNED")]
    [InlineData(IncidentCaseStatus.Open, "OPEN")]
    [InlineData(IncidentCaseStatus.AwaitingEvidence, "AWAITING_EVIDENCE")]
    [InlineData(IncidentCaseStatus.Concluded, "CONCLUDED")]
    [InlineData(IncidentCaseStatus.Linked, "LINKED")]
    public void ReaderStatus_UsesCanonicalCaseWorkflowVocabulary(IncidentCaseStatus status, string expected)
    {
        CaseWorkflowRepository.WireStatus(status).Should().Be(expected);
        CaseDefectReadReader.WireStatus(status).Should().Be(expected);
    }

    [Fact]
    public void ConsumerHash_UsesWebJsonAndSchemaIdentifier()
    {
        var reportId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var conclusionId = Guid.NewGuid();
        var snapshot = new CaseDefectSnapshotV1(
            "anh-huy.case-defect.v1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero),
            "",
            [new CaseReadFactV1(caseId, Guid.NewGuid(), "case-version", "AWAITING_EVIDENCE",
                [new CaseReportRefV1(reportId, "report-version", new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero))],
                [new ReportingSourceRefDto("CASE_CONCLUSION", conclusionId, "case-version")],
                [new CasePublicationRefV1(Guid.NewGuid(), "case-version", [reportId], [])])],
            [],
            [],
            ["defect_source_version_and_geometry_unavailable", "evidence_file_checksum_and_recipient_authority_not_captured"]);

        var canonicalBytes = CaseDefectReadReader.SerializeCanonical(snapshot with { Hash = "" });
        var canonicalJson = System.Text.Encoding.UTF8.GetString(canonicalBytes);
        var expected = CaseDefectReadReader.ComputeHash(snapshot);
        var legacyHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(snapshot with { Hash = "" })))
            .ToLowerInvariant();

        canonicalJson.Should().Contain("\"schemaVersion\"").And.NotContain("\"SchemaVersion\"");
        expected.Should().HaveLength(64);
        legacyHash.Should().NotBe(expected);
        snapshot.SchemaVersion.Should().Be("anh-huy.case-defect.v1");
        CaseDefectReadReader.ComputeHash(snapshot).Should().Be(expected);
        snapshot.Defects.Should().BeEmpty();
        snapshot.MissingReasons.Should().Contain("defect_source_version_and_geometry_unavailable");
    }
}
