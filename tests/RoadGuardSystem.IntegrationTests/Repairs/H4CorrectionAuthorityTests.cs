using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed partial class H4CorrectionAuthorityTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).AddInterceptors(interceptors).Options);

    [Fact]
    public async Task DifferentCurrentPmCorrectsFastTrackWithDurableHeadsAndReceipt()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.FastTrack, UserRoleCode.ProjectManager);
        var result = await Repository(db).CorrectAsync(Command(facts), CancellationToken.None);
        Assert.Equal(201, result.Status); db.ChangeTracker.Clear();
        var item = await db.Set<RepairItem>().Include(row => row.Decisions).SingleAsync(row => row.Id == facts.Item);
        var obligation = await db.Set<RepairObligation>().Include(row => row.ResolutionHistory).SingleAsync(row => row.Id == facts.Obligation);
        Assert.Equal(RepairItemState.CorrectionRequired, item.State); Assert.NotEqual(facts.OriginalDecision, item.EffectiveDecisionId);
        Assert.Equal(facts.Actor, item.EffectiveDecision!.ActorId); Assert.Equal(facts.OriginalDecision, item.EffectiveDecision.SupersedesDecisionId);
        Assert.Null(obligation.EffectiveResolutionDecisionId); Assert.Equal(item.EffectiveDecisionId, obligation.EffectiveResolutionHeadDecisionId);
        Assert.Equal(2, obligation.ResolutionHistory.Count); Assert.Equal(2, item.Decisions.Count);
        Assert.Equal(RepairPresentationState.Confirmed, item.Decisions.Single(row => row.Id == facts.OriginalDecision).Result);
        Assert.Single(await db.IdempotencyRecords.Where(row => row.ProjectId == facts.Project).ToListAsync());
        Assert.Single(await db.AuditLogs.Where(row => row.EntityId == facts.Item && row.EventType == "repair_decision_corrected").ToListAsync());
    }

    [Theory]
    [InlineData(RepairMode.FastTrack, UserRoleCode.Supervisor)]
    [InlineData(RepairMode.Normal, UserRoleCode.ProjectManager)]
    public async Task CurrentMembershipCannotOverrideModeAuthority(RepairMode mode, UserRoleCode role)
    {
        await using var db = Db(); var facts = await Seed(db, mode, role);
        Assert.Equal(403, (await Repository(db).CorrectAsync(Command(facts), CancellationToken.None)).Status);
        Assert.Equal(facts.OriginalDecision, await db.Set<RepairItem>().Where(row => row.Id == facts.Item).Select(row => row.EffectiveDecisionId).SingleAsync());
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ProjectId == facts.Project));
    }

    [Fact]
    public async Task GlobalSupervisorWithoutProjectMembershipCannotCorrectNormalDecision()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.Normal, UserRoleCode.Supervisor, membership: false);
        Assert.Equal(403, (await Repository(db).CorrectAsync(Command(facts), CancellationToken.None)).Status);
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ProjectId == facts.Project));
    }

    [Fact]
    public async Task RevocationDeniesReplayAndConflictingKeyWithoutRevealingStoredOutcome()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.Normal, UserRoleCode.Supervisor);
        var repository = Repository(db); Assert.Equal(201, (await repository.CorrectAsync(Command(facts), CancellationToken.None)).Status);
        db.ChangeTracker.Clear(); var member = await db.ProjectMembers.SingleAsync(row => row.Id == facts.Membership);
        member.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var replay = await repository.CorrectAsync(Command(facts), CancellationToken.None);
        var conflict = await repository.CorrectAsync(Command(facts) with { Input = Input(facts) with { Reason = "changed payload" } }, CancellationToken.None);
        Assert.Equal(403, replay.Status); Assert.Null(replay.Value); Assert.Equal(403, conflict.Status); Assert.Null(conflict.Value);
        Assert.Equal(2, await db.Set<RepairDecision>().CountAsync(row => row.ItemId == facts.Item));
    }

    [Fact]
    public async Task ChangedResourceCannotLaunderStoredFastTrackReceiptThroughNewSupervisorAuthority()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.FastTrack, UserRoleCode.ProjectManager);
        var repository = Repository(db); Assert.Equal(201, (await repository.CorrectAsync(Command(facts), CancellationToken.None)).Status);
        db.ChangeTracker.Clear(); var other = await OtherNormalItem(db, facts);
        var roleCode = UserRoleCode.Supervisor.ToDbCode();
        // Actual current authority rows change; no injected permission or remembered original actor is used.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [RoleCode]={roleCode} WHERE [Id]={facts.Actor}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ProjectMembers] SET [RoleCode]={roleCode} WHERE [Id]={facts.Membership}");
        db.ChangeTracker.Clear();
        var changed = Command(facts) with
        {
            Role = UserRoleCode.Supervisor,
            PackageId = other.Package,
            ItemId = other.Item,
            ExpectedVersion = other.Version,
            Input = new(other.Decision, "UNREPAIRED", "new normal item", new("current evidence basis", []))
        };
        var result = await repository.CorrectAsync(changed, CancellationToken.None);
        Assert.Equal(403, result.Status); Assert.Null(result.Value);
        Assert.Equal(other.Decision, await db.Set<RepairItem>().Where(row => row.Id == other.Item).Select(row => row.EffectiveDecisionId).SingleAsync());
        Assert.Single(await db.IdempotencyRecords.Where(row => row.ProjectId == facts.Project).ToListAsync());
    }

    [Fact]
    public async Task MissingObligationHeadIsFiniteConflictWithoutPartialDecision()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.FastTrack, UserRoleCode.ProjectManager);
        // Controlled inconsistent legacy state exercises fail-closed admission; this is not an authorized reopen producer.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET EffectiveResolutionDecisionId=NULL,EffectiveResolutionHeadDecisionId=NULL WHERE Id={facts.Obligation}");
        var result = await Repository(db).CorrectAsync(Command(facts), CancellationToken.None);
        Assert.Equal(409, result.Status); Assert.Equal("decision_head_conflict", result.Code);
        Assert.Single(await db.Set<RepairDecision>().Where(row => row.ItemId == facts.Item).ToListAsync());
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ProjectId == facts.Project));
    }

    [Fact]
    public async Task SameKeyMissBeforeAuthorityLockConvergesOnOneCommittedReplay()
    {
        Facts facts; await using (var setup = Db()) facts = await Seed(setup, RepairMode.Normal, UserRoleCode.Supervisor);
        var barrier = new FirstReceiptReadBarrier(); await using var first = Db(barrier); await using var second = Db(barrier);
        var results = await Task.WhenAll(Repository(first).CorrectAsync(Command(facts), CancellationToken.None),
            Repository(second).CorrectAsync(Command(facts), CancellationToken.None));
        Assert.Equal(new[] { 200, 201 }, results.Select(row => row.Status).Order().ToArray());
        Assert.Single(results.Where(row => row.Replayed));
        await using var verify = Db(); Assert.Equal(2, await verify.Set<RepairDecision>().CountAsync(row => row.ItemId == facts.Item));
        Assert.Single(await verify.IdempotencyRecords.Where(row => row.ProjectId == facts.Project).ToListAsync());
    }

    [Fact]
    public async Task DifferentKeysCompetingForSameHeadCannotBothChangeDecision()
    {
        Facts facts; await using (var setup = Db()) facts = await Seed(setup, RepairMode.Normal, UserRoleCode.Supervisor);
        var barrier = new FirstReceiptReadBarrier(); await using var first = Db(barrier); await using var second = Db(barrier);
        var results = await Task.WhenAll(Repository(first).CorrectAsync(Command(facts), CancellationToken.None),
            Repository(second).CorrectAsync(Command(facts) with { Key = "different-correction-key" }, CancellationToken.None));
        Assert.Equal(new[] { 201, 409 }, results.Select(row => row.Status).Order().ToArray());
        await using var verify = Db(); Assert.Equal(2, await verify.Set<RepairDecision>().CountAsync(row => row.ItemId == facts.Item));
        Assert.Single(await verify.IdempotencyRecords.Where(row => row.ProjectId == facts.Project).ToListAsync());
    }

    [Fact]
    public async Task ReceiptFailureRollsBackDecisionHeadsPackageDefectAuditAndOutbox()
    {
        Facts facts; Guid defect; long revision;
        await using (var setup = Db())
        {
            facts = await Seed(setup, RepairMode.FastTrack, UserRoleCode.ProjectManager);
            defect = await setup.Set<RepairItem>().Where(row => row.Id == facts.Item).Select(row => row.DefectId).SingleAsync();
            await setup.Database.ExecuteSqlInterpolatedAsync($"UPDATE Defects SET Status=4 WHERE Id={defect}");
            revision = await setup.Set<RepairPackage>().Where(row => row.Id == facts.Package).Select(row => EF.Property<long>(row, "MutationRevision")).SingleAsync();
        }
        await using (var failed = Db(new FailCorrectionReceipt()))
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Repository(failed).CorrectAsync(Command(facts), CancellationToken.None));
            Assert.Equal("Controlled precommit correction receipt failure.", error.Message);
        }
        await using var verify = Db();
        Assert.Equal(facts.OriginalDecision, await verify.Set<RepairItem>().Where(row => row.Id == facts.Item).Select(row => row.EffectiveDecisionId).SingleAsync());
        Assert.Equal(facts.OriginalDecision, await verify.Set<RepairObligation>().Where(row => row.Id == facts.Obligation).Select(row => row.EffectiveResolutionDecisionId).SingleAsync());
        Assert.Equal(revision, await verify.Set<RepairPackage>().Where(row => row.Id == facts.Package).Select(row => EF.Property<long>(row, "MutationRevision")).SingleAsync());
        Assert.Equal(DefectStatus.Resolved, await verify.Defects.Where(row => row.Id == defect).Select(row => row.Status).SingleAsync());
        Assert.Single(await verify.Set<RepairDecision>().Where(row => row.ItemId == facts.Item).ToListAsync());
        Assert.Single(await verify.Set<RepairObligation>().AsNoTracking().Where(row => row.Id == facts.Obligation).SelectMany(row => row.ResolutionHistory).ToListAsync());
        Assert.False(await verify.AuditLogs.AnyAsync(row => row.EntityId == facts.Item && row.EventType == "repair_decision_corrected"));
        Assert.False(await verify.OutboxMessages.AnyAsync(row => row.MessageType == "repair.decision.corrected.v1"));
        Assert.False(await verify.IdempotencyRecords.AnyAsync(row => row.ProjectId == facts.Project));
    }

    [Fact]
    public async Task UnresolvedMandatoryCorrectionInvalidatesResolvedDefectWithoutProjectCascade()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.FastTrack, UserRoleCode.ProjectManager);
        var item = await db.Set<RepairItem>().AsNoTracking().SingleAsync(row => row.Id == facts.Item);
        var projectStatus = await db.Projects.Where(row => row.Id == facts.Project).Select(row => row.Status).SingleAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Defects SET Status=4 WHERE Id={item.DefectId}");
        Assert.True((await db.Set<RepairPackage>().AsNoTracking().Include(row => row.Obligations).SingleAsync(row => row.Id == facts.Package)).IsComplete);
        Assert.Equal(201, (await Repository(db).CorrectAsync(Command(facts), CancellationToken.None)).Status); db.ChangeTracker.Clear();
        Assert.Equal(DefectStatus.Open, await db.Defects.Where(row => row.Id == item.DefectId).Select(row => row.Status).SingleAsync());
        Assert.Equal(projectStatus, await db.Projects.Where(row => row.Id == facts.Project).Select(row => row.Status).SingleAsync());
        Assert.False((await db.Set<RepairPackage>().AsNoTracking().Include(row => row.Obligations).SingleAsync(row => row.Id == facts.Package)).IsComplete);
        var audit = await db.AuditLogs.SingleAsync(row => row.EntityId == facts.Item && row.EventType == "repair_decision_corrected");
        Assert.Contains("UNKNOWN_LEGACY_SOURCE", audit.AfterSnapshot!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AcknowledgementLossRecoversOneCommittedDecisionAndReplay()
    {
        Facts facts; await using (var setup = Db()) facts = await Seed(setup, RepairMode.Normal, UserRoleCode.Supervisor);
        await using var db = Db(new FailFirstCorrectionAcknowledgement());
        var result = await Repository(db).CorrectAsync(Command(facts), CancellationToken.None);
        Assert.Equal(200, result.Status); Assert.True(result.Replayed);
        await using var verify = Db(); Assert.Equal(2, await verify.Set<RepairDecision>().CountAsync(row => row.ItemId == facts.Item));
        Assert.Single(await verify.IdempotencyRecords.Where(row => row.ProjectId == facts.Project).ToListAsync());
    }

    [Fact]
    public async Task RevocationAfterDurableCommitDeniesRecoveredOutcome()
    {
        Facts facts; await using (var setup = Db()) facts = await Seed(setup, RepairMode.Normal, UserRoleCode.Supervisor);
        var fault = new FailFirstCorrectionAcknowledgement(async () =>
        {
            await using var revoke = Db(); var member = await revoke.ProjectMembers.SingleAsync(row => row.Id == facts.Membership);
            member.Status = ProjectMemberStatus.Ended; await revoke.SaveChangesAsync();
        });
        await using var db = Db(fault); var result = await Repository(db).CorrectAsync(Command(facts), CancellationToken.None);
        Assert.Equal(403, result.Status); Assert.Null(result.Value);
        await using var verify = Db(); Assert.Equal(2, await verify.Set<RepairDecision>().CountAsync(row => row.ItemId == facts.Item));
        Assert.Single(await verify.IdempotencyRecords.Where(row => row.ProjectId == facts.Project).ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredOrFutureMembershipCannotCorrectEvenWithMatchingRole(bool future)
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.Normal, UserRoleCode.Supervisor);
        var member = await db.ProjectMembers.SingleAsync(row => row.Id == facts.Membership);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (future) member.ValidFrom = today.AddDays(1); else member.ValidTo = today.AddDays(-1);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Repository(db).CorrectAsync(Command(facts), CancellationToken.None)).Status);
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ProjectId == facts.Project));
    }

    private sealed class FirstReceiptReadBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int count;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("[IdempotencyRecords]", StringComparison.Ordinal) && Interlocked.Increment(ref count) <= 2)
            {
                if (Volatile.Read(ref count) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }
    private sealed class FailCorrectionReceipt : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<IdempotencyRecord>().Any(row => row.State == EntityState.Added && row.Entity.Operation == "h4.repair.correct.v1"))
                throw new InvalidOperationException("Controlled precommit correction receipt failure.");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class FailFirstCorrectionAcknowledgement(Func<Task>? afterCommit = null) : DbTransactionInterceptor
    {
        private int count;
        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref count) != 1) return;
            if (afterCommit is not null) await afterCommit();
            throw new InvalidOperationException("Controlled correction acknowledgement loss after durable commit.");
        }
    }

    private static RepairWorkflowRepository Repository(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), TimeProvider.System);
    private static RepairCorrectionData Input(Facts facts) => new(facts.OriginalDecision, "UNREPAIRED", "new observed correction", new("inspection establishes unresolved work", []));
    private static RepairCorrectionCommand Command(Facts facts) => new(facts.Actor, facts.Role, facts.Project,
        facts.Package, facts.Item, Input(facts), "correction-fixture-1", facts.Version);
    private sealed record Facts(Guid Project, Guid Package, Guid Item, Guid Obligation, Guid OriginalDecision,
        Guid Actor, UserRoleCode Role, Guid Membership, string Version);
    private sealed record OtherItem(Guid Package, Guid Item, Guid Decision, string Version);

    private static async Task<OtherItem> OtherNormalItem(RoadGuardDbContext db, Facts facts)
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-2);
        if (!await db.Roles.AnyAsync(row => row.Code == UserRoleCode.Supervisor)) db.Add(new ApplicationRole(UserRoleCode.Supervisor, "Supervisor"));
        var historicalActor = User(UserRoleCode.Supervisor, now); db.Add(historicalActor); await db.SaveChangesAsync();
        var original = await db.Set<RepairItem>().AsNoTracking().SingleAsync(row => row.Id == facts.Item);
        var defect = await db.Defects.AsNoTracking().SingleAsync(row => row.Id == original.DefectId);
        var road = await db.RoadSectionVersions.Where(row => row.Id == defect.RoadSectionVersionId).Select(row => row.RoadSectionId).SingleAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), facts.Project, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road, "actual-source-frame-v1", "R", 3, 4, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), facts.Project, defect.Id, [obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.Normal, facts.Actor,
            UserRoleCode.ProjectManager, now, new("second controlled plan", "checklist-v1"));
        package.AddItem(item); db.Add(package); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); var decision = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairDecisions (Id,ItemId,ObligationId,DefectId,Mode,ActorId,Role,Reason,At,Result) VALUES ({decision},{item.Id},{obligation.Id},{defect.Id},1,{historicalActor.Id},1,'controlled original acceptance',{now},3)");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET EffectiveDecisionId={decision},State=7 WHERE Id={item.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET EffectiveResolutionHeadDecisionId={decision},EffectiveResolutionDecisionId={decision} WHERE Id={obligation.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairObligationResolutionEvents (DecisionId,Accepted,At,ObligationId) VALUES ({decision},1,{now},{obligation.Id})");
        var version = await db.Set<RepairItem>().Where(row => row.Id == item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync();
        return new(package.Id, item.Id, decision, Convert.ToBase64String(version));
    }

    private static async Task<Facts> Seed(RoadGuardDbContext db, RepairMode mode, UserRoleCode currentRole, bool membership = true)
    {
        await db.Database.MigrateAsync(); var now = DateTimeOffset.UtcNow.AddMinutes(-5);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Actual correction scope", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "R"); var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "controlled SQL identity; no geospatial accuracy assertion");
        var type = DefectType.Create("H4" + Guid.NewGuid().ToString("N"), "Controlled correction source");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low,
            DefectStatus.Open, geometry.CreatePoint(new Coordinate(5, 0)), now);
        var originalRole = mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager;
        foreach (var role in new[] { currentRole, originalRole, UserRoleCode.ProjectManager }.Distinct())
            if (!await db.Roles.AnyAsync(row => row.Code == role)) db.Add(new ApplicationRole(role, role.ToString()));
        var actor = User(currentRole, now); var original = User(originalRole, now); var pm = User(UserRoleCode.ProjectManager, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = actor.Id,
            RoleCode = currentRole,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        db.AddRange(project, road, route, type, defect, actor, original, pm); if (membership) db.Add(member); await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "actual-source-frame-v1", "R", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, mode, pm.Id, UserRoleCode.ProjectManager, now, new("controlled assigned plan", "checklist-v1"));
        package.AddItem(item); db.Add(package); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); var decision = Guid.NewGuid();
        // Controlled historical accepted facts isolate the real correction producer; they grant no production execution rights.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairDecisions (Id,ItemId,ObligationId,DefectId,Mode,ActorId,Role,Reason,At,Result) VALUES ({decision},{item.Id},{obligation.Id},{defect.Id},{(byte)mode},{original.Id},{(byte)originalRole},'controlled original acceptance',{now},3)");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET EffectiveDecisionId={decision},State=7 WHERE Id={item.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET EffectiveResolutionHeadDecisionId={decision},EffectiveResolutionDecisionId={decision} WHERE Id={obligation.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairObligationResolutionEvents (DecisionId,Accepted,At,ObligationId) VALUES ({decision},1,{now},{obligation.Id})");
        var version = await db.Set<RepairItem>().Where(row => row.Id == item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync();
        return new(project.Id, package.Id, item.Id, obligation.Id, decision, actor.Id, currentRole, member.Id, Convert.ToBase64String(version));
    }
    private static ApplicationUser User(UserRoleCode role, DateTimeOffset now)
    {
        var name = Guid.NewGuid().ToString();
        return new()
        {
            Id = Guid.NewGuid(),
            UserName = name,
            NormalizedUserName = name.ToUpperInvariant(),
            DisplayName = "SQL correction actor",
            RoleCode = role,
            PasswordHash = "controlled-SQL-fixture-no-login",
            CreatedAt = now
        };
    }
}
