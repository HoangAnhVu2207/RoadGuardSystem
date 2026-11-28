using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.DTOs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineCanonicalFixtureTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public void SyntheticTypedByteProfileRoundTripsWithoutChangingExistingFieldCoreHash()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "tests/RoadGuardSystem.UnitTests/Offline/Fixtures/field-start-v1.json"))) directory = directory.Parent;
        Assert.NotNull(directory);
        foreach (var name in new[] { "field-start-v1.json", "field-submission-legacy-v1.json" })
        {
            using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "tests/RoadGuardSystem.UnitTests/Offline/Fixtures", name)));
            var operation = fixture.RootElement.GetProperty("operation").Deserialize<OfflineOperationInput>(Json)!;
            Assert.Equal(fixture.RootElement.GetProperty("coreSha256").GetString(), OfflineWorkflowEngine.FieldCoreHash(operation));
            Assert.Equal(fixture.RootElement.GetProperty("envelopeSha256").GetString(), OfflineWorkflowEngine.EnvelopeHash(operation));
            Assert.Equal(name == "field-start-v1.json" ? "FIELD_START" : "FIELD_SUBMISSION", OfflineWorkflowEngine.Describe(operation).Kind);
            Assert.Equal(fixture.RootElement.GetProperty("envelopeCanonicalUtf8").GetString(), JsonSerializer.Serialize(operation, Json));
            var internalOperation = OfflineContractMapping.ToData(operation);
            Assert.Equal(fixture.RootElement.GetProperty("envelopeSha256").GetString(),
                OfflineContractMapping.RepositoryAlgorithms.EnvelopeHash(internalOperation));
            Assert.Equal(fixture.RootElement.GetProperty("coreSha256").GetString(),
                OfflineWorkflowEngine.FieldCoreHash(OfflineContractMapping.ToWire(internalOperation)));
            Assert.Equal(JsonSerializer.Serialize(operation, Json), JsonSerializer.Serialize(internalOperation, Json));
        }
    }
    [Fact]
    public void LegacyCaptureBytesStayExactAndGpsSurvivesAllBoundaryAdapters()
    {
        const string legacy = """{"captureOriginId":"00000000-0000-0000-0000-000000000001","fileId":null,"purpose":"MEASUREMENT","checksumSha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","mediaType":"image/png","capturedAt":null,"attemptChecklist":null}""";
        var capture = JsonSerializer.Deserialize<FieldEvidenceDeclaration>(legacy, Json)!;
        Assert.Equal(legacy, JsonSerializer.Serialize(capture, Json));
        FieldEvidenceDeclarationFact fact = capture;
        Assert.Equal(legacy, JsonSerializer.Serialize(fact, Json));
        var point = new FieldCaptureCoordinates(106.125, 10.75, null, "CAPTURE", DateTimeOffset.Parse("2026-11-28T01:02:03Z", System.Globalization.CultureInfo.InvariantCulture));
        capture = capture with { Coordinates = point };
        var current = JsonSerializer.Serialize(capture, Json);
        Assert.NotEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(legacy))), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(current))));
        Assert.Equal(current, JsonSerializer.Serialize(BoundaryFactMappings.ToFacts(capture), Json));
        Assert.Equal(capture, (FieldEvidenceDeclaration)(FieldEvidenceDeclarationFact)capture);
        var submission = new FieldSubmissionInput(Guid.NewGuid(), Guid.NewGuid(), null, [], [capture],
            new("UNKNOWN", null, null, null), "MEASUREMENT", null, null, Guid.NewGuid());
        var op = new OfflineOperationInput(1, submission.OriginId, submission.OriginId, "FIELD_SUBMISSION", Guid.NewGuid(), submission.DeviceId!.Value,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "version", new string('0', 64), [], null, FieldSubmission: submission);
        op = op with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(op) };
        var data = OfflineContractMapping.ToData(op);
        Assert.Equal(JsonSerializer.Serialize(op, Json), JsonSerializer.Serialize(data, Json));
        Assert.Equal(op.CorePayloadHash, OfflineWorkflowEngine.FieldCoreHash(OfflineContractMapping.ToWire(data)));
        Assert.Equal(OfflineWorkflowEngine.EnvelopeHash(op), OfflineContractMapping.RepositoryAlgorithms.EnvelopeHash(data));
        var changed = op with { FieldSubmission = submission with { Evidence = [capture with { Coordinates = point with { Longitude = 107 } }] } };
        Assert.NotEqual(op.CorePayloadHash, OfflineWorkflowEngine.FieldCoreHash(changed));
        Assert.NotEqual(OfflineWorkflowEngine.EnvelopeHash(op), OfflineWorkflowEngine.EnvelopeHash(changed));
    }

}
