using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.DTOs.Offline;
using Xunit;

namespace RoadGuardSystem.ApiTests.Offline;

public sealed class OfflineOperationBindingTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public void SignedOperationUsesPropertyValidationAndPreservesFieldStartJson()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        using var provider = services.BuildServiceProvider();
        var metadata = provider.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(OfflineOperationInput));
        Assert.Null(metadata.BoundConstructor);
        Assert.Contains(metadata.Properties, property => property.Name == nameof(OfflineOperationInput.FieldStart));

        var origin = Guid.NewGuid();
        var input = JsonSerializer.Deserialize<OfflineOperationInput>(
            $$$"""{"schemaVersion":1,"originId":"{{{origin}}}","fieldStart":{"originId":"{{{origin}}}","claimedAt":"2026-11-25T00:00:00Z"}}""", Json);
        Assert.NotNull(input);
        Assert.Equal(origin, input.OriginId);
        Assert.Equal(origin, input.FieldStart?.OriginId);
        Assert.Null(input.FieldSubmission);
    }
}
