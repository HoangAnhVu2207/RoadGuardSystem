using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Implementations.Reports;
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
    public async Task Replay_ActorRevokedAfterRealPreflight_DeniesStoredReceiptWithoutNewEffects()
    {
        var reporter = await SeedReporterAsync();
        var evidence = await SeedVerifiedEvidenceAsync(reporter.Id);
        var key = Guid.NewGuid().ToString("N");
        var request = CreateRequest(evidence);

        await using (var createDb = fixture.CreateDbContext())
        {
            var created = await CreateService(createDb, CreateProducer(createDb)).CreateAsync(
                reporter.Id, UserRoleCode.Reporter, request, key, null);
            Assert.Equal(ReporterReportCommandStatus.Created, created.Status);
            Assert.NotNull(created.Report);
        }

        var beforeReplay = await CountEffectsAsync(reporter.Id, key);
        await using var replayDb = fixture.CreateDbContext();
        var replay = await CreateService(replayDb, new RevokeAfterPreflightProducer(
            CreateProducer(replayDb), () => RevokeAsync(reporter.Id))).CreateAsync(
            reporter.Id, UserRoleCode.Reporter, request, key, null);

        Assert.Equal(ReporterReportCommandStatus.Forbidden, replay.Status);
        Assert.Null(replay.Report);
        Assert.Equal(beforeReplay, await CountEffectsAsync(reporter.Id, key));
    }

    private static ReporterReportService CreateService(RoadGuardDbContext db, IAnhHuyProducerService producer)
        => new(producer, new ReporterReportRepository(db), new IdempotencyOperationService(db));

    private static AnhHuyProducerService CreateProducer(RoadGuardDbContext db)
        => new AnhHuyProducerService(new AnhHuyFactsRepository(db), null!, null!);

    private static CreateReporterReportRequestDto CreateRequest((Guid FileId, string Version) evidence)
        => new("SQL receipt guard regression", [new ReportEvidenceInputDto(evidence.FileId, evidence.Version, "UNKNOWN")]);

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

    private async Task<(Guid FileId, string Version)> SeedVerifiedEvidenceAsync(Guid ownerId)
    {
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "private/reporter-receipt.jpg", "reporter-receipt.jpg", "image/jpeg", 4,
            new string('a', 64), ownerId, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, ownerId, "private/reporter-receipt.jpg", "REPORT_PHOTO", "image/jpeg",
            4, new string('a', 64), 1, now.AddHours(1));
        await using var db = fixture.CreateDbContext();
        db.Files.Add(file);
        db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), file.Id, ownerId, now));
        db.UploadSessions.Add(upload);
        await db.SaveChangesAsync();
        upload.StartUploading("fixture-upload", now);
        await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(db.Entry(upload).Property<byte[]>("RowVersion").CurrentValue!), now);
        await db.SaveChangesAsync();
        upload.MarkVerified();
        await db.SaveChangesAsync();
        return (file.Id, Convert.ToBase64String(db.Entry(upload).Property<byte[]>("RowVersion").CurrentValue!));
    }

    private async Task RevokeAsync(Guid reporterId)
    {
        await using var db = fixture.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [Status] = {(byte)UserStatus.Suspended} WHERE [Id] = {reporterId}");
    }

    private async Task<ReporterEffectCounts> CountEffectsAsync(Guid reporterId, string idempotencyKey)
    {
        await using var db = fixture.CreateDbContext();
        var reportIds = db.Reports.Where(report => report.ReporterUserId == reporterId).Select(report => report.Id);
        var activeLinks = db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .Where(link => reportIds.Contains(link.ReportId) && link.EndedAt == null);
        return new(
            await reportIds.CountAsync(),
            await db.IncidentCases.CountAsync(@case => activeLinks.Select(link => link.CaseId).Contains(@case.Id)),
            await activeLinks.CountAsync(),
            await db.AuditLogs.CountAsync(audit => audit.ActorUserId == reporterId && audit.EventType == "report_received" && audit.Source == "huy01.reporter-intake"),
            await db.IdempotencyRecords.CountAsync(record => record.ActorUserId == reporterId && record.ProjectId == null &&
                record.Operation == "huy01.report.create.v1" && record.IdempotencyKey == idempotencyKey));
    }

    private sealed record ReporterEffectCounts(int Reports, int Cases, int ActiveLinks, int Audits, int Receipts);

    private sealed class RevokeAfterPreflightProducer(IAnhHuyProducerService inner, Func<Task> revoke) : IAnhHuyProducerService
    {
        private int _remainingRevoke = 1;

        public async Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePrivateEvidenceAsync(Guid actorId, UserRoleCode role,
            Guid fileId, Guid evidenceId, string? expectedFileVersion = null, CancellationToken cancellationToken = default)
        {
            var result = await inner.ResolvePrivateEvidenceAsync(actorId, role, fileId, evidenceId, expectedFileVersion, cancellationToken);
            if (result.Status == AnhHuyProducerStatus.Ready && Interlocked.Exchange(ref _remainingRevoke, 0) == 1)
            {
                await revoke();
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
}
