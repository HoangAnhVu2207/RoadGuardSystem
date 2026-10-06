using System.Text.Json;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Reporting;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

public sealed class H7CurrentRepairFactsReaderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepositoryBoundaryPreservesScopeFiltersAndSerializedCaptureFacts(bool webJson)
    {
        var actor = Guid.NewGuid(); var project = Guid.NewGuid(); var route = Guid.NewGuid();
        var segmentSet = Guid.NewGuid(); var segment = Guid.NewGuid(); var item = Guid.NewGuid();
        var obligation = Guid.NewGuid(); var decision = Guid.NewGuid(); var pendingItem = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var repository = new CaptureRepository((actualActor, actualProject, query, token) =>
        {
            if (actualActor != actor || actualProject != project || query.RouteVersionId != route ||
                query.SegmentSetId != segmentSet || query.SegmentIds is not [var actualSegment] ||
                actualSegment != segment || token != cancellation.Token)
                throw new UnauthorizedAccessException("Capture scope or filter changed at the repository boundary.");
            return new(project,
                [new(item, project, obligation, "item-rowversion", "obligation-rowversion", "UNREPAIRED", decision, "original-decision-hash"),
                 new(pendingItem, project, obligation, "pending-rowversion", "obligation-rowversion", "REPORTED_AWAITING_REVIEW", null, null)],
                ["REPAIR_SEGMENT_ALLOCATION_NOT_VERIFIED"]);
        });
        var filters = new ReportingFiltersDto(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero), route, segmentSet, [segment]);
        var actual = await new CurrentRepairFactsReader(repository).CaptureAsync(actor, project, filters, cancellation.Token);
        var expected = new CurrentRepairFacts(project,
            [new(item, project, obligation, "item-rowversion", "obligation-rowversion", "UNREPAIRED", decision, "original-decision-hash"),
             new(pendingItem, project, obligation, "pending-rowversion", "obligation-rowversion", "REPORTED_AWAITING_REVIEW", null, null)],
            ["REPAIR_SEGMENT_ALLOCATION_NOT_VERIFIED"]);
        var options = webJson ? new JsonSerializerOptions(JsonSerializerDefaults.Web) : new JsonSerializerOptions();
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(expected, options), JsonSerializer.SerializeToUtf8Bytes(actual, options));
    }

    [Fact]
    public async Task UnknownInventoryRemainsUnavailableAtReportingConsumer()
    {
        var project = Guid.NewGuid();
        var repository = new CaptureRepository((_, _, query, _) =>
        {
            if (query.RouteVersionId is not null || query.SegmentSetId is not null || query.SegmentIds is not null)
                throw new InvalidOperationException("Absent filters must stay absent.");
            return new(project, [], ["REPAIR_OBLIGATION_INVENTORY_NOT_VERIFIED"]);
        });
        var facts = await new CurrentRepairFactsReader(repository).CaptureAsync(Guid.NewGuid(), project, new(), default);
        var capture = new ReportingCaptureDto(new("reporting.project-summary.v1", project,
            new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero), "v1", new(), [], [], "SERIALIZABLE"),
            [], [], [], new("PARTIAL", []));
        var actual = CurrentRepairCaptureConsumer.Apply(capture, facts);
        var metric = Assert.Single(actual.Summary.Metrics);
        Assert.Equal("repairItemsByStatus", metric.Code);
        Assert.Equal("UNAVAILABLE", metric.Availability);
        Assert.Null(metric.Value);
        Assert.Equal(new[] { "REPAIR_OBLIGATION_INVENTORY_NOT_VERIFIED" }, metric.ReasonCodes);
        Assert.Empty(actual.Items);
    }

    private sealed class CaptureRepository(
        Func<Guid, Guid, CurrentRepairFactsQuery, CancellationToken, CurrentRepairInventory> capture)
        : ICurrentRepairFactsRepository
    {
        public Task<CurrentRepairInventory> CaptureAsync(Guid actor, Guid project, CurrentRepairFactsQuery filters,
            CancellationToken token) => Task.FromResult(capture(actor, project, filters, token));
    }
}
