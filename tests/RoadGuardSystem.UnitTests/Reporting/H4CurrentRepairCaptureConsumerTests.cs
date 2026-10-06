using System.Text.Json;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

public sealed class H4CurrentRepairCaptureConsumerTests
{
    private static readonly Guid Project = Guid.NewGuid();
    private static ReportingCaptureDto Capture() => new(new("reporting.project-summary.v1", Project, DateTimeOffset.UtcNow, "v1", new(),
        [new("repairItemsByStatus",new(),null,"count",null,null,false,"UNAVAILABLE",["producer_pending"],[]),
         new("repairAcceptanceRate",new(),null,"ratio",null,null,true,"UNAVAILABLE",["period_interpretation_pending"],[])], [], "SERIALIZABLE"), [], [], [], new("PARTIAL", []));
    private static CurrentRepairItemFact Item(string status, Guid? decision = null) => new(Guid.NewGuid(), Project, Guid.NewGuid(), "actual-item-version", "actual-obligation-version", status, decision, decision is null ? null : "immutable-decision-hash");
    [Fact]
    public void EffectiveCorrectionChangesNewStockCaptureAndPreservesOldSnapshotAndRate()
    {
        var old = Capture(); var bytes = JsonSerializer.SerializeToUtf8Bytes(old); var id = Guid.NewGuid();
        var before = CurrentRepairCaptureConsumer.Apply(old, new(Project, [Item("CONFIRMED", id)], []));
        var fact = Item("UNREPAIRED", Guid.NewGuid());
        var after = CurrentRepairCaptureConsumer.Apply(old, new(Project, [fact], []));
        Assert.Equal("CONFIRMED", Assert.Single(before.Summary.Metrics.Where(row => row.Code == "repairItemsByStatus")).Dimensions.Status);
        var metric = Assert.Single(after.Summary.Metrics.Where(row => row.Code == "repairItemsByStatus"));
        Assert.Equal("UNREPAIRED", metric.Dimensions.Status); Assert.Equal(1, metric.Value); Assert.False(metric.PeriodApplicable);
        Assert.Contains(metric.SourceRefs, row => row.Id == fact.DecisionId && row.Version == fact.DecisionVersion);
        Assert.Equal("UNREPAIRED", Assert.Single(after.Items).Status);
        Assert.Equal(Assert.Single(old.Summary.Metrics.Where(row => row.Code == "repairAcceptanceRate")), Assert.Single(after.Summary.Metrics.Where(row => row.Code == "repairAcceptanceRate")));
        Assert.Equal(bytes, JsonSerializer.SerializeToUtf8Bytes(old));
    }
    [Fact]
    public void MissingSourceNeverBecomesAvailableZero()
    {
        var result = CurrentRepairCaptureConsumer.Apply(Capture(), new(Project, [], ["repair_current_attempt_pin_pending"]));
        var metric = Assert.Single(result.Summary.Metrics.Where(row => row.Code == "repairItemsByStatus"));
        Assert.Null(metric.Value); Assert.Equal("UNAVAILABLE", metric.Availability); Assert.Contains("repair_current_attempt_pin_pending", metric.ReasonCodes);
    }
    [Fact]
    public void CompleteEmptyInventoryCanProveZeroWithoutInventingRate()
    {
        var result = CurrentRepairCaptureConsumer.Apply(Capture(), new(Project, [], []));
        var metric = Assert.Single(result.Summary.Metrics.Where(row => row.Code == "repairItemsByStatus"));
        Assert.Equal(0, metric.Value); Assert.Equal("AVAILABLE", metric.Availability);
        Assert.Null(Assert.Single(result.Summary.Metrics.Where(row => row.Code == "repairAcceptanceRate")).Value);
    }
    [Theory]
    [InlineData("UNKNOWN")]
    [InlineData("CONFIRMED")]
    public void CrossProjectAndInvalidSourceAreRejected(string status)
    {
        var fact = Item(status) with { ProjectId = Guid.NewGuid() };
        Assert.Throws<InvalidOperationException>(() => CurrentRepairCaptureConsumer.Apply(Capture(), new(Project, [fact], [])));
    }
}
