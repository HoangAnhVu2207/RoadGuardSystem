using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Implementations.Reporting;
using RoadGuardSystem.Services.Reporting;
using RoadGuardSystem.DTOs.Reporting;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class LD08DefectStatisticsSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task GenuineDefectMultiSegmentSharedIdentityCountsOnceAndActualQuantitiesStayKnownUnallocatedOrUnknown()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db); var repo = new DefectStatisticsRepository(db, TimeProvider.System);
        var read = await repo.ReadAsync(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, new(), default);
        Assert.Equal(200, read.Status); Assert.Equal(2, read.Value!.Statistics.ProjectDistinctDefects);
        Assert.Equal(2, read.Value.Statistics.UnmappedDefects); Assert.All(read.Value.Statistics.Segments, row => Assert.Null(row.RelatedDefects));
        var scope = read.Value.Scopes.Single(row => row.DefectId == state.Source.Defect); var measurement = read.Value.Measurements.Single(row => row.Id == state.Area);
        var shared = Guid.NewGuid(); var input = new DefectStatisticsInput(scope.ObligationId, scope.ScopeHash, scope.DefectVersion, shared, state.Segments,
            [new(measurement.Id, measurement.Version, state.Segments[0])], "TEST_ONLY", "TEST_ONLY explicit physical identity");
        var command = new DefectStatisticsCommand(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, input, Guid.NewGuid().ToString(), read.Value.Version);
        Assert.Equal(403, (await repo.ConfirmAsync(command with { ActorId = state.Source.Supervisor, Role = UserRoleCode.Supervisor }, default)).Status);
        Assert.Equal(409, (await repo.ConfirmAsync(command with { Input = input with { ScopeHash = new string('0', 64) } }, default)).Status);
        Assert.Equal(409, (await repo.ConfirmAsync(command with { Input = input with { Provenance = "REAL_SOURCE" } }, default)).Status);
        var first = await repo.ConfirmAsync(command, default); Assert.Equal(201, first.Status);
        Assert.Equal(200, (await repo.ConfirmAsync(command, default)).Status);
        Assert.Equal(409, (await repo.ConfirmAsync(command with { Input = input with { Reason = "different" } }, default)).Status);
        var secondScope = first.Value!.Scopes.Single(row => row.DefectId == state.SecondDefect);
        var secondInput = input with { ObligationId = secondScope.ObligationId, ScopeHash = secondScope.ScopeHash, DefectVersion = secondScope.DefectVersion };
        var second = await repo.ConfirmAsync(command with { Input = secondInput, Key = Guid.NewGuid().ToString(), ExpectedVersion = first.Value.Version }, default);
        Assert.Equal(201, second.Status); var statistics = second.Value!.Statistics;
        Assert.Equal(2, statistics.ProjectDistinctDefects); Assert.Equal(1, statistics.SharedParts);
        Assert.All(statistics.Segments, row => Assert.Equal(2, row.RelatedDefects)); Assert.Equal(4, statistics.Segments.Sum(row => row.RelatedDefects));
        Assert.Equal(0, statistics.UnmappedDefects); var quantity = Assert.Single(statistics.Quantities);
        Assert.Equal(12m, quantity.Value); Assert.Equal("m\u00b2", quantity.Unit); Assert.Equal(state.Area, quantity.MeasurementId); Assert.Equal("TEST_ONLY", quantity.Provenance);
        var filtered = await repo.ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, new(SegmentIds: [state.Segments[1]]), default);
        Assert.Equal(2, filtered.Value!.Statistics.ProjectDistinctDefects); Assert.Equal(2, filtered.Value.Statistics.MatchedDistinctDefects); Assert.Empty(filtered.Value.Statistics.Quantities);
        var repairStock = await new CurrentRepairFactsRepository(db, TimeProvider.System).CaptureAsync(state.Source.Pm,
            state.Source.Project, new(SegmentIds: [state.Segments[1]]), default);
        Assert.Equal(scope.ObligationId, Assert.Single(repairStock.Items).ObligationId);
        Assert.Empty(repairStock.MissingReasons);
        var frozen = JsonSerializer.Serialize(statistics, Json); var old = first.Value.History.Single();
        var current = await repo.ReadAsync(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, new(), default);
        var unknown = current.Value!.Measurements.Single(row => row.Id == state.Unknown);
        // A new explicit physical identity with different actual scope cannot borrow another part's measured value.
        Assert.Equal(409, (await repo.ConfirmAsync(command with
        {
            Key = Guid.NewGuid().ToString(),
            ExpectedVersion = current.Value.Version,
            Input = input with { SharedPartId = Guid.NewGuid(), SupersedesId = old.Id }
        }, default)).Status);
        // Explicit allocation supersession updates the single shared source and all dependent segment projections.
        var unallocatedInput = input with { Quantities = [new(measurement.Id, measurement.Version, null)], SupersedesId = old.Id };
        var revised = await repo.ConfirmAsync(command with { Key = Guid.NewGuid().ToString(), ExpectedVersion = current.Value.Version, Input = unallocatedInput }, default);
        Assert.Equal(201, revised.Status); Assert.Null(Assert.Single(revised.Value!.Statistics.Quantities).SegmentId); Assert.Equal(12m, revised.Value.Statistics.Quantities[0].Value);
        Assert.Equal(2, revised.Value.Statistics.ProjectDistinctDefects); Assert.Equal(1, revised.Value.Statistics.SharedParts);
        Assert.Equal(frozen, JsonSerializer.Serialize(statistics, Json));
        var captured = DefectStatisticsCaptureConsumer.Apply(new(new("test", state.Source.Project, DateTimeOffset.UtcNow, "current", new(), [], [], "SERIALIZABLE"), [], [], [], new("AVAILABLE", [])), statistics);
        Assert.Equal(2, Assert.Single(captured.Summary.Metrics.Where(row => row.Code == "projectDistinctDefects")).Value);
        Assert.Equal(12, Assert.Single(captured.Summary.Metrics.Where(row => row.Code == "sourcedAllocatedQuantity")).Value);
        Assert.Equal(statistics, ((RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingCaptureFact)captured).DefectStatistics);
        var blocked = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DefectStatisticsSources SET Reason='changed' WHERE Id={old.Id}")); Assert.Equal(51800, blocked.Number);
        blocked = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261007131646_OwnerRoadCoverageActivation")); Assert.Equal(51890, blocked.Number);
        await db.ProjectMembers.Where(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Pm).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(403, (await repo.ConfirmAsync(command, default)).Status);
        Assert.Equal(403, (await repo.ReadAsync(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, new(), default)).Status);
    }
    [Fact]
    public async Task UnallocatedUnknownAndImmutableMeasurementsStayDistinctWhenGeometrySourcesBecomeStale()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db); var repo = new DefectStatisticsRepository(db, TimeProvider.System);
        var read = (await repo.ReadAsync(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, new(), default)).Value!;
        var scope = read.Scopes.Single(row => row.DefectId == state.Source.Defect); var area = read.Measurements.Single(row => row.Id == state.Area);
        var unknown = read.Measurements.Single(row => row.Id == state.Unknown);
        var input = new DefectStatisticsInput(scope.ObligationId, scope.ScopeHash, scope.DefectVersion, Guid.NewGuid(), state.Segments,
            [new(area.Id, area.Version, null), new(unknown.Id, unknown.Version, state.Segments[1])], "TEST_ONLY", "TEST_ONLY sourced quantities");
        var command = new DefectStatisticsCommand(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, input, Guid.NewGuid().ToString(), read.Version);
        var confirmed = await repo.ConfirmAsync(command, default); Assert.Equal(201, confirmed.Status);
        var known = Assert.Single(confirmed.Value!.Statistics.Quantities.Where(row => row.ValueState == "KNOWN")); Assert.Null(known.SegmentId); Assert.Equal(12m, known.Value);
        var missing = Assert.Single(confirmed.Value.Statistics.Quantities.Where(row => row.ValueState == "UNKNOWN")); Assert.Null(missing.Value); Assert.Equal("TEST_ONLY unmeasured depth", missing.Reason);
        Assert.Contains("QUANTITY_VALUE_UNKNOWN", confirmed.Value.Statistics.MissingReasons);
        var immutable = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE GroundTruthMeasurements SET Value=99 WHERE Id={state.Area}"));
        Assert.Contains("immutable", immutable.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(12m, await db.GroundTruthMeasurements.AsNoTracking().Where(row => row.Id == state.Area).Select(row => row.Value).SingleAsync());
        var set = await db.RoadSegmentSets.SingleAsync(row => row.Id == state.Source.Set); set.Supersede(); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(409, (await repo.ConfirmAsync(command, default)).Status);
        var stale = await repo.ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, new(), default);
        Assert.Contains("STATISTICS_SOURCE_STALE", stale.Value!.Statistics.MissingReasons); Assert.Empty(stale.Value.Statistics.Quantities);
        Assert.Equal(2, stale.Value.Statistics.UnmappedDefects);
    }
    [Fact]
    public async Task ReceiptRollbackConcurrentReplayAndEmptyMigrationReapplyPreserveSourceGraph()
    {
        await using var isolated = new IdentitySqlServerFixture(); await isolated.InitializeAsync();
        await using var db = isolated.CreateDbContext(); var state = await Seed(db);
        await db.GetService<IMigrator>().MigrateAsync("20261007131646_OwnerRoadCoverageActivation"); await db.Database.MigrateAsync();
        Assert.True(await db.Defects.AnyAsync(row => row.Id == state.Source.Defect)); Assert.True(await db.GroundTruthMeasurements.AnyAsync(row => row.Id == state.Area));
        var repo = new DefectStatisticsRepository(db, TimeProvider.System); var read = (await repo.ReadAsync(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, new(), default)).Value!;
        var scope = read.Scopes.Single(row => row.DefectId == state.Source.Defect); var area = read.Measurements.Single(row => row.Id == state.Area);
        var input = new DefectStatisticsInput(scope.ObligationId, scope.ScopeHash, scope.DefectVersion, Guid.NewGuid(), state.Segments, [new(area.Id, area.Version, null)], "TEST_ONLY", "TEST_ONLY concurrency");
        var key = "ld08-rollback-" + Guid.NewGuid(); var command = new DefectStatisticsCommand(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, input, key, read.Version);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER TR_LD08ReceiptRollback ON IdempotencyRecords AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Operation='ld08a.defect-statistics.confirm.v1' AND IdempotencyKey LIKE 'ld08-rollback-%') THROW 51899, 'TEST_ONLY forced rollback', 1; END");
        try { await Assert.ThrowsAnyAsync<Exception>(() => repo.ConfirmAsync(command, default)); }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER TR_LD08ReceiptRollback"); db.ChangeTracker.Clear(); }
        Assert.False(await db.Set<DefectStatisticsSource>().AnyAsync(row => row.ProjectId == state.Source.Project));
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.IdempotencyKey == key));
        Assert.False(await db.AuditLogs.AnyAsync(row => row.ActorUserId == state.Source.Pm && row.EventType == "defect_statistics_confirmed"));
        command = command with { Key = Guid.NewGuid().ToString() };
        async Task<DefectStatisticsResult> Run() { await using var fresh = isolated.CreateDbContext(); return await new DefectStatisticsRepository(fresh, TimeProvider.System).ConfirmAsync(command, default); }
        var outcomes = await Task.WhenAll(Run(), Run()); Assert.Equal(new[] { 200, 201 }, outcomes.Select(row => row.Status).Order().ToArray());
        Assert.Single(await db.Set<DefectStatisticsSource>().Where(row => row.ProjectId == state.Source.Project).ToArrayAsync());
        Assert.Single(await db.OutboxMessages.Where(row => row.MessageType == "defect.statistics.confirmed.v1" && row.CorrelationId == outcomes[0].Value!.History.Single().Id).ToArrayAsync());
    }
    private sealed record State(H4GenuineRepairSource.Source Source, Guid SecondDefect, Guid[] Segments, Guid Area, Guid Unknown);
    private async Task<State> Seed(RoadGuardDbContext db)
    {
        var source = await H4GenuineRepairSource.Seed(db, sql); var now = DateTimeOffset.UtcNow;
        var version = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, version, [new("FORMAL_REPAIR", true, new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "TEST_ONLY actual scope")], "TEST_ONLY package"), Guid.NewGuid().ToString(), version), default); Assert.Equal(201, created.Status);
        var obligation = await db.RepairObligations.Include(row => row.Scope).SingleAsync(row => row.DefectId == source.Defect);
        var original = await db.Defects.SingleAsync(row => row.Id == source.Defect);
        var second = Defect.Create(Guid.NewGuid(), source.Project, source.Route, null, original.DefectTypeCode, null, DefectSeverity.Low, DefectStatus.Open, new NetTopologySuite.Geometries.GeometryFactory(new NetTopologySuite.Geometries.PrecisionModel(), 32648).CreatePoint(new NetTopologySuite.Geometries.Coordinate(1, 0)), now);
        db.Add(second); await db.SaveChangesAsync();
        db.Add(RepairPackage.Create(Guid.NewGuid(), source.Project, second.Id, [RepairObligation.Create(Guid.NewGuid(),source.Project,second.Id,RepairObligationKind.FormalRepair,true,
            RepairActualScope.Create(Guid.NewGuid(),source.Road,obligation.Scope.LocationVersion,"TEST_ONLY",1,2,0,1))]));
        var segment = RoadSegment.Create(Guid.NewGuid(), source.Set, source.Route, 2); db.Add(segment); await db.SaveChangesAsync();
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var package = Assert.IsType<RepairPackageFact>(created.Value);
        var proposed = await repairs.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
            new(obligation.Id, "NORMAL", "TEST_ONLY plan", "TEST_ONLY checklist", "TEST_ONLY proposal"), Guid.NewGuid().ToString(), created.Version!), default); Assert.Equal(201, proposed.Status);
        var item = Assert.IsType<RepairItemFact>(proposed.Value);
        var approved = await repairs.ApproveItemAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project, package.Id, item.Id,
            new("TEST_ONLY actual normal approval"), Guid.NewGuid().ToString(), proposed.Version!), default); Assert.Equal(201, approved.Status);
        var assigned = await repairs.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id, item.Id,
            new(new(source.Defect, version, null, "REPORTER", source.Route, source.Set, null, null, "POST_REPAIR", 1, "{}", null, source.Crew, now.AddDays(1)), null, "TEST_ONLY actual assignment"),
            Guid.NewGuid().ToString(), approved.Version!), default); Assert.Equal(201, assigned.Status);
        var binding = Assert.IsType<RepairTaskBindingFact>(assigned.Value);
        var field = new RoadGuardSystem.Repositories.Implementations.Inspections.FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var native = await db.FieldInspectionTasks.SingleAsync(row => row.Id == binding.TaskId);
        var admission = new RoadGuardSystem.Repositories.Inspections.FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        var accepted = await field.ExecuteAsync(new(source.Project, native.Id, "accept", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("TEST_ONLY accept"),
            Guid.NewGuid().ToString(), Convert.ToBase64String(native.RowVersion), admission), _ => Task.FromResult(true), default); Assert.Equal(201, accepted.Status);
        var nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking().Where(row => row.Id == native.Id).Select(row => row.RowVersion).SingleAsync());
        var started = await field.ExecuteAsync(new(source.Project, native.Id, "start", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow),
            Guid.NewGuid().ToString(), nativeVersion, admission), _ => Task.FromResult(true), default); Assert.Equal(201, started.Status);
        var first = await db.Set<FieldTaskStartOrigin>().SingleAsync(row => row.TaskId == native.Id);
        var itemVersion = Convert.ToBase64String(await db.RepairItems.Where(row => row.Id == item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var assessed = await repairs.AssessAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project, package.Id, item.Id, native.Id,
            new(Guid.NewGuid(), first.Id, [new("TEST_ONLY-area","Area",12,"KNOWN",null,"AREA","m\u00b2",null,null,"TEST_ONLY no CRS assertion","TEST_ONLY","TEST_ONLY explicit measurement","TEST_ONLY measured area source"),
                new("TEST_ONLY-depth","DepressionDepth",null,"UNKNOWN","TEST_ONLY unmeasured depth","LENGTH","mm",null,null,"TEST_ONLY no CRS assertion","TEST_ONLY","TEST_ONLY explicit measurement","TEST_ONLY unknown measurement source")], null, null),
            Guid.NewGuid().ToString(), itemVersion), default); Assert.Equal(201, assessed.Status);
        var area = await db.GroundTruthMeasurements.SingleAsync(row => row.DefectId == source.Defect && row.SampleId == "TEST_ONLY-area");
        var unknown = await db.GroundTruthMeasurements.SingleAsync(row => row.DefectId == source.Defect && row.SampleId == "TEST_ONLY-depth");
        return new(source, second.Id, await db.RoadSegments.Where(row => row.SegmentSetId == source.Set).OrderBy(row => row.Sequence).Select(row => row.Id).ToArrayAsync(), area.Id, unknown.Id);
    }
}
