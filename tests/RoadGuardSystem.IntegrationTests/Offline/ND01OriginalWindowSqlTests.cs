using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Offline;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Offline;
using Xunit;
using RoadGuardSystem.TestFixtures;
using static RoadGuardSystem.TestFixtures.ND01OriginalWindowFixture;

namespace RoadGuardSystem.IntegrationTests.Offline;

public sealed class ND01OriginalWindowSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task SignedOriginalWindowStartCommitsOnceWithoutVerifyingDeviceTimeOrResettingClock()
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db);
        using var keys = state.Keys;
        var result = await Sync(db, state, TimeProvider.System);
        Assert.Equal(200, result.Status);
        var outcome = JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("items")[0];
        Assert.True(outcome.GetProperty("durableAcknowledgment").GetBoolean(), outcome.GetRawText());
        var execution = await db.Set<RepairExecutionStart>().AsNoTracking().SingleAsync(row => row.Id == state.Operation.EffectId);
        Assert.Equal(state.Source.Crew, execution.OriginalActorId);
        Assert.Null(execution.VerifiedOriginalAt); Assert.Equal(RepairTimeProvenance.Uncertain, execution.TimeProvenance);
        Assert.Equal(state.Operation.Repair!.Start!.ClaimedAt, execution.ClaimedAt);
        var replay = await Sync(db, state, new Clock(state.Expiry.AddSeconds(1)));
        Assert.Equal(200, replay.Status);
        var replayItem = JsonSerializer.SerializeToElement(replay.Value, Json).GetProperty("items")[0];
        Assert.True(replayItem.GetProperty("durableAcknowledgment").GetBoolean());
        Assert.Equal(state.Operation.EffectId, replayItem.GetProperty("effectId").GetGuid());
        var member = await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Crew);
        member.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Sync(db, state, TimeProvider.System)).Status);
        Assert.Equal(1, await db.Set<RepairExecutionStart>().CountAsync(row => row.ItemId == state.Item));
        var authorization = await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleAsync(row => row.Id == state.Authorization);
        Assert.Equal(state.First.Id, authorization.FirstStartOriginId); Assert.Equal(state.Expiry, authorization.ExpiresAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ExpiryAndRetriesCannotBackdateOrReopenOriginalEntitlement(int secondsAfterExpiry)
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db); using var keys = state.Keys;
        var clock = new Clock(state.Expiry.AddSeconds(secondsAfterExpiry));
        await AssertDenied(db, state, clock, "fast_track_offline_authority_not_activated");
        await AssertDenied(db, state, clock, "fast_track_offline_authority_not_activated");
        var auth = await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleAsync(row => row.Id == state.Authorization);
        Assert.Equal(state.Expiry, auth.ExpiresAt); Assert.Equal(state.First.Id, auth.FirstStartOriginId);
    }

    [Theory]
    [InlineData("backdated")]
    [InlineData("future")]
    [InlineData("unverified-first")]
    public async Task UntrustedTimeCannotEstablishOrExtendExecutionAuthority(string condition)
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db, condition == "unverified-first" ? condition : "valid"); using var keys = state.Keys;
        if (condition != "unverified-first") state = Resign(state, state.Operation.Repair!.Start! with
        { ClaimedAt = condition == "future" ? state.Expiry.AddHours(1) : state.First.VerifiedOriginalAt!.Value.AddSeconds(-1) });
        await AssertDenied(db, state, TimeProvider.System, condition == "unverified-first"
            ? "fast_track_offline_time_unproven" : "fast_track_offline_time_source_conflict");
    }

    [Theory]
    [InlineData("wrong-crew")]
    [InlineData("revoked-crew")]
    [InlineData("wrong-project")]
    [InlineData("wrong-device")]
    [InlineData("revoked-device")]
    [InlineData("stale-assignment")]
    public async Task SignedFtStartRequiresCurrentExactCrewProjectDeviceAndAssignment(string condition)
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db); using var keys = state.Keys;
        Guid? actor = null; Guid? project = null;
        switch (condition)
        {
            case "wrong-crew":
                var other = await H4GenuineRepairSource.Seed(db, sql); actor = other.Crew;
                db.Add(new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = state.Source.Project,
                    UserId = other.Crew,
                    RoleCode = UserRoleCode.RepairCrew,
                    Status = ProjectMemberStatus.Active,
                    ValidFrom = new(2000, 1, 1)
                });
                break;
            case "wrong-project": project = Guid.NewGuid(); break;
            case "wrong-device": state = Resign(state, state.Operation.Repair!.Start! with { DeviceId = Guid.NewGuid() }); break;
            case "revoked-device":
                db.Add(OfflineDeviceRevocation.Record(Guid.NewGuid(), state.Source.Project, state.Device.Id,
                state.Source.Supervisor, DateTimeOffset.UtcNow, "device revoked")); break;
            case "revoked-crew":
                var member = await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Crew);
                member.Status = ProjectMemberStatus.Ended; break;
            case "stale-assignment":
                var assignment = await db.FieldInspectionAssignments.SingleAsync(row => row.Id == state.Operation.AssignmentId);
                db.Entry(assignment).Property(row => row.Status).CurrentValue = FieldInspectionAssignmentStatus.Ended;
                db.Entry(assignment).Property(row => row.EndedAt).CurrentValue = DateTimeOffset.UtcNow; break;
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await Sync(db, state, TimeProvider.System, actor, project);
        if (condition == "stale-assignment")
        {
            Assert.Equal(200, result.Status);
            var outcome = JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("items")[0];
            Assert.Equal("stale_snapshot", outcome.GetProperty("code").GetString());
            Assert.False(outcome.GetProperty("durableAcknowledgment").GetBoolean());
        }
        else Assert.Equal(403, result.Status);
        await AssertNoStart(db, state);
    }

    [Theory]
    [InlineData("test-only")]
    [InlineData("missing")]
    [InlineData("stale")]
    [InlineData("mismatched")]
    [InlineData("policy-revoked")]
    public async Task SignedFtStartCannotBypassCoverageOrPinnedPolicy(string condition)
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db, condition is "test-only" or "missing" or "mismatched" ? condition : "valid"); using var keys = state.Keys;
        if (condition == "stale")
        {
            var warranty = await db.Warranties.SingleAsync(row => row.ProjectId == state.Source.Project);
            db.Entry(warranty).Property(row => row.Terms).CurrentValue = "updated source terms";
        }
        if (condition == "policy-revoked")
        {
            var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.ItemId == state.Item);
            var policies = new RepairPolicyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
            var read = await policies.ExecuteAsync(new(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project,
                "revision-get", binding.PolicyRevisionId, null, null, null, null), default);
            Assert.Equal(200, read.Status);
            Assert.Equal(201, (await policies.ExecuteAsync(new(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project,
                "revoke", binding.PolicyRevisionId, null, "source no longer applicable", Guid.NewGuid().ToString(), read.Value!.Version), default)).Status);
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await AssertDenied(db, state, TimeProvider.System, condition == "test-only" ? "test_only_source_not_executable"
            : condition == "missing" ? "fast_track_coverage_mapping_unknown" : "fast_track_prerequisites_not_met");
    }

    [Fact]
    public async Task ConcurrentSignedRetriesProduceOneStartAndOneAcknowledgment()
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db); using var keys = state.Keys;
        async Task<OfflineWorkflowFact> Run()
        { await using var concurrent = sql.CreateRetryingDbContext(); return await Sync(concurrent, state, TimeProvider.System); }
        var results = await Task.WhenAll(Run(), Run());
        Assert.All(results, result =>
        {
            Assert.Equal(200, result.Status); Assert.True(JsonSerializer.SerializeToElement(result.Value, Json)
            .GetProperty("items")[0].GetProperty("durableAcknowledgment").GetBoolean());
        });
        Assert.Equal(1, await db.Set<RepairExecutionStart>().CountAsync(row => row.ItemId == state.Item));
        Assert.Equal(1, await db.Set<OfflineOperationResult>().CountAsync(row => row.ProjectId == state.Source.Project && row.DurableAck));
    }

    [Fact]
    public async Task FailureAfterExecutionWritesRollsBackStartOriginHistoryAndAcknowledgment()
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db); using var keys = state.Keys;
        var auditCount = await db.AuditLogs.CountAsync(row => row.EntityId == state.Item);
        await using (var failing = sql.CreateDbContext(new RejectExecutionAcknowledgment()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => Sync(failing, state, TimeProvider.System));
        await AssertNoStart(db, state);
        Assert.Equal(auditCount, await db.AuditLogs.CountAsync(row => row.EntityId == state.Item));
        Assert.False(await db.Set<FieldInspectionOperationOrigin>().AnyAsync(row => row.OriginId == state.Operation.OriginId));
        Assert.False(await db.Set<RepairEligibilityAssessment>().AnyAsync(row => row.ItemId == state.Item));
        Assert.False(await db.Set<IdempotencyRecord>().AnyAsync(row => row.OperationId == state.Operation.EffectId));
        Assert.True(JsonSerializer.SerializeToElement((await Sync(db, state, TimeProvider.System)).Value, Json)
            .GetProperty("items")[0].GetProperty("durableAcknowledgment").GetBoolean());
    }

    [Fact]
    public async Task ImmutablePolicyPinAndMismatchedFirstSourceCannotAuthorizeSignedStart()
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db); using var keys = state.Keys;
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.ItemId == state.Item);
        db.Entry(binding).Property(row => row.PolicyContentHash).CurrentValue = new string('a', 64);
        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("append-only", failure.InnerException!.Message, StringComparison.Ordinal);
        db.ChangeTracker.Clear();
        state = Resign(state, state.Operation.Repair!.Start! with { FieldFirstStartId = Guid.NewGuid() });
        await AssertDenied(db, state, TimeProvider.System, "assessment_source_conflict");
    }

    [Fact]
    public async Task ExpiredFtWindowStillRetainsEvidenceAndAcceptsFormalDataSubmission()
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db); using var keys = state.Keys;
        var clock = new Clock(state.Expiry.AddSeconds(1));
        await AssertDenied(db, state, clock, "fast_track_offline_authority_not_activated");
        var original = await db.Set<RepairMeasurementAssessment>().AsNoTracking().Include(row => row.Evidence)
            .SingleAsync(row => row.Id == state.Operation.Repair!.Start!.AssessmentId);
        var evidence = Assert.Single(original.Evidence);
        Assert.True(await db.Files.AnyAsync(row => row.Id == evidence.FileId));
        var native = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == state.Operation.TaskId);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), clock);
        var submitted = await field.ExecuteAsync(new(state.Source.Project, native.Id, "submit",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionInputFact(Guid.NewGuid(), state.First.Id,
                null, null, null, null, "REPAIR_CLAIM", false, "execution entitlement expired"),
            Guid.NewGuid().ToString(), Convert.ToBase64String(native.RowVersion),
            new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Crew, "DIRECT", true)),
            async token => await new ProjectScopeGuard(new ProjectMembershipReadModel(db), clock)
                .AuthorizeAsync(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project, token) is not null, default);
        Assert.Equal(201, submitted.Status);
        Assert.Equal(1, await db.Set<FieldInspectionSubmission>().CountAsync(row => row.TaskId == native.Id));
        Assert.Equal(original.ContentHash, (await db.Set<RepairMeasurementAssessment>().AsNoTracking().SingleAsync(row => row.Id == original.Id)).ContentHash);
        await AssertNoStart(db, state);
        Assert.Equal(state.Expiry, (await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleAsync(row => row.Id == state.Authorization)).ExpiresAt);
    }

    [Fact]
    public async Task ExpiryCrossedDuringAdmissionRollsBackEligibilityAndStart()
    {
        await using var db = sql.CreateDbContext(); var state = await Prepare(db); using var keys = state.Keys;
        var clock = new MovingClock(DateTimeOffset.UtcNow);
        await using var crossing = sql.CreateDbContext(new CrossExpiry(clock, state.Expiry));
        await AssertDenied(crossing, state, clock, "fast_track_offline_authority_not_activated");
        Assert.False(await db.Set<RepairEligibilityAssessment>().AnyAsync(row => row.ItemId == state.Item));
        Assert.False(await db.Set<FieldInspectionOperationOrigin>().AnyAsync(row => row.OriginId == state.Operation.OriginId));
    }
    private sealed class MovingClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class CrossExpiry(MovingClock clock, DateTimeOffset expiry) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<RepairEligibilityAssessment>().Any(row => row.State == EntityState.Added))
                clock.Now = expiry;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class RejectExecutionAcknowledgment : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<OfflineOperationResult>().Any(row => row.State == EntityState.Added && row.Entity.DurableAck))
                throw new InvalidOperationException("ND01 injected acknowledgment persistence failure");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
    private static State Resign(State state, RepairExecutionStartInput input)
    {
        var payload = state.Operation.Repair! with { Start = input };
        var operation = state.Operation with { Repair = payload, SourceDeviceId = input.DeviceId!.Value, CorePayloadHash = RepairCoreHash.ExecutionStart(state.Operation.TaskId, state.Item, state.Source.Crew, input) };
        var manifest = OfflineWorkflowEngine.CanonicalManifest(state.Source.Project, state.Batch.BatchId, state.Device.Id, [OfflineWorkflowEngine.Describe(operation)]);
        return state with { Operation = operation, Batch = state.Batch with { Operations = [operation], Signature = OfflinePackageAuthentication.SignClaim(manifest, state.Keys) } };
    }
    private static async Task AssertDenied(RoadGuardDbContext db, State state, TimeProvider clock, string code)
    {
        var result = await Sync(db, state, clock); Assert.Equal(200, result.Status);
        var outcome = JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("items")[0];
        Assert.False(outcome.GetProperty("durableAcknowledgment").GetBoolean()); Assert.Equal(code, outcome.GetProperty("code").GetString());
        await AssertNoStart(db, state);
    }
    private static async Task AssertNoStart(RoadGuardDbContext db, State state)
    {
        Assert.False(await db.Set<RepairExecutionStart>().AnyAsync(row => row.ItemId == state.Item));
        Assert.False(await db.Set<OfflineOperationResult>().AnyAsync(row => row.ProjectId == state.Source.Project && row.DurableAck));
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
    private static Task<OfflineWorkflowFact> Sync(RoadGuardDbContext db, State state, TimeProvider clock, Guid? actor = null, Guid? project = null)
        => ND01OriginalWindowFixture.Offline(db, clock).ExecuteAsync(new(actor ?? state.Source.Crew, UserRoleCode.RepairCrew, project ?? state.Source.Project, "sync",
            OfflineContractMapping.ToData("sync", state.Batch), null, Guid.NewGuid().ToString(), null),
            OfflineContractMapping.RepositoryAlgorithms, async token => await new ProjectScopeGuard(new ProjectMembershipReadModel(db), clock)
                .AuthorizeAsync(actor ?? state.Source.Crew, UserRoleCode.RepairCrew, project ?? state.Source.Project, token) is not null, default);
    private async Task<State> Prepare(RoadGuardDbContext db, string condition = "valid")
    {
        var source = await H4GenuineRepairSource.Seed(db, sql);
        return await ND01OriginalWindowFixture.Prepare(db, new(source.Pm, source.Crew, source.Supervisor,
            source.Project, source.Road, source.Route, source.Set, source.Defect), condition);
    }
}
