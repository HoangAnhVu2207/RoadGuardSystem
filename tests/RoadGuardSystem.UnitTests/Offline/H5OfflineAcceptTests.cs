using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineAcceptTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public void ExplicitAcceptBindsUnchangedActionBodyAsItsOwnSignedDependency()
    {
        var origin=Guid.NewGuid(); var actor=Guid.NewGuid(); var task=Guid.NewGuid();
        var action=new FieldTaskActionInput("accepted offline");
        var core=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {id=task,input=action,OriginalActorId=actor},Json))).ToLowerInvariant();
        var operation=new OfflineOperationInput(1,origin,origin,"FIELD_ACCEPT",actor,Guid.NewGuid(),Guid.NewGuid(),task,
            Guid.NewGuid(),Convert.ToBase64String(new byte[8]),core,[],null,FieldAction:action);
        Assert.Equal(core,OfflineWorkflowEngine.FieldCoreHash(operation));
        Assert.Equal("FIELD_ACCEPT",OfflineWorkflowEngine.Describe(operation).Kind);
        Assert.Throws<ArgumentException>(()=>OfflineWorkflowEngine.Describe(operation with {FieldAction=null}));
        Assert.Throws<ArgumentException>(()=>OfflineWorkflowEngine.Describe(operation with {FieldAction=action with {AssignedToUserId=Guid.NewGuid()}}));
    }
    [Fact]
    public void DurableReplayedDependencyCanUnblockPendingOperation()
    {
        var dependency=Guid.NewGuid(); var origin=Guid.NewGuid(); var actor=Guid.NewGuid(); var task=Guid.NewGuid(); var device=Guid.NewGuid();
        var start=new FieldStartInput(origin,DateTimeOffset.UtcNow,device);
        var core=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {id=task,input=start,OriginalActorId=actor},Json))).ToLowerInvariant();
        var operation=new OfflineOperationInput(1,origin,origin,"FIELD_START",actor,device,Guid.NewGuid(),task,
            Guid.NewGuid(),Convert.ToBase64String(new byte[8]),core,[dependency],null,start);
        Assert.Equal(new[] {origin},OfflineWorkflowEngine.ReadyOrigins([operation],new Dictionary<Guid,string>{{dependency,"REPLAYED"}}));
    }
}
