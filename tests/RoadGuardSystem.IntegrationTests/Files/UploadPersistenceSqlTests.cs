using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
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
        var project = await AddProjectAsync(context);
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
    public async Task Anh01_LargeMetadata_ResumeAndExpiredReceipt_DoNotCreateAnotherMultipart()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await AddUserAsync(context);
        var project = await AddProjectAsync(context);
        var storage = new DeterministicUploadStorage(new(8589934592L, new string('a', 64), "video/mp4"));
        var repository = new UploadPersistenceService(context, new IdempotencyOperationService(context), storage);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "SYNTHETIC-METADATA");
        var task = RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest.Create(Guid.NewGuid(), project.Id, road.Id, null, user.Id,
            SurveyType.Original, SurveyRequestStatus.Accepted, DateTimeOffset.UtcNow);
        context.RoadSections.Add(road); context.SurveyRequests.Add(task);
        context.SurveyAssignments.Add(RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment.Create(Guid.NewGuid(), task.Id, user.Id, user.Id,
            DateTimeOffset.UtcNow, null, null, null, null, null));
        await context.SaveChangesAsync();
        var request = new UploadCreatePersistenceRequest(user.Id, project.Id, null, "SURVEY_VIDEO", "large.mp4", "video/mp4",
            8589934592L, new string('a', 64), 8388608, DateTimeOffset.UtcNow.AddHours(24), Guid.NewGuid().ToString(), new string('b', 64), null);
        request = request with { TargetId = task.Id };
        var created = await repository.CreateAsync(request);
        created.Status.Should().Be(UploadPersistenceStatus.Success);
        var file = await context.Files.AsNoTracking().SingleAsync(f => f.Id == created.Session!.FileId);
        file.SizeBytes.Should().Be(8589934592L);
        var now = DateTimeOffset.UtcNow;
        var key = Guid.NewGuid().ToString();
        await using var contender = _fixture.CreateDbContext();
        var contenderRepository = new UploadPersistenceService(contender, new IdempotencyOperationService(contender), storage);
        var initialized = await Task.WhenAll(
            repository.GetPartUrlsAsync(user.Id, project.Id, created.Session!.Id, [1, 1024], key, new string('c', 64), now, now.AddMinutes(15)),
            contenderRepository.GetPartUrlsAsync(user.Id, project.Id, created.Session.Id, [1, 1024], key, new string('c', 64), now, now.AddMinutes(15)));
        initialized.Count(result => result.Status == UploadPersistenceStatus.Success).Should().Be(1);
        initialized.Should().OnlyContain(result => result.Status == UploadPersistenceStatus.Success || result.Status == UploadPersistenceStatus.Replayed || result.Status == UploadPersistenceStatus.StorageUnavailable);
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

    private async Task<ApplicationUser> AddUserAsync(RoadGuardDbContext context)
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
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Project> AddProjectAsync(RoadGuardDbContext context)
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
        var user = await context.Users.OrderByDescending(u => u.CreatedAt).FirstAsync();
        context.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = user.Id,
            RoleCode = user.RoleCode, Status = ProjectMemberStatus.Active, ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
        await context.SaveChangesAsync();
        return project;
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
