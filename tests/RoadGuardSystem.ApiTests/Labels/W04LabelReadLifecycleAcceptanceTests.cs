using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
using Xunit.Abstractions;

using RoadGuardSystem.BusinessObjects.Labels;
using RoadGuardSystem.Repositories.Implementations.Labels;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.ApiTests.Labels;



// Repository fixtures exercise current-head HTTP projection, not source eligibility.
[Collection(AuthenticationApiFixture.Name)]
public sealed class W04LabelReadLifecycleAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private readonly AuthenticationSqlServerFixture _sql = sql;
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadsFollowCurrentRevisionAndPreservePrivateHistoryAcrossHostRestart(bool cookie)
    {
        var scope = await SeedScopeAsync(); var job = await CreateJobAsync(scope);
        var id = Guid.NewGuid(); var detection = Guid.NewGuid();
        string email; string firstVersion;
        await using (var db = _sql.CreateDbContext())
        {
            email = (await db.Users.SingleAsync(x => x.Id == scope.ActorId)).Email!;
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = scope.ProjectId,
                UserId = scope.ActorId,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            });
            db.Add(AIDetection.Create(detection, job.Id, scope.ModelId, null, null, "POTHOLE", 0.9m, null, null, "{}"));
            await db.SaveChangesAsync();
            var repo = new TrainingLabelRepository(db);
            var initial = await repo.CreateAsync(scope.ActorId,
                TrainingLabel.Create(id, scope.ProjectId, detection, "AI_DETECTION", "history-source-v1", scope.RawFileId,
                    scope.Checksum, 0.1m, 0.2m, 0.3m, 0.4m, "POTHOLE", "private-old-reason"), "private-old-reason", null, default);
            var approved = await repo.ReviewAsync(scope.ActorId, scope.ProjectId, id, initial.Version,
                TrainingLabelReviewStatus.Approved, "private-old-review", null, default);
            firstVersion = approved.Version;
        }
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        var route = $"/api/v1/projects/{scope.ProjectId}/labels";
        string oldRows;
        await using (var db = _sql.CreateDbContext()) oldRows = await History(db);
        await using (var host = Host())
        {
            using var client = await Login(host);
            await Read(client, 1, "APPROVED", "history-source-v1", 0.1m, "private-old-review", firstVersion);
            await using var db = _sql.CreateDbContext();
            var revised = await new TrainingLabelRepository(db).ReviseAsync(scope.ActorId, scope.ProjectId, id, firstVersion,
                "current-source-v2", scope.RawFileId, scope.Checksum,
                new("BBOX", "NORMALIZED", 0.5m, 0.1m, 0.2m, 0.2m), "POTHOLE", "current-revision", null, default);
            revised.Version.Should().NotBe(firstVersion);
            await Read(client, 2, "PENDING", "current-source-v2", 0.5m, null, revised.Version);
        }
        // New host, same owned SQL: no in-memory cache or history fallback may supply the response.
        await using (var host = Host())
        {
            using var client = await Login(host);
            await using var db = _sql.CreateDbContext();
            var repo = new TrainingLabelRepository(db);
            var current = await repo.CurrentAsync(scope.ProjectId, id, default);
            await Read(client, 2, "PENDING", "current-source-v2", 0.5m, null, current!.Version);
            var reviewed = await repo.ReviewAsync(scope.ActorId, scope.ProjectId, id, current.Version,
                TrainingLabelReviewStatus.Rejected, "current-review", null, default);
            await Read(client, 2, "REJECTED", "current-source-v2", 0.5m, "current-review", reviewed.Version);
            (await History(db)).Should().Be(oldRows);
            (await db.Set<HuyTrainingLabelRevision>().CountAsync(x => x.LabelId == id)).Should().Be(2);
            (await db.Set<HuyTrainingLabelReview>().CountAsync(x => x.LabelId == id)).Should().Be(2);
            // Cookie and bearer use the same current project scope check on every fresh read.
            await db.ProjectMembers.Where(x => x.ProjectId == scope.ProjectId && x.UserId == scope.ActorId)
                .ExecuteUpdateAsync(x => x.SetProperty(v => v.ValidTo, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
            foreach (var path in new[] { route, route + "/" + id })
            {
                using var denied = await client.GetAsync(path);
                denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("access_forbidden");
            }
        }

        AuthenticationWebApplicationFactory Host() => new(_sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
                services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock);
            });
        async Task<HttpClient> Login(AuthenticationWebApplicationFactory host)
        {
            if (cookie) return await WebCookieTestClient.Login(host, email);
            var client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            return client;
        }
        async Task Read(HttpClient client, int revision, string status, string source, decimal x, string? review, string version)
        {
            using var detail = await client.GetAsync(route + "/" + id);
            detail.StatusCode.Should().Be(HttpStatusCode.OK);
            detail.Headers.ETag!.Tag.Should().Be('"' + version + '"');
            Check(await detail.Content.ReadFromJsonAsync<JsonElement>());
            using var list = await client.GetAsync(route);
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await list.Content.ReadFromJsonAsync<JsonElement>();
            Check(page.GetProperty("items").EnumerateArray().Should().ContainSingle().Subject);
            page.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
            void Check(JsonElement body)
            {
                body.GetProperty("id").GetGuid().Should().Be(id);
                body.GetProperty("revision").GetInt32().Should().Be(revision);
                body.GetProperty("status").GetString().Should().Be(status);
                body.GetProperty("sourceVersion").GetString().Should().Be(source);
                body.GetProperty("annotation").GetProperty("x").GetDecimal().Should().Be(x);
                body.GetProperty("version").GetString().Should().Be(version);
                body.GetProperty("reviewReason").GetString().Should().Be(review);
                if (review is null)
                    foreach (var field in new[] { "reviewedBy", "reviewedAt" }) body.GetProperty(field).ValueKind.Should().Be(JsonValueKind.Null);
                body.TryGetProperty("history", out _).Should().BeFalse(); body.TryGetProperty("revisions", out _).Should().BeFalse();
                if (revision == 2) body.GetRawText().Should().NotContain("history-source-v1").And.NotContain("private-old-review").And.NotContain("private-old-reason");
            }
        }
        async Task<string> History(RoadGuardSystem.Repositories.RoadGuardDbContext db)
        {
            var revision = await db.Set<HuyTrainingLabelRevision>().AsNoTracking().SingleAsync(x => x.LabelId == id && x.Revision == 1);
            var review = await db.Set<HuyTrainingLabelReview>().AsNoTracking().SingleAsync(x => x.RevisionId == revision.Id);
            return JsonSerializer.Serialize(new { revision, review });
        }
    }
    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private async Task<SeededJob> CreateJobAsync(Scope scope)
    {
        await using var context = _sql.CreateDbContext();
        var repository = new ProcessingV2PersistenceService(context, new IdempotencyOperationService(context));
        var key = Guid.NewGuid().ToString("N");
        var result = await repository.CreateAsync(new(scope.ActorId, scope.DatasetId, "{}", scope.ModelId, "pre-v1", "config-v1", "MOCK", key, Hash(key), null));
        result.Status.Should().Be(ProcessingJobPersistenceStatus.Success);
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync(value => value.Id == result.Job!.Id);
        return new SeededJob(job.Id, job.ProjectId, job.ManifestHash, Convert.ToBase64String(job.RowVersion));
    }

    private async Task<Scope> SeedScopeAsync()
    {
        // Reuse RF-10-05's entity setup; jobs/attempts are created by the production repository.
        await using var context = _sql.CreateDbContext();
        var actor = await _sql.CreateUserAsync("a0801_" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = "A0801-" + Guid.NewGuid().ToString("N"), Name = "Isolated callback characterization", EngineeringUtmSrid = 32648, Status = ProjectStatus.Active, CreatedAt = now };
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, "ROAD-" + Guid.NewGuid().ToString("N"));
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "fixture-model", Guid.NewGuid().ToString("N"), "file:///fixture-model", null, null, AIModelVersionStatus.Released, now, actor.Id);
        var checksum = new string('a', 64);
        var file = StoredFile.Create(Guid.NewGuid(), "objects/a0801/" + Guid.NewGuid().ToString("N"), "result.json", "application/json", 32, checksum, actor.Id, now, null);
        var fileScope = FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, actor.Id, "AI_RESULT", now);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor.Id, file.StorageUri, "AI_RESULT", "application/json", 32, checksum, 32, now.AddHours(1));
        context.AddRange(project, section, version, survey, dataset, model, file, fileScope, upload);
        if (!await context.DefectTypes.AnyAsync(value => value.Code == "POTHOLE")) context.Add(DefectType.Create("POTHOLE", "Pothole"));
        await context.SaveChangesAsync();
        upload.StartUploading("fixture-upload", now);
        await context.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await context.SaveChangesAsync();
        upload.MarkVerified();
        await context.SaveChangesAsync();
        return new Scope(project.Id, actor.Id, dataset.Id, model.Id, file.Id, checksum);
    }


    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private sealed record Scope(Guid ProjectId, Guid ActorId, Guid DatasetId, Guid ModelId, Guid RawFileId, string Checksum);
    private sealed record SeededJob(Guid Id, Guid ProjectId, string ManifestHash, string Version);
}
