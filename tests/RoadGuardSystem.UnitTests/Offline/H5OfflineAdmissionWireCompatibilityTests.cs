using System.Text.Json;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineAdmissionWireCompatibilityTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Legacy = "{\"captureOriginId\":\"00000000-0000-0000-0000-000000000001\",\"fileId\":null,\"purpose\":\"MEASUREMENT\",\"checksumSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"mediaType\":\"image/png\",\"capturedAt\":null,\"attemptChecklist\":null}";

    [Fact]
    public void LegacyCaptureBytesStayIdenticalAcrossWirePersistenceAndInternalBoundaries()
    {
        var wire = JsonSerializer.Deserialize<FieldEvidenceDeclaration>(Legacy, Json)!;
        FieldEvidenceDeclarationFact fact = wire;
        var internalValue = JsonSerializer.Deserialize<RepairFieldEvidenceData>(Legacy, Json)!;
        Assert.Null(wire.SizeBytes); Assert.Null(fact.SizeBytes); Assert.Null(internalValue.SizeBytes);
        Assert.Equal(Legacy, JsonSerializer.Serialize(wire, Json));
        Assert.Equal(Legacy, JsonSerializer.Serialize(fact, Json));
        Assert.Equal(Legacy, JsonSerializer.Serialize(internalValue, Json));
        Assert.Equal(Legacy, JsonSerializer.Serialize((FieldEvidenceDeclaration)fact, Json));
        var sized = wire with { SizeBytes = 68 };
        FieldEvidenceDeclarationFact sizedFact = sized;
        Assert.Equal(68, sizedFact.SizeBytes);
        Assert.Equal(JsonSerializer.Serialize(sized, Json), JsonSerializer.Serialize(sizedFact, Json));
        Assert.NotEqual(Legacy, JsonSerializer.Serialize(sized, Json));
    }
}
