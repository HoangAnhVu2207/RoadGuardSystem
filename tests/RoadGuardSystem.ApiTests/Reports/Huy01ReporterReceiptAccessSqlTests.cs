using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Reports;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed class Huy01ReporterReceiptAccessSqlTests(AuthenticationSqlServerFixture fixture)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task OrdinaryReplay_AndChangedPayloadConflict_KeepOneScopedIntakeGraph()
    {
        var setup = await CreateReceiptAsync();
        var beforeReplay = await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId);

        await using var replayDb = fixture.CreateDbContext();
        var service = CreateService(replayDb, CreateProducer(replayDb));
        var replay = await service.CreateAsync(setup.Reporter.Id, UserRoleCode.Reporter, setup.Request, setup.Key, null);
        var conflict = await service.CreateAsync(setup.Reporter.Id, UserRoleCode.Reporter,
            setup.Request with { Description = "Changed payload must not replace receipt" }, setup.Key, null);

        Assert.Equal(ReporterReportCommandStatus.Replayed, replay.Status);
        Assert.Equal(setup.ReportId, replay.Report!.Id);
        Assert.Equal(ReporterReportCommandStatus.IdempotencyConflict, conflict.Status);
        Assert.Null(conflict.Report);
        Assert.Equal(beforeReplay, await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId));
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData(ActorMutation.Suspended)]
    [InlineData(ActorMutation.RoleChanged)]
    [InlineData(ActorMutation.MustChangePassword)]
    public async Task Replay_ActorAuthorityChangedAfterRealPreflight_DeniesStoredReceiptWithoutNewEffects(ActorMutation mutation)
    {
        var setup = await CreateReceiptAsync();
        var beforeReplay = await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId);
        await using var replayDb = fixture.CreateDbContext();
        var replay = await CreateService(replayDb, new MutateAfterPreflightProducer(
            CreateProducer(replayDb), () => MutateActorAsync(setup.Reporter.Id, mutation))).CreateAsync(
            setup.Reporter.Id, UserRoleCode.Reporter, setup.Request, setup.Key, null);

        Assert.Equal(ReporterReportCommandStatus.Forbidden, replay.Status);
        Assert.Null(replay.Report);
        Assert.Equal(beforeReplay, await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId));
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData(EvidenceMutation.Pending, ReporterReportCommandStatus.SourceNotReady)]
    [InlineData(EvidenceMutation.Failed, ReporterReportCommandStatus.SourceNotReady)]
    [InlineData(EvidenceMutation.VersionChanged, ReporterReportCommandStatus.StaleFile)]
    public async Task Replay_EvidenceFactsChangedAfterRealPreflight_DeniesStoredReceiptWithoutNewEffects(
        EvidenceMutation mutation, ReporterReportCommandStatus expected)
    {
        var setup = await CreateReceiptAsync();
        var beforeReplay = await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId);
        await using var replayDb = fixture.CreateDbContext();
        var replay = await CreateService(replayDb, new MutateAfterPreflightProducer(
            CreateProducer(replayDb), () => MutateEvidenceAsync(setup.Evidence, mutation))).CreateAsync(
            setup.Reporter.Id, UserRoleCode.Reporter, setup.Request, setup.Key, null);

        Assert.Equal(expected, replay.Status);
        Assert.Null(replay.Report);
        Assert.Equal(beforeReplay, await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId));
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_WithWrongOwnerOrNonPrivateScope_UsesRealProducerAndReturnsPrivacySafeNotFound(bool projectScoped)
    {
        var reporter = await SeedReporterAsync();
        var evidence = projectScoped
            ? await SeedVerifiedEvidenceAsync(reporter.Id, projectScoped: true)
            : await SeedVerifiedEvidenceAsync((await SeedReporterAsync()).Id);
        var key = Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateDbContext();
        var result = await CreateService(db, CreateProducer(db)).CreateAsync(
            reporter.Id, UserRoleCode.Reporter, CreateRequest(evidence), key, null);

        Assert.Equal(ReporterReportCommandStatus.NotFound, result.Status);
        Assert.Null(result.Report);
        Assert.Equal(new ReporterEffectCounts(0, 0, 0, 0, 0),
            await CountEffectsAsync(reporter.Id, key, null));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task Replay_GuardCancellationAfterRealPreflight_DoesNotReturnProtectedReceipt()
    {
        var setup = await CreateReceiptAsync();
        var beforeReplay = await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId);
        using var cancellation = new CancellationTokenSource();
        await using var replayDb = fixture.CreateDbContext();
        var repository = new CancellingAfterCurrentValidationRepository(new ReporterReportRepository(replayDb), cancellation);
        var service = CreateService(replayDb, CreateProducer(replayDb), repository);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateAsync(
            setup.Reporter.Id, UserRoleCode.Reporter, setup.Request, setup.Key, null, cancellation.Token));
        Assert.Equal(1, repository.GuardCalls);
        Assert.Equal(0, repository.CreateCalls);
        Assert.Equal(beforeReplay, await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task Replay_DelegateGuardFailureAfterCurrentValidation_DoesNotReturnReceiptOrRunCreate()
    {
        var setup = await CreateReceiptAsync();
        var beforeReplay = await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId);
        await using var replayDb = fixture.CreateDbContext();
        var repository = new FailingAfterCurrentValidationRepository(new ReporterReportRepository(replayDb));
        var result = await CreateService(replayDb, CreateProducer(replayDb), repository).CreateAsync(
            setup.Reporter.Id, UserRoleCode.Reporter, setup.Request, setup.Key, null);

        Assert.Equal(ReporterReportCommandStatus.Forbidden, result.Status);
        Assert.Null(result.Report);
        Assert.Equal(1, repository.GuardCalls);
        Assert.Equal(0, repository.CreateCalls);
        Assert.Equal(beforeReplay, await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task ExecutionStrategyRetry_FindsReporterReceiptWithoutRunningCreateAgain()
    {
        var setup = await CreateReceiptAsync();
        var beforeReplay = await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId);
        var interceptor = new FailFirstIdempotencyLookupInterceptor();
        await using var replayDb = CreateInterceptedDbContext(interceptor, enableRetry: true);
        var repository = new CountingRepository(new ReporterReportRepository(replayDb));
        var result = await CreateService(replayDb, CreateProducer(replayDb), repository).CreateAsync(
            setup.Reporter.Id, UserRoleCode.Reporter, setup.Request, setup.Key, null);

        Assert.Equal(ReporterReportCommandStatus.Replayed, result.Status);
        Assert.Equal(setup.ReportId, result.Report!.Id);
        Assert.Equal(1, interceptor.FailureCount);
        Assert.True(interceptor.LookupCount >= 2);
        Assert.Equal(1, repository.GuardCalls);
        Assert.Equal(0, repository.CreateCalls);
        Assert.Equal(beforeReplay, await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task ExecutionStrategyRetry_AuthorityRevokedAfterRealPreflight_DeniesRetryReceiptPath()
    {
        var setup = await CreateReceiptAsync();
        var beforeReplay = await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId);
        var interceptor = new FailFirstIdempotencyLookupInterceptor();
        await using var replayDb = CreateInterceptedDbContext(interceptor, enableRetry: true);
        var repository = new CountingRepository(new ReporterReportRepository(replayDb));
        var result = await CreateService(replayDb, new MutateAfterPreflightProducer(
            CreateProducer(replayDb), () => MutateActorAsync(setup.Reporter.Id, ActorMutation.Suspended)), repository).CreateAsync(
            setup.Reporter.Id, UserRoleCode.Reporter, setup.Request, setup.Key, null);

        Assert.Equal(ReporterReportCommandStatus.Forbidden, result.Status);
        Assert.Null(result.Report);
        Assert.Equal(1, interceptor.FailureCount);
        Assert.True(interceptor.LookupCount >= 2);
        Assert.Equal(1, repository.GuardCalls);
        Assert.Equal(0, repository.CreateCalls);
        Assert.Equal(beforeReplay, await CountEffectsAsync(setup.Reporter.Id, setup.Key, setup.ReportId));
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData(false)] [InlineData(true)]
    public async Task DuplicateKeyRecovery_UsesTwoReporterHandlerAttemptsButPersistsOneIntakeGraph(bool revoke)
    {
        var reporter = await SeedReporterAsync();
        var evidence = await SeedVerifiedEvidenceAsync(reporter.Id);
        var key = Guid.NewGuid().ToString("N");
        var request = CreateRequest(evidence);
        var preflightGate = new TwoParticipantGate();
        var handlerGate = new TwoParticipantGate();
        var uniqueProbe = new UniqueViolationProbe();
        await using var leftDb = CreateInterceptedDbContext(uniqueProbe, enableRetry: false);
        await using var rightDb = CreateInterceptedDbContext(uniqueProbe, enableRetry: false);
        Func<Task>? beforeGuard = revoke ? () => MutateActorAsync(reporter.Id, ActorMutation.Suspended) : null;
        var leftRepository = new CountingRepository(new ReporterReportRepository(leftDb), beforeGuard);
        var rightRepository = new CountingRepository(new ReporterReportRepository(rightDb), beforeGuard);
        var left = CreateService(leftDb, new TwoPhaseProducer(CreateProducer(leftDb), preflightGate, handlerGate), leftRepository)
            .CreateAsync(reporter.Id, UserRoleCode.Reporter, request, key, null);
        var right = CreateService(rightDb, new TwoPhaseProducer(CreateProducer(rightDb), preflightGate, handlerGate), rightRepository)
            .CreateAsync(reporter.Id, UserRoleCode.Reporter, request, key, null);

        var results = await Task.WhenAll(left, right);
        var created = Assert.Single(results.Where(result => result.Status == ReporterReportCommandStatus.Created));
        var recovered = Assert.Single(results.Where(result => result.Status == (revoke ? ReporterReportCommandStatus.Forbidden : ReporterReportCommandStatus.Replayed)));
        if (revoke) Assert.Null(recovered.Report); else Assert.Equal(created.Report!.Id, recovered.Report!.Id);
        Assert.Equal(1, uniqueProbe.Count);
        Assert.Equal(1, leftRepository.GuardCalls + rightRepository.GuardCalls);
        Assert.Equal(2, leftRepository.CreateCalls + rightRepository.CreateCalls);
        Assert.Equal(new ReporterEffectCounts(1, 1, 1, 1, 1),
            await CountEffectsAsync(reporter.Id, key, created.Report!.Id));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task PostCommitAcknowledgementRecovery_ReplaysDurableReporterReceiptWithoutSecondHandler()
    {
        var reporter = await SeedReporterAsync();
        var evidence = await SeedVerifiedEvidenceAsync(reporter.Id);
        var key = Guid.NewGuid().ToString("N");
        var interceptor = new FailOnceAfterCommitInterceptor();
        await using var db = CreateInterceptedDbContext(interceptor, enableRetry: true);
        var repository = new CountingRepository(new ReporterReportRepository(db));
        var result = await CreateService(db, CreateProducer(db), repository).CreateAsync(
            reporter.Id, UserRoleCode.Reporter, CreateRequest(evidence), key, null);

        Assert.Equal(ReporterReportCommandStatus.Replayed, result.Status);
        Assert.Equal(1, interceptor.FailureCount);
        Assert.Equal(1, repository.CreateCalls);
        Assert.Equal(new ReporterEffectCounts(1, 1, 1, 1, 1),
            await CountEffectsAsync(reporter.Id, key, result.Report!.Id));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task PostCommitAcknowledgementRecovery_AuthorityRevokedBeforeDurableLookup_DeniesReceipt()
    {
        var reporter = await SeedReporterAsync();
        var evidence = await SeedVerifiedEvidenceAsync(reporter.Id);
        var key = Guid.NewGuid().ToString("N");
        var interceptor = new RevokeAfterFirstCommitInterceptor(() => MutateActorAsync(reporter.Id, ActorMutation.Suspended));
        await using var db = CreateInterceptedDbContext(interceptor, enableRetry: false);
        var repository = new CountingRepository(new ReporterReportRepository(db));
        var result = await CreateService(db, CreateProducer(db), repository).CreateAsync(
            reporter.Id, UserRoleCode.Reporter, CreateRequest(evidence), key, null);

        Assert.Equal(ReporterReportCommandStatus.Forbidden, result.Status);
        Assert.Null(result.Report);
        Assert.Equal(1, interceptor.FailureCount);
        Assert.Equal(1, repository.CreateCalls);
        Assert.Equal(new ReporterEffectCounts(1, 1, 1, 1, 1),
            await CountEffectsAsync(reporter.Id, key, null));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task PreCommitFailure_RollsBackEveryReporterGraphAttempt()
    {
        var reporter = await SeedReporterAsync();
        var evidence = await SeedVerifiedEvidenceAsync(reporter.Id);
        var key = Guid.NewGuid().ToString("N");
        var interceptor = new AlwaysFailBeforeCommitInterceptor();
        await using var db = CreateInterceptedDbContext(interceptor, enableRetry: true);
        var repository = new CountingRepository(new ReporterReportRepository(db));
        var service = CreateService(db, CreateProducer(db), repository);

        await Assert.ThrowsAsync<RetryLimitExceededException>(() => service.CreateAsync(
            reporter.Id, UserRoleCode.Reporter, CreateRequest(evidence), key, null));

        Assert.Equal(3, interceptor.FailureCount);
        Assert.Equal(3, repository.CreateCalls);
        Assert.Equal(new ReporterEffectCounts(0, 0, 0, 0, 0),
            await CountEffectsAsync(reporter.Id, key, null));
    }

    private static ReporterReportService CreateService(RoadGuardDbContext db, IAnhHuyProducerService producer,
        IReporterReportRepository? repository = null)
        => new(producer, repository ?? new ReporterReportRepository(db), new IdempotencyOperationService(db));

    private static AnhHuyProducerService CreateProducer(RoadGuardDbContext db)
        => new AnhHuyProducerService(new AnhHuyFactsRepository(db), null!, null!);

    private static CreateReporterReportRequestDto CreateRequest(EvidenceFixture evidence)
        => new("SQL receipt guard regression", [new ReportEvidenceInputDto(evidence.FileId, evidence.Version, "UNKNOWN")]);

    private async Task<ReceiptSetup> CreateReceiptAsync()
    {
        var reporter = await SeedReporterAsync();
        var evidence = await SeedVerifiedEvidenceAsync(reporter.Id);
        var key = Guid.NewGuid().ToString("N");
        var request = CreateRequest(evidence);
        await using var db = fixture.CreateDbContext();
        var created = await CreateService(db, CreateProducer(db)).CreateAsync(
            reporter.Id, UserRoleCode.Reporter, request, key, null);
        Assert.Equal(ReporterReportCommandStatus.Created, created.Status);
        return new(reporter, evidence, key, request, created.Report!.Id);
    }

    private async Task<ApplicationUser> SeedReporterAsync()
    {
        var id = Guid.NewGuid();
        var userName = $"reporter-{id:N}";
        var email = $"{userName}@example.test";
        var user = new ApplicationUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = "Reporter receipt guard fixture",
            PasswordHash = "fixture-password-hash",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            RoleCode = UserRoleCode.Reporter,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await using var db = fixture.CreateDbContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<EvidenceFixture> SeedVerifiedEvidenceAsync(Guid ownerId, bool projectScoped = false)
    {
        var now = DateTimeOffset.UtcNow;
        var objectKey = $"private/reporter-receipt-{Guid.NewGuid():N}.jpg";
        var file = StoredFile.Create(Guid.NewGuid(), objectKey, "reporter-receipt.jpg", "image/jpeg", 4,
            new string('a', 64), ownerId, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, ownerId, objectKey, "REPORT_PHOTO", "image/jpeg",
            4, new string('a', 64), 1, now.AddHours(1));
        await using var db = fixture.CreateDbContext();
        db.Files.Add(file);
        if (projectScoped)
        {
            var project = Project.Create(Guid.NewGuid(), $"RP-{Guid.NewGuid():N}", "Reporter receipt scope fixture", null,
                32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now);
            db.Projects.Add(project);
            db.FileScopes.Add(FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, ownerId, "REPORT_PHOTO", now));
        }
        else
        {
            db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), file.Id, ownerId, now));
        }
        db.UploadSessions.Add(upload);
        await db.SaveChangesAsync();
        upload.StartUploading("fixture-upload", now);
        await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(db.Entry(upload).Property<byte[]>("RowVersion").CurrentValue!), now);
        await db.SaveChangesAsync();
        upload.MarkVerified();
        await db.SaveChangesAsync();
        return new(file.Id, upload.Id, Convert.ToBase64String(db.Entry(upload).Property<byte[]>("RowVersion").CurrentValue!));
    }

    private async Task MutateActorAsync(Guid reporterId, ActorMutation mutation)
    {
        await using var db = fixture.CreateDbContext();
        var actor = await db.Users.SingleAsync(user => user.Id == reporterId);
        switch (mutation)
        {
            case ActorMutation.Suspended:
                actor.Status = UserStatus.Suspended;
                break;
            case ActorMutation.RoleChanged:
                await new IdentityRepository(db).ChangeUserRoleAtomicAsync(actor.Id, UserRoleCode.ProjectManager,
                    actor.RowVersion, actor.Id, Guid.NewGuid(), reason: "Reporter receipt guard race fixture");
                return;
            case ActorMutation.MustChangePassword:
                actor.MustChangePassword = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }
        await db.SaveChangesAsync();
    }

    private async Task MutateEvidenceAsync(EvidenceFixture evidence, EvidenceMutation mutation)
    {
        await using var db = fixture.CreateDbContext();
        switch (mutation)
        {
            case EvidenceMutation.Pending:
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [UploadSessions] SET [Status] = {(byte)UploadSessionStatus.Pending} WHERE [Id] = {evidence.UploadId}");
                break;
            case EvidenceMutation.Failed:
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [UploadSessions] SET [Status] = {(byte)UploadSessionStatus.Failed} WHERE [Id] = {evidence.UploadId}");
                break;
            case EvidenceMutation.VersionChanged:
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [UploadSessions] SET [StorageUploadId] = {Guid.NewGuid().ToString("N")} WHERE [Id] = {evidence.UploadId}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }
    }

    private RoadGuardDbContext CreateInterceptedDbContext(IInterceptor interceptor, bool enableRetry)
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite())
            .AddInterceptors(interceptor);
        if (enableRetry)
        {
            options.ReplaceService<IExecutionStrategyFactory, CommitFailureExecutionStrategyFactory>();
        }
        return new RoadGuardDbContext(options.Options);
    }

    private async Task<ReporterEffectCounts> CountEffectsAsync(Guid reporterId, string idempotencyKey, Guid? expectedReportId)
    {
        await using var db = fixture.CreateDbContext();
        var reportIds = db.Reports.Where(report => report.ReporterUserId == reporterId).Select(report => report.Id);
        var activeLinks = db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .Where(link => reportIds.Contains(link.ReportId) && link.EndedAt == null);
        var receipts = db.IdempotencyRecords.Where(record => record.ActorUserId == reporterId && record.ProjectId == null &&
            record.Operation == "huy01.report.create.v1" && record.IdempotencyKey == idempotencyKey);
        if (expectedReportId is Guid reportId)
        {
            receipts = receipts.Where(record => record.OperationId == reportId);
        }
        return new(
            await reportIds.CountAsync(),
            await db.IncidentCases.CountAsync(@case => activeLinks.Select(link => link.CaseId).Contains(@case.Id) && @case.Status == IncidentCaseStatus.Unassigned),
            await activeLinks.CountAsync(),
            await db.AuditLogs.CountAsync(audit => audit.ActorUserId == reporterId && audit.EventType == "report_received" && audit.Source == "huy01.reporter-intake"),
            await receipts.CountAsync());
    }

    private sealed record EvidenceFixture(Guid FileId, Guid UploadId, string Version);
    private sealed record ReceiptSetup(ApplicationUser Reporter, EvidenceFixture Evidence, string Key,
        CreateReporterReportRequestDto Request, Guid ReportId);
    private sealed record ReporterEffectCounts(int Reports, int UnassignedCases, int ActiveLinks, int Audits, int Receipts);
    public enum ActorMutation { Suspended, RoleChanged, MustChangePassword }
    public enum EvidenceMutation { Pending, Failed, VersionChanged }

    private sealed class MutateAfterPreflightProducer(IAnhHuyProducerService inner, Func<Task> mutate) : IAnhHuyProducerService
    {
        private int _remainingMutation = 1;

        public async Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePrivateEvidenceAsync(Guid actorId, UserRoleCode role,
            Guid fileId, Guid evidenceId, string? expectedFileVersion = null, CancellationToken cancellationToken = default)
        {
            var result = await inner.ResolvePrivateEvidenceAsync(actorId, role, fileId, evidenceId, expectedFileVersion, cancellationToken);
            if (result.Status == AnhHuyProducerStatus.Ready && Interlocked.Exchange(ref _remainingMutation, 0) == 1)
            {
                await mutate();
            }

            return result;
        }

        public Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePublicationEvidenceAsync(Guid actorId, UserRoleCode role,
            Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken cancellationToken = default)
            => inner.ResolvePublicationEvidenceAsync(actorId, role, publicationId, reportId, evidenceId, cancellationToken);

        public Task<AnhHuyProducerResult<ProjectGeometryContext>> ResolveGeometryAsync(Guid actorId, UserRoleCode role,
            Guid projectId, Guid routeVersionId, Guid segmentSetId, string? expectedVersion = null, bool requireCurrent = true,
            CancellationToken cancellationToken = default)
            => inner.ResolveGeometryAsync(actorId, role, projectId, routeVersionId, segmentSetId, expectedVersion, requireCurrent, cancellationToken);

        public Task<AnhHuyProducerResult<ResolvedCandidateSourceFacts>> ResolveCandidateSourceAsync(Guid actorId, UserRoleCode role,
            Guid projectId, RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind kind, Guid sourceId,
            string? expectedSourceVersion = null, string? expectedGeometryVersion = null, string? expectedDispositionVersion = null,
            CancellationToken cancellationToken = default)
            => inner.ResolveCandidateSourceAsync(actorId, role, projectId, kind, sourceId, expectedSourceVersion,
                expectedGeometryVersion, expectedDispositionVersion, cancellationToken);
    }

    private sealed class CountingRepository(IReporterReportRepository inner, Func<Task>? beforeGuard = null) : IReporterReportRepository
    {
        public int GuardCalls { get; private set; }
        public int CreateCalls { get; private set; }

        public async Task EnsureCurrentReceiptAccessAsync(Guid reporterUserId, IReadOnlyList<VerifiedEvidenceReference> evidence,
            CancellationToken cancellationToken = default)
        {
            GuardCalls++;
            if (beforeGuard is not null) await beforeGuard();
            await inner.EnsureCurrentReceiptAccessAsync(reporterUserId, evidence, cancellationToken);
        }

        public async Task<ReporterReportWriteResult> CreateAndSaveAsync(Guid reporterUserId, string description,
            IReadOnlyList<VerifiedEvidenceReference> evidence, Guid? correlationId, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return await inner.CreateAndSaveAsync(reporterUserId, description, evidence, correlationId, cancellationToken);
        }
    }

    private sealed class FailingAfterCurrentValidationRepository(IReporterReportRepository inner) : IReporterReportRepository
    {
        public int GuardCalls { get; private set; }
        public int CreateCalls { get; private set; }

        public async Task EnsureCurrentReceiptAccessAsync(Guid reporterUserId, IReadOnlyList<VerifiedEvidenceReference> evidence,
            CancellationToken cancellationToken = default)
        {
            GuardCalls++;
            await inner.EnsureCurrentReceiptAccessAsync(reporterUserId, evidence, cancellationToken);
            throw new ReporterIntakeFactsException(ReporterIntakeFactsStatus.Forbidden);
        }

        public async Task<ReporterReportWriteResult> CreateAndSaveAsync(Guid reporterUserId, string description,
            IReadOnlyList<VerifiedEvidenceReference> evidence, Guid? correlationId, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return await inner.CreateAndSaveAsync(reporterUserId, description, evidence, correlationId, cancellationToken);
        }
    }

    private sealed class CancellingAfterCurrentValidationRepository(IReporterReportRepository inner,
        CancellationTokenSource cancellation) : IReporterReportRepository
    {
        public int GuardCalls { get; private set; }
        public int CreateCalls { get; private set; }

        public async Task EnsureCurrentReceiptAccessAsync(Guid reporterUserId, IReadOnlyList<VerifiedEvidenceReference> evidence,
            CancellationToken cancellationToken = default)
        {
            GuardCalls++;
            await inner.EnsureCurrentReceiptAccessAsync(reporterUserId, evidence, cancellationToken);
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task<ReporterReportWriteResult> CreateAndSaveAsync(Guid reporterUserId, string description,
            IReadOnlyList<VerifiedEvidenceReference> evidence, Guid? correlationId, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return await inner.CreateAndSaveAsync(reporterUserId, description, evidence, correlationId, cancellationToken);
        }
    }

    private sealed class TwoPhaseProducer(IAnhHuyProducerService inner, TwoParticipantGate preflightGate,
        TwoParticipantGate handlerGate) : IAnhHuyProducerService
    {
        private int _calls;

        public async Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePrivateEvidenceAsync(Guid actorId, UserRoleCode role,
            Guid fileId, Guid evidenceId, string? expectedFileVersion = null, CancellationToken cancellationToken = default)
        {
            var result = await inner.ResolvePrivateEvidenceAsync(actorId, role, fileId, evidenceId, expectedFileVersion, cancellationToken);
            if (result.Status == AnhHuyProducerStatus.Ready)
            {
                var call = Interlocked.Increment(ref _calls);
                if (call == 1) await preflightGate.ArriveAsync(cancellationToken);
                if (call == 2) await handlerGate.ArriveAsync(cancellationToken);
            }
            return result;
        }

        public Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePublicationEvidenceAsync(Guid actorId, UserRoleCode role,
            Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken cancellationToken = default)
            => inner.ResolvePublicationEvidenceAsync(actorId, role, publicationId, reportId, evidenceId, cancellationToken);

        public Task<AnhHuyProducerResult<ProjectGeometryContext>> ResolveGeometryAsync(Guid actorId, UserRoleCode role,
            Guid projectId, Guid routeVersionId, Guid segmentSetId, string? expectedVersion = null, bool requireCurrent = true,
            CancellationToken cancellationToken = default)
            => inner.ResolveGeometryAsync(actorId, role, projectId, routeVersionId, segmentSetId, expectedVersion, requireCurrent, cancellationToken);

        public Task<AnhHuyProducerResult<ResolvedCandidateSourceFacts>> ResolveCandidateSourceAsync(Guid actorId, UserRoleCode role,
            Guid projectId, RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind kind, Guid sourceId,
            string? expectedSourceVersion = null, string? expectedGeometryVersion = null, string? expectedDispositionVersion = null,
            CancellationToken cancellationToken = default)
            => inner.ResolveCandidateSourceAsync(actorId, role, projectId, kind, sourceId, expectedSourceVersion,
                expectedGeometryVersion, expectedDispositionVersion, cancellationToken);
    }

    private sealed class TwoParticipantGate
    {
        private int _arrivals;
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ArriveAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrivals) == 2)
            {
                _released.TrySetResult();
            }
            return _released.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class UniqueViolationProbe : DbCommandInterceptor
    {
        private int _count;
        public int Count => _count;
        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Exception is Microsoft.Data.SqlClient.SqlException exception && exception.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(error => error.Number is 2601 or 2627))
                Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }

    private sealed class FailFirstIdempotencyLookupInterceptor : DbCommandInterceptor
    {
        private int _remainingFailures = 1;
        public int FailureCount { get; private set; }
        public int LookupCount { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("[IdempotencyRecords]", StringComparison.Ordinal))
            {
                return ValueTask.FromResult(result);
            }
            LookupCount++;
            if (Interlocked.Exchange(ref _remainingFailures, 0) == 1)
            {
                FailureCount++;
                throw new CommitFailureTransientException("Injected retry before Reporter receipt lookup.");
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RevokeAfterFirstCommitInterceptor(Func<Task> revoke) : DbTransactionInterceptor
    {
        private int _remainingFailures = 1;
        public int FailureCount { get; private set; }

        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _remainingFailures, 0) == 1)
            {
                FailureCount++;
                await revoke();
                throw new CommitFailureTransientException("Injected acknowledgement loss after durable Reporter receipt.");
            }
        }
    }

    private sealed class AlwaysFailBeforeCommitInterceptor : DbTransactionInterceptor
    {
        public int FailureCount { get; private set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            FailureCount++;
            throw new CommitFailureTransientException("Injected pre-commit Reporter intake failure.");
        }
    }
}
