using System.Text.Json;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineCanonicalFixtureTests
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    [Fact]
    public void SyntheticTypedByteProfileRoundTripsWithoutChangingExistingFieldCoreHash()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null && !File.Exists(Path.Combine(directory.FullName,"tests/RoadGuardSystem.UnitTests/Offline/Fixtures/field-start-v1.json")))directory=directory.Parent;
        Assert.NotNull(directory);
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName,"tests/RoadGuardSystem.UnitTests/Offline/Fixtures/field-start-v1.json")));
        var operation=fixture.RootElement.GetProperty("operation").Deserialize<OfflineOperationInput>(Json)!;
        Assert.Equal(fixture.RootElement.GetProperty("coreSha256").GetString(),OfflineWorkflowEngine.FieldCoreHash(operation));
        Assert.Equal(fixture.RootElement.GetProperty("envelopeSha256").GetString(),OfflineWorkflowEngine.EnvelopeHash(operation));
        Assert.Equal("FIELD_START",OfflineWorkflowEngine.Describe(operation).Kind);
        var internalOperation = OfflineContractMapping.ToData(operation);
        Assert.Equal(fixture.RootElement.GetProperty("envelopeSha256").GetString(),
            OfflineContractMapping.RepositoryAlgorithms.EnvelopeHash(internalOperation));
        Assert.Equal(fixture.RootElement.GetProperty("coreSha256").GetString(),
            OfflineWorkflowEngine.FieldCoreHash(OfflineContractMapping.ToWire(internalOperation)));
        Assert.Equal(JsonSerializer.Serialize(operation, Json), JsonSerializer.Serialize(internalOperation, Json));
    }
}
