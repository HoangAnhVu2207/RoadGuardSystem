using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("TaskId", "P2-023/P2-024/P2-025/P2-026/P2-027/P2-028")]
public sealed class UploadPersistenceSqlTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public UploadPersistenceSqlTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UploadMigration_CreatesScopedTablesAndConcurrencyToken()
    {
        await using var context = _fixture.CreateDbContext();

        var objects = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST((SELECT COUNT(*) FROM sys.tables WHERE [name] IN ('FileScopes', 'UploadSessions', 'UploadParts')) AS int) AS [Value]")
            .SingleAsync();
        var rowVersions = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST((SELECT COUNT(*) FROM sys.columns WHERE [object_id] = OBJECT_ID('UploadSessions') AND [name] = 'RowVersion' AND [system_type_id] = 189) AS int) AS [Value]")
            .SingleAsync();

        objects.Should().Be(3);
        rowVersions.Should().Be(1);
    }

    [Fact]
    public async Task UploadFlow_ReplaysCreateRejectsStaleVersionAndVerifiesCompletedObject()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await AddUserAsync(context);
        var project = await AddProjectAsync(context, user);
        var checksum = new string('a', 64);
        var storage = new DeterministicUploadStorage(new(16, checksum, "application/pdf"));
        var repository = new UploadPersistenceService(context, new IdempotencyOperationService(context), storage);
        var request = new UploadCreatePersistenceRequest(
            user.Id,
            project.Id,
            null,
            "DOCUMENT",
            "evidence.pdf",
            "application/pdf",
            16,
            checksum,
            8 * 1024 * 1024,
            DateTimeOffset.UtcNow.AddHours(24),
            $"create-{Guid.NewGuid():N}",
            new string('b', 64),
            Guid.NewGuid());

        var created = await repository.CreateAsync(request);
        var replayed = await repository.CreateAsync(request);

        created.Status.Should().Be(UploadPersistenceStatus.Success);
        replayed.Status.Should().Be(UploadPersistenceStatus.Replayed);
        replayed.Session!.Id.Should().Be(created.Session!.Id);
        (await context.IdempotencyRecords.CountAsync(record => record.Operation == "UploadSessionCreated" && record.IdempotencyKey == request.IdempotencyKey))
            .Should().Be(1);

        var urls = await repository.GetPartUrlsAsync(
            user.Id,
            project.Id,
            created.Session.Id,
            [1],
            $"parts-{Guid.NewGuid():N}",
            new string('c', 64),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(15));
        urls.Status.Should().Be(UploadPersistenceStatus.Success);
        urls.Parts.Should().ContainSingle();

        var current = await repository.GetSessionAsync(created.Session.Id);
        current.Should().NotBeNull();

        var stale = await repository.CompleteAsync(new(
            user.Id,
            project.Id,
            created.Session.Id,
            Convert.ToBase64String(Enumerable.Repeat((byte)255, 8).ToArray()),
            [new CompletedStoragePart(1, "etag-1")],
            checksum,
            $"complete-stale-{Guid.NewGuid():N}",
            new string('d', 64),
            Guid.NewGuid()));
        stale.Status.Should().Be(UploadPersistenceStatus.ConcurrencyConflict);

        var completed = await repository.CompleteAsync(new(
            user.Id,
            project.Id,
            created.Session.Id,
            current!.Version,
            [new CompletedStoragePart(1, "etag-1")],
            checksum,
            $"complete-{Guid.NewGuid():N}",
            new string('e', 64),
            Guid.NewGuid()));
        completed.Status.Should().Be(UploadPersistenceStatus.Success);
        completed.Session!.Status.Should().Be("VERIFYING");

        storage.FailReadOnce = true;
        (await repository.VerifyNextAsync()).Should().Be(UploadPersistenceStatus.StorageUnavailable);
        (await repository.GetSessionAsync(created.Session.Id))!.Status.Should().Be("VERIFYING");
        (await repository.VerifyNextAsync()).Should().Be(UploadPersistenceStatus.Success);
        var file = await repository.GetFileMetadataAsync(created.Session.FileId);
        file.Should().NotBeNull();
        file!.Status.Should().Be("VERIFIED");
        storage.CompleteCalls.Should().Be(2);
    }

    [Fact]
    public async Task VerificationCursor_SkipsMoreThanOneBatchOfRetryableFailures_ThenRetriesAfterRecovery()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await AddUserAsync(context);
        var project = await AddProjectAsync(context, user);
        var checksum = new string('a', 64);
        var storage = new DeterministicUploadStorage(new(16, checksum, "application/pdf"));
        var repository = new UploadPersistenceService(context, new IdempotencyOperationService(context), storage);
        var uploads = new List<UploadSessionPersistenceView>();

        for (var index = 0; index < 22; index++)
        {
            var created = await repository.CreateAsync(new UploadCreatePersistenceRequest(
                user.Id, project.Id, null, "DOCUMENT", $"fair-{index}.pdf", "application/pdf", 16,
                checksum, 8 * 1024 * 1024, DateTimeOffset.UtcNow.AddHours(1).AddMinutes(index),
                $"fair-create-{Guid.NewGuid():N}", new string('b', 64), Guid.NewGuid()));
            created.Status.Should().Be(UploadPersistenceStatus.Success);
            var upload = created.Session!;
            var urls = await repository.GetPartUrlsAsync(user.Id, project.Id, upload.Id, [1],
                $"fair-parts-{Guid.NewGuid():N}", new string('c', 64), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(15));
            urls.Status.Should().Be(UploadPersistenceStatus.Success);
            var version = (await repository.GetSessionAsync(upload.Id))!.Version;
            var completed = await repository.CompleteAsync(new UploadCompletePersistenceRequest(
                user.Id, project.Id, upload.Id, version, [new CompletedStoragePart(1, "etag-1")],
                checksum, $"fair-complete-{Guid.NewGuid():N}", new string('d', 64), Guid.NewGuid()));
            completed.Status.Should().Be(UploadPersistenceStatus.Success);
            uploads.Add(upload);
            if (index < 21) storage.FailingObjectKeys.Add($"uploads/{upload.FileId:N}");
        }

        for (var offset = 0; offset < 21; offset++)
            (await repository.VerifyNextAsync(offset)).Should().Be(UploadPersistenceStatus.StorageUnavailable);

        (await repository.VerifyNextAsync(21)).Should().Be(UploadPersistenceStatus.Success);
        (await repository.GetFileMetadataAsync(uploads[21].FileId))!.Status.Should().Be("VERIFIED");
        (await repository.GetSessionAsync(uploads[0].Id))!.Status.Should().Be("VERIFYING");

        storage.FailingObjectKeys.Clear();
        (await repository.VerifyNextAsync(0)).Should().Be(UploadPersistenceStatus.Success);
        (await repository.GetFileMetadataAsync(uploads[0].FileId))!.Status.Should().Be("VERIFIED");
    }

    [Fact]
    public async Task Anh01_LargeMetadata_ResumeAndExpiredReceipt_DoNotCreateAnotherMultipart()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await AddUserAsync(context);
        var project = await AddProjectAsync(context, user);
        var survey = await AddAcceptedSurveyAsync(context, project, user);
        var storage = new DeterministicUploadStorage(new(8589934592L, new string('a', 64), "video/mp4"));
        var repository = new UploadPersistenceService(context, new IdempotencyOperationService(context), storage);
        var request = new UploadCreatePersistenceRequest(user.Id, project.Id, survey.Id, "SURVEY_VIDEO", "large.mp4", "video/mp4",
            8589934592L, new string('a', 64), 8388608, DateTimeOffset.UtcNow.AddHours(24), Guid.NewGuid().ToString(), new string('b', 64), null);
        var created = await repository.CreateAsync(request);
        created.Status.Should().Be(UploadPersistenceStatus.Success);
        var file = await context.Files.AsNoTracking().SingleAsync(f => f.Id == created.Session!.FileId);
        file.SizeBytes.Should().Be(8589934592L);
        var now = DateTimeOffset.UtcNow;
        // Multipart initialization is a fenced external phase. Complete it before
        // racing the same durable URL receipt; CALLING recovery is covered by
        // MultipartRecoveryStateSqlTests rather than assuming immediate replay.
        var prepared = await repository.GetPartUrlsAsync(user.Id, project.Id, created.Session!.Id, [1],
            Guid.NewGuid().ToString(), new string('f', 64), now, now.AddMinutes(15));
        prepared.Status.Should().Be(UploadPersistenceStatus.Success);
        var key = Guid.NewGuid().ToString();
        await using var contender = _fixture.CreateDbContext();
        var contenderRepository = new UploadPersistenceService(contender, new IdempotencyOperationService(contender), storage);
        var initialized = await Task.WhenAll(
            repository.GetPartUrlsAsync(user.Id, project.Id, created.Session!.Id, [1, 1024], key, new string('c', 64), now, now.AddMinutes(15)),
            contenderRepository.GetPartUrlsAsync(user.Id, project.Id, created.Session.Id, [1, 1024], key, new string('c', 64), now, now.AddMinutes(15)));
        initialized.Count(result => result.Status == UploadPersistenceStatus.Success).Should().Be(1);
        initialized.Count(result => result.Status == UploadPersistenceStatus.Replayed).Should().Be(1);
        var first = initialized.Single(result => result.Status == UploadPersistenceStatus.Success);
        first.Status.Should().Be(UploadPersistenceStatus.Success);
        await using var resumed = _fixture.CreateDbContext();
        var resumedRepository = new UploadPersistenceService(resumed, new IdempotencyOperationService(resumed), storage);
        var replay = await resumedRepository.GetPartUrlsAsync(user.Id, project.Id, created.Session.Id, [1, 1024], key, new string('c', 64), now.AddMinutes(1), now.AddMinutes(16));
        replay.Status.Should().Be(UploadPersistenceStatus.Replayed);
        replay.Parts.Should().BeEquivalentTo(first.Parts);
        var expired = await resumedRepository.GetPartUrlsAsync(user.Id, project.Id, created.Session.Id, [1, 1024], key, new string('c', 64), now.AddMinutes(16), now.AddMinutes(31));
        expired.Status.Should().Be(UploadPersistenceStatus.Conflict);
        var fresh = await resumedRepository.GetPartUrlsAsync(user.Id, project.Id, created.Session.Id, [1, 1024], Guid.NewGuid().ToString(), new string('c', 64), now.AddMinutes(16), now.AddMinutes(31));
        fresh.Status.Should().Be(UploadPersistenceStatus.Success);
        storage.InitiateCalls.Should().Be(1);
        (await resumed.UploadSessions.SingleAsync(s => s.Id == created.Session.Id)).ExpiresAt.Should().Be(request.ExpiresAt);
    }

    private async Task<ApplicationUser> AddUserAsync(RoadGuardDbContext context, UserRoleCode role = UserRoleCode.DroneOperator)
    {
        await _fixture.SeedRolesAsync(context);
        var userName = $"upload-user-{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            DisplayName = "Upload SQL fixture user",
            PasswordHash = "fixture-password-hash",
            RoleCode = role,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Project> AddProjectAsync(RoadGuardDbContext context, ApplicationUser user)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"UPLOAD-{Guid.NewGuid():N}",
            Name = "Upload SQL fixture project",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Projects.Add(project);
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user.Id,
            RoleCode = user.RoleCode,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        });
        await context.SaveChangesAsync();
        return project;
    }

    private async Task<SurveyRequest> AddAcceptedSurveyAsync(RoadGuardDbContext context, Project project, ApplicationUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var manager = await AddUserAsync(context, UserRoleCode.ProjectManager);
        context.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, manager.Id,
            DateOnly.FromDateTime(now.UtcDateTime)));
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "UPLOAD-ROAD");
        context.RoadSections.Add(road);
        var request = SurveyRequest.Create(Guid.NewGuid(), project.Id, road.Id, null, manager.Id,
            SurveyType.Original, SurveyRequestStatus.NewAssigned, now, now.AddDays(1));
        request.Accept();
        context.SurveyRequests.Add(request);
        context.SurveyAssignments.Add(SurveyAssignment.Create(Guid.NewGuid(), request.Id, user.Id, manager.Id,
            now, now, null, null, null, null));
        await context.SaveChangesAsync();
        return request;
    }

    private sealed class DeterministicUploadStorage : IUploadObjectStorage
    {
        private readonly UploadObjectVerification _verification;

        public DeterministicUploadStorage(UploadObjectVerification verification)
        {
            _verification = verification;
        }

        public int CompleteCalls { get; private set; }
        public int InitiateCalls { get; private set; }
        public bool FailReadOnce { get; set; }
        public HashSet<string> FailingObjectKeys { get; } = [];

        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
        {
            InitiateCalls++;
            return Task.FromResult($"upload-{objectKey}");
        }

        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(
            string objectKey,
            string uploadId,
            IReadOnlyList<int> partNumbers,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(
                partNumbers.Select(partNumber => new PresignedUploadPart(partNumber, $"https://storage.test/{uploadId}/{partNumber}", expiresAt)).ToArray());

        public Task<UploadObjectVerification> CompleteAndVerifyAsync(
            string objectKey,
            string uploadId,
            IReadOnlyList<CompletedStoragePart> parts,
            CancellationToken cancellationToken = default)
        {
            CompleteCalls++;
            if (FailingObjectKeys.Contains(objectKey))
                throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Transient verification read outage");
            if (FailReadOnce)
            {
                FailReadOnce = false;
                throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Transient verification read outage");
            }
            return Task.FromResult(_verification);
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream());
    }
}
