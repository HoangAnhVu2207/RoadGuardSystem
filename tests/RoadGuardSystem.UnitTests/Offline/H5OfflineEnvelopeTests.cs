using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineEnvelopeTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public void FieldCoreHashIsTheExactExistingCanonicalBodyWithoutOfflineSelectors()
    {
        var input=Start();var expected=Hash(new{id=input.TaskId,input=input.FieldStart,input.OriginalActorId});
        Assert.Equal(expected,OfflineWorkflowEngine.FieldCoreHash(input));
        Assert.Equal(expected,OfflineWorkflowEngine.FieldCoreHash(input with{SnapshotId=Guid.NewGuid()}));
        Assert.NotEqual(OfflineWorkflowEngine.EnvelopeHash(input),OfflineWorkflowEngine.EnvelopeHash(input with{SnapshotId=Guid.NewGuid()}));
    }
    [Fact]
    public void SignedDescriptorOrderIsCanonicalAndIndependentOfBatchInputOrder()
    {
        var first=Start();var second=Start();var descriptors=new[]{OfflineWorkflowEngine.Describe(first),OfflineWorkflowEngine.Describe(second)};
        var project=Guid.NewGuid();var batch=Guid.NewGuid();var device=Guid.NewGuid();
        Assert.Equal(OfflineWorkflowEngine.CanonicalManifest(project,batch,device,descriptors),OfflineWorkflowEngine.CanonicalManifest(project,batch,device,descriptors.Reverse().ToArray()));
    }
    [Fact]
    public void OriginBodyOrDeviceRelabelingCannotPassTypedEnvelopeValidation()
    {
        var input=Start();Assert.Throws<ArgumentException>(()=>OfflineWorkflowEngine.Describe(input with{OriginId=Guid.NewGuid()}));
        Assert.Throws<ArgumentException>(()=>OfflineWorkflowEngine.Describe(input with{SourceDeviceId=Guid.NewGuid()}));
        Assert.Throws<ArgumentException>(()=>OfflineWorkflowEngine.Describe(input with{Kind="FIELD_SUBMISSION"}));
        Assert.Throws<ArgumentException>(()=>OfflineWorkflowEngine.Describe(input with{CorePayloadHash=new string('f',64)}));
    }
    [Fact]
    public void IndependentOperationsContinueWhileDependenciesAwaitDurableAcknowledgment()
    {
        var first=Start();var dependent=Start() with{Dependencies=[first.OriginId]};var independent=Start();
        var operations=new[]{dependent,independent,first};var statuses=new Dictionary<Guid,string>();
        Assert.Equal(new[]{first.OriginId,independent.OriginId}.Order(),OfflineWorkflowEngine.ReadyOrigins(operations,statuses).Order());
        statuses[first.OriginId]="CONFLICT";Assert.DoesNotContain(dependent.OriginId,OfflineWorkflowEngine.ReadyOrigins(operations,statuses));
        statuses[first.OriginId]="COMMITTED";Assert.Contains(dependent.OriginId,OfflineWorkflowEngine.ReadyOrigins(operations,statuses));
    }
    [Theory]
    [InlineData(-1,"ON_TIME")]
    [InlineData(0,"LATE")]
    [InlineData(1,"LATE")]
    public void VerifiedFinishSyncBoundaryUsesOriginalFinishAndExactTwentyFourHours(int seconds,string state)
    {
        var finish=new DateTimeOffset(2026,10,6,1,0,0,TimeSpan.Zero);
        Assert.Equal(state,OfflineWorkflowEngine.SyncLateness(finish,finish.AddHours(24).AddSeconds(seconds)));
        Assert.Equal("UNKNOWN",OfflineWorkflowEngine.SyncLateness(null,finish.AddYears(1)));
    }
    private static OfflineOperationInput Start()
    {
        var origin=Guid.NewGuid();var actor=Guid.NewGuid();var device=Guid.NewGuid();var task=Guid.NewGuid();var body=new FieldStartInput(origin,DateTimeOffset.UtcNow,device,10,"boot","offline claimed integrity proof");
        var hash=Hash(new{id=task,input=body,OriginalActorId=actor});
        return new(1,origin,origin,"FIELD_START",actor,device,Guid.NewGuid(),task,Guid.NewGuid(),Convert.ToBase64String(new byte[8]),hash,[],null,body);
    }
    private static string Hash(object value)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value,Json))).ToLowerInvariant();
}
