using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.Repositories.Models.Huy01;
using Xunit;

namespace RoadGuardSystem.ApiTests.Files;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ReporterEvidenceApiTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("role")]
    [InlineData("mismatch")]
    [InlineData("inactive")]
    [InlineData("password")]
    public async Task PrivateProducer_CurrentSqlAuthority_DeniesMetadataAndCreatesNoEffects(string change)
    {
        var owner = await sql.CreateUserAsync($"authority-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new PhotoStorage()); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        var file = await UploadVerified(client, factory);
        await using var db = sql.CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Id == owner.Id);
        var role = await db.Roles.SingleAsync(r => r.Code == UserRoleCode.Reporter);
        var before = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id);
        var files = await db.Files.CountAsync(f => f.UploadedByUserId == owner.Id);
        try
        {
            switch (change)
            {
                case "role": role.IsActive = false; break;
                // Isolated SQL authority fault, not a production identity mutation.
                case "mismatch": await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [RoleCode]={UserRoleCode.DroneOperator.ToDbCode()} WHERE [Id]={owner.Id}"); break;
                case "inactive": user.Status = UserStatus.Suspended; break;
                case "password": user.MustChangePassword = true; break;
            }
            await db.SaveChangesAsync();
            using var scope = factory.Services.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<IAnhHuyProducerService>();
            (await producer.ResolvePrivateEvidenceAsync(owner.Id, UserRoleCode.Reporter, file.File, Guid.NewGuid())).Status
                .Should().Be(AnhHuyProducerStatus.Forbidden);
            foreach (var suffix in new[] { "", "/content" })
            {
                var response = await client.GetAsync($"/api/v1/reporter-evidence/files/{file.File}{suffix}");
                response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
                response.Headers.ETag.Should().BeNull();
            }
            (await Create(client, Guid.NewGuid().ToString())).StatusCode.Should()
                .BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id)).Should().Be(before);
            (await db.Files.CountAsync(f => f.UploadedByUserId == owner.Id)).Should().Be(files);
        }
        finally
        {
            if (change == "mismatch") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [RoleCode]={UserRoleCode.Reporter.ToDbCode()} WHERE [Id]={owner.Id}");
            role.IsActive = true; user.Status = UserStatus.Active; user.MustChangePassword = false;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task PrivateReceipt_RoleChangedAfterRealPreflight_DeniesReplayConflictAndNewCreate()
    {
        var owner = await sql.CreateUserAsync($"receipt-authority-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var db = sql.CreateDbContext();
        var repo = new RoadGuardSystem.Repositories.Implementations.Files.UploadPersistenceService(db,
            new RoadGuardSystem.Repositories.Idempotency.IdempotencyOperationService(db), new PhotoStorage());
        var input = new RoadGuardSystem.Repositories.Files.UploadCreatePersistenceRequest(owner.Id, null, null, "REPORT_PHOTO",
            "photo.jpg", "image/jpeg", 4, PhotoStorage.Hash, 8388608, DateTimeOffset.UtcNow.AddHours(24), Guid.NewGuid().ToString(), PhotoStorage.Hash, null);
        var first = await repo.CreateAsync(input);
        first.Status.Should().Be(RoadGuardSystem.Repositories.Files.UploadPersistenceStatus.Success);
        var facts = new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(db);
        (await facts.IsCurrentActorAsync(owner.Id, UserRoleCode.Reporter, default)).Should().BeTrue();
        var role = await db.Roles.SingleAsync(r => r.Code == UserRoleCode.Reporter);
        var before = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id);
        try
        {
            role.IsActive = false; await db.SaveChangesAsync();
            foreach (var request in new[] { input, input with { RequestFingerprint = new string('a', 64) }, input with { IdempotencyKey = Guid.NewGuid().ToString() } })
                (await repo.CreateAsync(request)).Status.Should().Be(RoadGuardSystem.Repositories.Files.UploadPersistenceStatus.NotFound);
            (await repo.GetPartUrlsAsync(owner.Id, null, first.Session!.Id, [1], Guid.NewGuid().ToString(), new string('c', 64),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15))).Status.Should().Be(RoadGuardSystem.Repositories.Files.UploadPersistenceStatus.NotFound);
            (await repo.CompleteAsync(new(owner.Id, null, first.Session.Id, first.Session.Version,
                [new CompletedStoragePart(1, "synthetic-etag")], PhotoStorage.Hash, Guid.NewGuid().ToString(), new string('d', 64), null)))
                .Status.Should().Be(RoadGuardSystem.Repositories.Files.UploadPersistenceStatus.NotFound);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == owner.Id)).Should().Be(before);
            (await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == first.Session.Id)).StorageUploadId.Should().BeNull();
        }
        // The receipt primitive clears tracking on retry; restore through a fresh
        // scoped context, not a potentially detached Role instance.
        finally { await using var restore = sql.CreateDbContext(); await restore.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Roles] SET [IsActive]={true} WHERE [Code]={UserRoleCode.Reporter.ToDbCode()}"); }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrivateReadContract_BindsOwnerBytesAndMetadata_DeniesGuessingAndRoleChangesWithoutEffects(bool content)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var owner = await sql.CreateUserAsync($"read-owner-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var outsider = await sql.CreateUserAsync($"read-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var pm = await sql.CreateUserAsync($"read-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var storage = new PhotoStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        var verified = await UploadVerified(client, factory);
        var suffix = content ? "/content" : "";
        var path = $"/api/v1/reporter-evidence/files/{verified.File}{suffix}";
        var response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var original = await response.Content.ReadAsByteArrayAsync();
        if (content)
        {
            original.Should().Equal(new byte[] { 255, 216, 255, 0 });
            response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        }
        else
        {
            using var body = JsonDocument.Parse(original);
            body.RootElement.GetProperty("id").GetGuid().Should().Be(verified.File);
            body.RootElement.GetProperty("version").GetString().Should().Be(verified.Version);
            body.RootElement.GetProperty("status").GetString().Should().Be("VERIFIED");
            body.RootElement.GetProperty("checksumSha256").GetString().Should().Be(PhotoStorage.Hash);
            body.RootElement.GetProperty("mediaType").GetString().Should().Be("image/jpeg");
            body.RootElement.GetProperty("sizeBytes").GetInt64().Should().Be(4);
            body.RootElement.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo(
                "id", "status", "checksumSha256", "mediaType", "sizeBytes", "version");
            response.Headers.ETag!.Tag.Should().Be($"\"{verified.Version}\"");
        }
        await using var beforeDb = sql.CreateDbContext();
        var before = await beforeDb.UploadSessions.AsNoTracking().Where(x => x.FileId == verified.File)
            .Select(x => new { x.Id, x.Status, x.RowVersion }).SingleAsync();
        var fileBefore = await beforeDb.Files.AsNoTracking().SingleAsync(x => x.Id == verified.File);
        var receiptsBefore = await beforeDb.IdempotencyRecords.CountAsync(x => x.ActorUserId == owner.Id);
        var presigns = storage.Presigns; var reads = storage.Reads;
        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.synthetic.token");
        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var deniedActors = new[] { outsider, pm }.ToList();
        foreach (var role in new[] { UserRoleCode.Supervisor, UserRoleCode.DroneOperator, UserRoleCode.RepairCrew })
            deniedActors.Add(await sql.CreateUserAsync($"read-role-{Guid.NewGuid():N}", "Current1!", role));
        foreach (var actor in deniedActors)
        {
            await Login(client, actor.UserName!);
            var denied = await client.GetAsync(path);
            denied.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var error = await denied.Content.ReadFromJsonAsync<JsonElement>();
            error.GetProperty("status").GetInt32().Should().Be(404);
            error.GetProperty("instance").GetString().Should().Be(path);
        }
        await Login(client, owner.UserName!);
        (await client.GetAsync($"/api/v1/reporter-evidence/files/{Guid.NewGuid()}{suffix}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/v1/reporter-evidence/files/not-a-guid{suffix}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        // Existing token must not retain reporter access after a committed role change.
        await using (var db = sql.CreateDbContext())
            await db.Users.Where(x => x.Id == owner.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleCode, UserRoleCode.ProjectManager));
        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        storage.Reads.Should().Be(reads); storage.Presigns.Should().Be(presigns);
        await using var afterDb = sql.CreateDbContext();
        var after = await afterDb.UploadSessions.AsNoTracking().Where(x => x.FileId == verified.File)
            .Select(x => new { x.Id, x.Status, x.RowVersion }).SingleAsync();
        after.Should().BeEquivalentTo(before);
        (await afterDb.Files.AsNoTracking().SingleAsync(x => x.Id == verified.File)).Should().BeEquivalentTo(fileBefore);
        (await afterDb.IdempotencyRecords.CountAsync(x => x.ActorUserId == owner.Id)).Should().Be(receiptsBefore);
        // Role restoration permits a fresh admission without altering evidence metadata or bytes.
        await afterDb.Users.Where(x => x.Id == owner.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleCode, UserRoleCode.Reporter));
        await Login(client, owner.UserName!);
        var restored = await client.GetAsync(path);
        restored.StatusCode.Should().Be(HttpStatusCode.OK);
        (await restored.Content.ReadAsByteArrayAsync()).Should().Equal(original);
    }

    [Fact]
    public async Task PrivateContentStorageFailureIsRedactedAndCannotBypassOwnerGuard()
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var owner = await sql.CreateUserAsync($"read-fault-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var outsider = await sql.CreateUserAsync($"read-denied-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var storage = new PhotoStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!); var file = await UploadVerified(client, factory);
        await using var before = sql.CreateDbContext();
        var original = await before.Files.AsNoTracking().SingleAsync(x => x.Id == file.File);
        var scope = await before.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == file.File);
        var receipts = await before.IdempotencyRecords.CountAsync();
        var reads = storage.Reads; var presigns = storage.Presigns;
        storage.FailRead = true;
        var path = $"/api/v1/reporter-evidence/files/{file.File}/content";
        var failed = await client.GetAsync(path);
        failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await failed.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(503);
        body.GetProperty("code").GetString().Should().Be("upload_storage_unavailable");
        body.GetProperty("instance").GetString().Should().Be(path);
        var text = body.ToString();
        text.Should().NotContain(original.StorageUri).And.NotContain("synthetic-private-key").And.NotContain("storage.internal.invalid");
        storage.Reads.Should().Be(reads + 1);
        await Login(client, outsider.UserName!);
        var denied = await client.GetAsync(path);
        denied.StatusCode.Should().Be(HttpStatusCode.NotFound);
        storage.Reads.Should().Be(reads + 1);
        await Login(client, owner.UserName!); storage.FailRead = false;
        var recovered = await client.GetAsync(path);
        recovered.StatusCode.Should().Be(HttpStatusCode.OK);
        (await recovered.Content.ReadAsByteArrayAsync()).Should().Equal(new byte[] { 255, 216, 255, 0 });
        await using var after = sql.CreateDbContext();
        (await after.Files.AsNoTracking().SingleAsync(x => x.Id == file.File)).Should().BeEquivalentTo(original);
        (await after.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == file.File)).Should().BeEquivalentTo(scope);
        (await after.IdempotencyRecords.CountAsync()).Should().Be(receipts);
        storage.Presigns.Should().Be(presigns);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersonalMetadataCannotBeReadUsingProjectAuthorityOrSpoofedProjectSelectors(bool content)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var owner = await sql.CreateUserAsync($"scope-owner-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync($"scope-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var managerA = await sql.CreateUserAsync($"scope-a-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var managerB = await sql.CreateUserAsync($"scope-b-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var storage = new PhotoStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        async Task<Guid> Project(Guid manager)
        {
            var created = await client.PostAsJsonAsync("/api/v1/projects", new
            {
                projectCode = $"META-{Guid.NewGuid():N}",
                name = "Metadata isolation fixture",
                engineeringUtmSrid = 32648,
                startDate = "2026-09-01",
                endDate = "2027-09-01",
                primaryProjectManagerUserId = manager,
                handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
                operationId = Guid.NewGuid()
            });
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
        }
        var a = await Project(managerA.Id); var b = await Project(managerB.Id);
        await Login(client, owner.UserName!); var file = await UploadVerified(client, factory);
        var path = $"/api/v1/reporter-evidence/files/{file.File}" + (content ? "/content" : "");
        await using var before = sql.CreateDbContext();
        var original = await before.Files.AsNoTracking().SingleAsync(x => x.Id == file.File);
        var scope = await before.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == file.File);
        scope.ProjectId.Should().BeNull(); scope.TargetId.Should().BeNull();
        var reads = storage.Reads; var presigns = storage.Presigns;
        foreach (var manager in new[] { managerA, managerB })
        {
            await Login(client, manager.UserName!);
            foreach (var project in new[] { a, b })
            {
                var denied = await client.GetAsync($"{path}?projectId={project}");
                denied.StatusCode.Should().Be(HttpStatusCode.NotFound);
                var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
                problem.GetProperty("code").GetString().Should().Be("file_not_found");
                problem.GetProperty("status").GetInt32().Should().Be(404);
                problem.GetProperty("instance").GetString().Should().Be(path);
                var text = problem.ToString(); text.Should().NotContain(original.StorageUri);
            }
        }
        await Login(client, owner.UserName!);
        foreach (var project in new[] { a, b })
        {
            var allowed = await client.GetAsync($"{path}?projectId={project}");
            allowed.StatusCode.Should().Be(HttpStatusCode.OK);
            if (content)
            {
                (await allowed.Content.ReadAsByteArrayAsync()).Should().Equal(new byte[] { 255, 216, 255, 0 });
                continue;
            }
            var body = await allowed.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(file.File);
            body.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("id", "status", "checksumSha256", "mediaType", "sizeBytes", "version");
        }
        await using var after = sql.CreateDbContext();
        (await after.Files.AsNoTracking().SingleAsync(x => x.Id == file.File)).Should().BeEquivalentTo(original);
        (await after.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == file.File)).Should().BeEquivalentTo(scope);
        storage.Reads.Should().Be(reads + (content ? 2 : 0)); storage.Presigns.Should().Be(presigns);
    }

    [Fact]
    public async Task PrivateSignedUrl_UsesFiveMinuteWindow_ReplayDoesNotRenew_ExpiryAndRevocationDenyIssuance()
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var owner = await sql.CreateUserAsync($"private-ttl-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var clock = new EvidenceClock(DateTimeOffset.UtcNow);
        var storage = new PhotoStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage);
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock);
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        var created = await Create(client, Guid.NewGuid().ToString());
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var upload = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IReporterEvidenceService>();
        var key = Guid.NewGuid().ToString(); var issuedAt = clock.GetUtcNow();
        var issued = await service.GetPartUrlsAsync(owner.Id, UserRoleCode.Reporter, upload, new([1]), key);
        issued.Status.Should().Be(UploadServiceStatus.Success);
        issued.PartUrls!.Parts.Single().ExpiresAt.Should().Be(issuedAt.AddSeconds(300));
        clock.Advance(TimeSpan.FromSeconds(120));
        var replay = await service.GetPartUrlsAsync(owner.Id, UserRoleCode.Reporter, upload, new([1]), key);
        replay.Status.Should().Be(UploadServiceStatus.Replayed);
        replay.PartUrls!.Parts.Single().ExpiresAt.Should().Be(issuedAt.AddSeconds(300));
        clock.Advance(TimeSpan.FromSeconds(180));
        var presigns = storage.Presigns;
        (await service.GetPartUrlsAsync(owner.Id, UserRoleCode.Reporter, upload, new([1]), key)).Status.Should().Be(UploadServiceStatus.Conflict);
        storage.Presigns.Should().Be(presigns);
        await using (var db = sql.CreateDbContext())
        { var actor = await db.Users.SingleAsync(x => x.Id == owner.Id); actor.Status = UserStatus.Suspended; await db.SaveChangesAsync(); }
        (await service.GetPartUrlsAsync(owner.Id, UserRoleCode.Reporter, upload, new([1]), key)).Status.Should().Be(UploadServiceStatus.NotFound);
        (await service.GetPartUrlsAsync(owner.Id, UserRoleCode.Reporter, upload, new([1]), Guid.NewGuid().ToString())).Status.Should().Be(UploadServiceStatus.NotFound);
        storage.Presigns.Should().Be(presigns);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(299)]
    [InlineData(301)]
    [InlineData(86400)]
    public async Task PrivateSignedUrl_InvalidConfiguredLifetimeFailsClosed(int seconds)
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
            services.PostConfigure<RoadGuardSystem.Repositories.Options.UploadSessionOptions>(options => options.PrivateEvidenceSignedUrlLifetimeSeconds = seconds));
        Func<Task> start = () => Task.Run(() => { using var client = factory.CreateClient(); });
        await start.Should().ThrowAsync<Microsoft.Extensions.Options.OptionsValidationException>();
    }

    private sealed class EvidenceClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan amount) => _now += amount;
    }

    [Theory]
    [InlineData("suspended")]
    [InlineData("password-change")]
    [InlineData("role-changed")]
    public async Task CurrentAuthorityLoss_DeniesPrivateReadsAndPartIssuanceWithoutStorageOrFileMutation(string change)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var owner = await sql.CreateUserAsync($"private-current-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var storage = new PhotoStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        var verified = await UploadVerified(client, factory);
        var pending = await Create(client, Guid.NewGuid().ToString());
        pending.StatusCode.Should().Be(HttpStatusCode.Created);
        var upload = (await pending.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        string before;
        await using (var db = sql.CreateDbContext())
        {
            before = JsonSerializer.Serialize(await db.UploadSessions.AsNoTracking().Where(x => x.FileId == verified.File || x.Id == upload).OrderBy(x => x.Id).ToArrayAsync());
            var actor = await db.Users.SingleAsync(x => x.Id == owner.Id);
            if (change == "suspended") actor.Status = UserStatus.Suspended;
            else if (change == "password-change") actor.MustChangePassword = true;
            else await new RoadGuardSystem.Repositories.Identity.IdentityRepository(db).ChangeUserRoleAtomicAsync(
                actor.Id, UserRoleCode.ProjectManager, actor.RowVersion, actor.Id, Guid.NewGuid(), reason: "Private evidence current-authority fixture");
            await db.SaveChangesAsync();
        }
        var reads = storage.Reads; var presigns = storage.Presigns;
        // Call service as well as HTTP: stale role arguments cannot bypass current DB authority.
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IReporterEvidenceService>();
            (await service.DownloadAsync(owner.Id, UserRoleCode.Reporter, verified.File)).Status.Should().Be(UploadServiceStatus.NotFound);
            (await service.GetFileMetadataAsync(owner.Id, UserRoleCode.Reporter, verified.File)).Status.Should().Be(UploadServiceStatus.NotFound);
            (await service.GetSessionAsync(owner.Id, UserRoleCode.Reporter, upload)).Status.Should().Be(UploadServiceStatus.NotFound);
            (await service.GetPartUrlsAsync(owner.Id, UserRoleCode.Reporter, upload,
                new UploadPartUrlsRequestDto([1]), Guid.NewGuid().ToString())).Status.Should().Be(UploadServiceStatus.NotFound);
        }
        storage.Reads.Should().Be(reads); storage.Presigns.Should().Be(presigns);
        await using var after = sql.CreateDbContext();
        JsonSerializer.Serialize(await after.UploadSessions.AsNoTracking().Where(x => x.FileId == verified.File || x.Id == upload).OrderBy(x => x.Id).ToArrayAsync()).Should().Be(before);
    }

    [Fact]
    public async Task StorageWaitCharacterization_ReporterAuthorityIsCheckedBeforeOpen_NotAfterIt()
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var owner = await sql.CreateUserAsync($"private-wait-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var storage = new PhotoStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!); var verified = await UploadVerified(client, factory);
        storage.OpenEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.ReleaseOpen = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scope = factory.Services.CreateScope();
        var download = scope.ServiceProvider.GetRequiredService<IReporterEvidenceService>().DownloadAsync(owner.Id, UserRoleCode.Reporter, verified.File);
        await storage.OpenEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await using var db = sql.CreateDbContext();
            var actor = await db.Users.SingleAsync(x => x.Id == owner.Id);
            actor.Status = UserStatus.Suspended; await db.SaveChangesAsync();
        }
        finally { storage.ReleaseOpen.SetResult(); }
        var result = await download;
        // Admission preceded revocation; no new access is admitted after the commit.
        result.Status.Should().Be(UploadServiceStatus.Success);
        await using var content = result.Content!;
        using var bytes = new MemoryStream(); await content.CopyToAsync(bytes);
        bytes.ToArray().Should().Equal(new byte[] { 255, 216, 255, 0 });
        storage.OpenEntered = null; storage.ReleaseOpen = null;
        (await scope.ServiceProvider.GetRequiredService<IReporterEvidenceService>().DownloadAsync(owner.Id, UserRoleCode.Reporter, verified.File)).Status.Should().Be(UploadServiceStatus.NotFound);
    }

    [Fact]
    public async Task VerifiedPrivateEvidence_AccountSuspensionRejectsExistingBearerWithoutChangingFile()
    {
        var owner = await sql.CreateUserAsync($"private-temporal-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new PhotoStorage()); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        var verified = await UploadVerified(client, factory);
        var metadataPath = $"/api/v1/reporter-evidence/files/{verified.File}";
        (await client.GetAsync(metadataPath + "/content")).StatusCode.Should().Be(HttpStatusCode.OK);
        var before = await client.GetAsync(metadataPath);
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        var beforeBody = await before.Content.ReadAsStringAsync();
        var beforeEtag = before.Headers.ETag;

        await using (var db = sql.CreateDbContext())
        {
            var actor = await db.Users.SingleAsync(row => row.Id == owner.Id);
            actor.Status = UserStatus.Suspended;
            await db.SaveChangesAsync();
        }
        // Keep the original bearer: current account authority must be checked per request.
        (await client.GetAsync(metadataPath)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync(metadataPath + "/content")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await using (var db = sql.CreateDbContext())
        {
            var actor = await db.Users.SingleAsync(row => row.Id == owner.Id);
            actor.Status = UserStatus.Active;
            await db.SaveChangesAsync();
        }
        var after = await client.GetAsync(metadataPath);
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        after.Headers.ETag.Should().Be(beforeEtag);
        (await after.Content.ReadAsStringAsync()).Should().Be(beforeBody);
        // This covers fresh API authorization, not revocation of already issued object URLs.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrivateIntake_OwnerOnly_Replay_CurrentAuthority_AndVerification(bool failVerification)
    {
        var owner = await sql.CreateUserAsync($"reporter-evidence-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var other = await sql.CreateUserAsync($"reporter-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var storage = new PhotoStorage { FailVerification = failVerification };
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        var key = Guid.NewGuid().ToString();
        var created = await Create(client, key);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await created.Content.ReadFromJsonAsync<JsonElement>();
        var upload = json.GetProperty("id").GetGuid(); var file = json.GetProperty("fileId").GetGuid();
        var pendingMetadata = await client.GetAsync($"/api/v1/reporter-evidence/files/{file}");
        pendingMetadata.StatusCode.Should().Be(HttpStatusCode.OK);
        (await pendingMetadata.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("PENDING");
        (await Create(client, key)).StatusCode.Should().Be(HttpStatusCode.Created);
        using (var producerScope = factory.Services.CreateScope())
        {
            var producer = producerScope.ServiceProvider.GetRequiredService<IAnhHuyProducerService>();
            (await producer.ResolvePrivateEvidenceAsync(owner.Id, UserRoleCode.Reporter, file, Guid.NewGuid())).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
            (await producer.ResolvePrivateEvidenceAsync(other.Id, UserRoleCode.Reporter, file, Guid.NewGuid())).Status.Should().Be(AnhHuyProducerStatus.NotFound);
        }
        (await client.GetAsync($"/api/v1/reporter-evidence/files/{file}/content")).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.GetAsync($"/api/v1/files/{file}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v1/uploads/{upload}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Login(client, other.UserName!);
        (await client.GetAsync($"/api/v1/reporter-evidence/uploads/{upload}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/v1/reporter-evidence/files/{file}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var outsiderParts = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/part-urls") { Content = JsonContent.Create(new { partNumbers = new[] { 1 } }) };
        outsiderParts.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await client.SendAsync(outsiderParts)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var outsiderComplete = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/complete") { Content = JsonContent.Create(new { checksumSha256 = PhotoStorage.Hash, parts = new[] { new { partNumber = 1, eTag = "part" } } }) };
        outsiderComplete.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); outsiderComplete.Headers.TryAddWithoutValidation("If-Match", created.Headers.ETag!.ToString());
        (await client.SendAsync(outsiderComplete)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await Login(client, owner.UserName!);
        using var parts = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/part-urls") { Content = JsonContent.Create(new { partNumbers = new[] { 1 } }) };
        parts.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await client.SendAsync(parts)).StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await client.GetAsync($"/api/v1/reporter-evidence/uploads/{upload}");
        using var complete = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/complete") { Content = JsonContent.Create(new { checksumSha256 = PhotoStorage.Hash, parts = new[] { new { partNumber = 1, eTag = "part" } } }) };
        var completeKey = Guid.NewGuid().ToString();
        complete.Headers.Add("Idempotency-Key", completeKey); complete.Headers.TryAddWithoutValidation("If-Match", session.Headers.ETag!.ToString());
        (await client.SendAsync(complete)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        using var replay = new HttpRequestMessage(HttpMethod.Post, complete.RequestUri) { Content = JsonContent.Create(new { checksumSha256 = PhotoStorage.Hash, parts = new[] { new { partNumber = 1, eTag = "part" } } }) };
        replay.Headers.Add("Idempotency-Key", completeKey); replay.Headers.TryAddWithoutValidation("If-Match", session.Headers.ETag!.ToString());
        (await client.SendAsync(replay)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var metadata = await client.GetAsync($"/api/v1/reporter-evidence/files/{file}");
        var metadataText = await metadata.Content.ReadAsStringAsync();
        metadataText.Should().NotContain("objectKey").And.NotContain("storageUri");
        JsonDocument.Parse(metadataText).RootElement.GetProperty("status").GetString().Should().Be(failVerification ? "FAILED" : "VERIFIED");
        (await client.GetAsync($"/api/v1/reporter-evidence/files/{file}/content")).StatusCode.Should().Be(failVerification ? HttpStatusCode.Conflict : HttpStatusCode.OK);
        using (var producerScope = factory.Services.CreateScope())
        {
            var producer = producerScope.ServiceProvider.GetRequiredService<IAnhHuyProducerService>();
            var facts = await producer.ResolvePrivateEvidenceAsync(owner.Id, UserRoleCode.Reporter, file, Guid.NewGuid());
            facts.Status.Should().Be(failVerification ? AnhHuyProducerStatus.SourceNotReady : AnhHuyProducerStatus.Ready);
            if (!failVerification)
            {
                facts.Facts!.ChecksumSha256.Should().Be(PhotoStorage.Hash);
                facts.Facts.SizeBytes.Should().Be(4);
                facts.Facts.ProjectId.Should().BeNull();
                facts.Facts.Reference.OwnerUserId.Should().Be(owner.Id);
                (await producer.ResolvePrivateEvidenceAsync(owner.Id, UserRoleCode.Reporter, file, Guid.NewGuid(), "stale-file-version")).Status.Should().Be(AnhHuyProducerStatus.StaleFile);
            }
        }
        await using var db = sql.CreateDbContext();
        ((Guid?)(await db.FileScopes.SingleAsync(x => x.FileId == file)).ProjectId).Should().BeNull();
        var user = await db.Users.SingleAsync(x => x.Id == owner.Id);
        user.MustChangePassword = true; await db.SaveChangesAsync();
        using (var currentAuthority = factory.Services.CreateScope())
            (await currentAuthority.ServiceProvider.GetRequiredService<IReporterEvidenceService>().CreateAsync(owner.Id, UserRoleCode.Reporter,
                new ReporterEvidenceCreateRequestDto("photo.jpg", "image/jpeg", 4, PhotoStorage.Hash), key, null)).Status.Should().Be(UploadServiceStatus.Forbidden);
        user.MustChangePassword = false; user.Status = UserStatus.Suspended; await db.SaveChangesAsync();
        using (var currentAuthority = factory.Services.CreateScope())
            (await currentAuthority.ServiceProvider.GetRequiredService<IReporterEvidenceService>().CreateAsync(owner.Id, UserRoleCode.Reporter,
                new ReporterEvidenceCreateRequestDto("photo.jpg", "image/jpeg", 4, PhotoStorage.Hash), key, null)).Status.Should().Be(UploadServiceStatus.Forbidden);
        (await Create(client, key)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Theory]
    [InlineData(20971520L, "image/jpeg", HttpStatusCode.Created)]
    [InlineData(20971521L, "image/jpeg", HttpStatusCode.UnprocessableEntity)]
    [InlineData(4L, "application/pdf", HttpStatusCode.UnprocessableEntity)]
    public async Task AdmissionLimits_AndNoClientScope(long bytes, string mime, HttpStatusCode expected)
    {
        var owner = await sql.CreateUserAsync($"reporter-limit-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reporter-evidence/uploads") { Content = JsonContent.Create(new { fileName = "photo.jpg", mediaType = mime, sizeBytes = bytes, checksumSha256 = PhotoStorage.Hash }) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await client.SendAsync(request)).StatusCode.Should().Be(expected);
        using var spoof = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reporter-evidence/uploads") { Content = JsonContent.Create(new { fileName = "photo.jpg", mediaType = "image/jpeg", sizeBytes = 4, checksumSha256 = PhotoStorage.Hash, projectId = Guid.NewGuid(), ownerUserId = Guid.NewGuid() }) };
        spoof.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await client.SendAsync(spoof)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task ProjectRoles_CannotReachPrivateScopeThroughEitherRoute()
    {
        var owner = await sql.CreateUserAsync($"reporter-scope-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var pm = await sql.CreateUserAsync($"reporter-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, owner.UserName!);
        var created = await Create(client, Guid.NewGuid().ToString());
        var json = await created.Content.ReadFromJsonAsync<JsonElement>();
        var file = json.GetProperty("fileId").GetGuid(); var upload = json.GetProperty("id").GetGuid();
        await Login(client, pm.UserName!);
        (await client.GetAsync($"/api/v1/files/{file}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v1/uploads/{upload}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v1/reporter-evidence/files/{file}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Create(client, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    [Fact]
    public async Task PublicationDownload_IsRecipientScoped_AndTriageDoesNotPublicizeFile()
    {
        var owner = await sql.CreateUserAsync($"publication-owner-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var other = await sql.CreateUserAsync($"publication-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var pm = await sql.CreateUserAsync($"publication-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync($"publication-supervisor-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new PhotoStorage()); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var projectResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"PUB-{Guid.NewGuid():N}",
            name = "Publication fixture",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = pm.Id,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        projectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await projectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
        await Login(client, owner.UserName!); var owned = await UploadVerified(client, factory);
        await Login(client, other.UserName!); var otherFile = await UploadVerified(client, factory);
        var reportA = Report.Create(Guid.NewGuid(), owner.Id, "Owner source", DateTimeOffset.UtcNow,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), owned.File, owned.Version, owner.Id)]);
        var reportB = Report.Create(Guid.NewGuid(), other.Id, "Other source", DateTimeOffset.UtcNow,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), otherFile.File, otherFile.Version, other.Id)]);
        var evidenceA = reportA.OriginalEvidence.Single().Id; var evidenceB = reportB.OriginalEvidence.Single().Id;
        // Fixture-owned relational setup only; this does not exercise Huy command orchestration.
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), reportA.Id, DateTimeOffset.UtcNow);
        incident.Triage(project, CaseVerificationMethod.ExistingEvidence, "Fixture triage", DateTimeOffset.UtcNow);
        var source = IncidentCase.CreateUnassigned(Guid.NewGuid(), reportB.Id, DateTimeOffset.UtcNow);
        source.Triage(project, CaseVerificationMethod.ExistingEvidence, "Fixture triage", DateTimeOffset.UtcNow);
        incident.LinkReportsFrom(source, [reportB.Id], pm.Id, "Fixture linked recipients", DateTimeOffset.UtcNow);
        incident.Conclude(CaseConclusion.Create(Guid.NewGuid(), CaseConclusionOutcome.NoDefect, [], [evidenceA], "Fixture no defect", DateTimeOffset.UtcNow),
            CaseConclusionPrerequisites.Create([], [evidenceA]));
        var pubA = incident.Publish(Guid.NewGuid(), [reportA.Id], [], [evidenceA], "Owner publication", DateTimeOffset.UtcNow,
            CasePublicationPrerequisites.Create([CasePublicationRecipientFacts.Create(reportA.Id, [], [evidenceA])]));
        var pubB = incident.Publish(Guid.NewGuid(), [reportB.Id], [], [evidenceB], "Other publication", DateTimeOffset.UtcNow,
            CasePublicationPrerequisites.Create([CasePublicationRecipientFacts.Create(reportB.Id, [], [evidenceB])]));
        // Adversarial fixture: snapshot claims owner A, actual referenced verified file belongs to B.
        // This bypasses the absent Huy attach command deliberately to verify the producer fails closed.
        var mismatchReport = Report.Create(Guid.NewGuid(), owner.Id, "Fixture incorrect owner snapshot", DateTimeOffset.UtcNow,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), otherFile.File, otherFile.Version, owner.Id)]);
        var mismatchEvidence = mismatchReport.OriginalEvidence.Single().Id;
        var mismatchCase = IncidentCase.CreateUnassigned(Guid.NewGuid(), mismatchReport.Id, DateTimeOffset.UtcNow);
        mismatchCase.Triage(project, CaseVerificationMethod.ExistingEvidence, "Fixture", DateTimeOffset.UtcNow);
        mismatchCase.Conclude(CaseConclusion.Create(Guid.NewGuid(), CaseConclusionOutcome.NoDefect, [], [mismatchEvidence], "Fixture", DateTimeOffset.UtcNow),
            CaseConclusionPrerequisites.Create([], [mismatchEvidence]));
        var mismatchPublication = mismatchCase.Publish(Guid.NewGuid(), [mismatchReport.Id], [], [mismatchEvidence], "Fixture", DateTimeOffset.UtcNow,
            CasePublicationPrerequisites.Create([CasePublicationRecipientFacts.Create(mismatchReport.Id, [], [mismatchEvidence])]));
        await using (var db = sql.CreateDbContext())
        {
            db.Reports.AddRange(reportA, reportB, mismatchReport); db.IncidentCases.AddRange(incident, mismatchCase);
            db.Set<HuyPublicationRecipient>().Add(new HuyPublicationRecipient { PublicationId = mismatchPublication.Id, ReportId = mismatchReport.Id });
            db.Set<HuyPublicationEvidence>().Add(new HuyPublicationEvidence
            {
                PublicationId = mismatchPublication.Id,
                RecipientReportId = mismatchReport.Id,
                EvidenceId = mismatchEvidence,
                SourceReportId = mismatchReport.Id,
                OriginalEvidenceId = mismatchEvidence
            });
            db.Set<HuyPublicationRecipient>().AddRange(new HuyPublicationRecipient { PublicationId = pubA.Id, ReportId = reportA.Id }, new HuyPublicationRecipient { PublicationId = pubB.Id, ReportId = reportB.Id });
            db.Set<HuyPublicationEvidence>().AddRange(new HuyPublicationEvidence { PublicationId = pubA.Id, RecipientReportId = reportA.Id, EvidenceId = evidenceA, SourceReportId = reportA.Id, OriginalEvidenceId = evidenceA },
                new HuyPublicationEvidence { PublicationId = pubB.Id, RecipientReportId = reportB.Id, EvidenceId = evidenceB, SourceReportId = reportB.Id, OriginalEvidenceId = evidenceB });
            await db.SaveChangesAsync();
        }
        await Login(client, pm.UserName!);
        (await client.GetAsync($"/api/v1/files/{owned.File}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Login(client, owner.UserName!);
        var pathA = $"/api/v1/reporter-evidence/publications/{pubA.Id}/reports/{reportA.Id}/evidence/{evidenceA}/content";
        (await client.GetAsync(pathA)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/reporter-evidence/publications/{mismatchPublication.Id}/reports/{mismatchReport.Id}/evidence/{mismatchEvidence}/content")).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.GetAsync($"/api/v1/reporter-evidence/publications/{pubB.Id}/reports/{reportB.Id}/evidence/{evidenceB}/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await Login(client, other.UserName!);
        (await client.GetAsync(pathA)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/v1/reporter-evidence/publications/{pubA.Id}/reports/{reportB.Id}/evidence/{evidenceA}/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/v1/reporter-evidence/publications/{pubB.Id}/reports/{reportB.Id}/evidence/{evidenceA}/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/v1/reporter-evidence/publications/{pubB.Id}/reports/{reportB.Id}/evidence/{evidenceB}/content")).StatusCode.Should().Be(HttpStatusCode.OK);
        // Deliberate version drift in an isolated SQL fixture. No Huy command or shared DB mutation.
        await using (var drift = sql.CreateDbContext())
            await drift.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET FailureCode = 'fixture_version_drift' WHERE FileId = {owned.File}");
        await Login(client, owner.UserName!);
        (await client.GetAsync(pathA)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        await Login(client, other.UserName!);
        (await client.GetAsync(pathA)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    private static async Task<(Guid File, string Version)> UploadVerified(HttpClient client, AuthenticationWebApplicationFactory factory)
    {
        var create = await Create(client, Guid.NewGuid().ToString()); create.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await create.Content.ReadFromJsonAsync<JsonElement>(); var upload = json.GetProperty("id").GetGuid(); var file = json.GetProperty("fileId").GetGuid();
        using var parts = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/part-urls") { Content = JsonContent.Create(new { partNumbers = new[] { 1 } }) };
        parts.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); (await client.SendAsync(parts)).StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await client.GetAsync($"/api/v1/reporter-evidence/uploads/{upload}");
        using var complete = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{upload}/complete") { Content = JsonContent.Create(new { checksumSha256 = PhotoStorage.Hash, parts = new[] { new { partNumber = 1, eTag = "part" } } }) };
        complete.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); complete.Headers.TryAddWithoutValidation("If-Match", session.Headers.ETag!.ToString());
        (await client.SendAsync(complete)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var metadata = await client.GetAsync($"/api/v1/reporter-evidence/files/{file}");
        return (file, (await metadata.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()!);
    }
    private static async Task Login(HttpClient client, string name)
    { var r = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(name), password = "Current1!" }); r.EnsureSuccessStatusCode(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()); }
    private static Task<HttpResponseMessage> Create(HttpClient client, string key)
    { var r = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reporter-evidence/uploads") { Content = JsonContent.Create(new { fileName = "photo.jpg", mediaType = "image/jpeg", sizeBytes = 4, checksumSha256 = PhotoStorage.Hash }) }; r.Headers.Add("Idempotency-Key", key); return client.SendAsync(r); }
    private sealed class PhotoStorage : IUploadObjectStorage
    {
        public int Reads { get; private set; }
        public int Presigns { get; private set; }
        public TaskCompletionSource? OpenEntered { get; set; }
        public TaskCompletionSource? ReleaseOpen { get; set; }
        public bool FailVerification { get; init; }
        public bool FailRead { get; set; }
        public static string Hash => Convert.ToHexString(SHA256.HashData(new byte[] { 255, 216, 255, 0 })).ToLowerInvariant();
        public Task<string> InitiateAsync(string key, string type, CancellationToken cancellationToken = default) => Task.FromResult("multipart");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string key, string id, IReadOnlyList<int> parts, DateTimeOffset expires, CancellationToken cancellationToken = default) { Presigns++; return Task.FromResult<IReadOnlyList<PresignedUploadPart>>(parts.Select(x => new PresignedUploadPart(x, "https://storage.invalid/part", expires)).ToArray()); }
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string key, string id, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(FailVerification ? 5 : 4, Hash, "image/jpeg"));
        public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            Reads++; OpenEntered?.TrySetResult();
            if (FailRead) throw new FileStorageException("synthetic_unavailable", "synthetic-private-key https://storage.internal.invalid/object");
            if (ReleaseOpen is not null) await ReleaseOpen.Task.WaitAsync(cancellationToken);
            return new MemoryStream(new byte[] { 255, 216, 255, 0 });
        }
    }
}
