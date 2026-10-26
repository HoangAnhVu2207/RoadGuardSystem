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



// Repository-created read data; not source eligibility or a label mutation journey.
[Collection(AuthenticationApiFixture.Name)]
public sealed class W04LabelReadQueryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private readonly AuthenticationSqlServerFixture _sql = sql;
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LabelReadQueryDefaultsBoundsAndRouteConstraints(bool cookie)
    {
        var scope = await SeedScopeAsync(); var job = await CreateJobAsync(scope);
        var ids = Enumerable.Range(0, 52).Select(_ => Guid.NewGuid()).ToArray();
        string email;
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
            var sources = ids.ToDictionary(x => x, _ => Guid.NewGuid());
            foreach (var id in ids) db.Add(AIDetection.Create(sources[id], job.Id, scope.ModelId, null, null, "POTHOLE", 0.9m, null, null, "{}"));
            await db.SaveChangesAsync();
            var repo = new TrainingLabelRepository(db);
            foreach (var id in ids) await repo.CreateAsync(scope.ActorId,
                TrainingLabel.Create(id, scope.ProjectId, sources[id], "AI_DETECTION", "query-read-source", scope.RawFileId,
                    scope.Checksum, 0.1m, 0.2m, 0.3m, 0.4m, "POTHOLE", "private-fixture"), "private-fixture", null, default);
        }
        await using var host = new AuthenticationWebApplicationFactory(_sql.ConnectionString,
            configureTestServices: services => services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>());
        using var client = cookie ? await WebCookieTestClient.Login(host, email) : host.CreateClient(new()
        { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        if (!cookie)
        {
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var root = $"/api/v1/projects/{scope.ProjectId}/labels";
        var before = await Snapshot();
        var defaultPage = await Page("", 50);
        var explicitPage = await Page("?pageSize=50", 50);
        defaultPage.GetRawText().Should().Be(explicitPage.GetRawText());
        (await Page("?cursor=", 50)).GetRawText().Should().Be(defaultPage.GetRawText());
        var cursor = defaultPage.GetProperty("nextCursor").GetString(); cursor.Should().NotBeNullOrEmpty();
        var tail = await Page("?cursor=" + Uri.EscapeDataString(cursor!), 2);
        tail.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        var combined = defaultPage.GetProperty("items").EnumerateArray().Concat(tail.GetProperty("items").EnumerateArray())
            .Select(x => x.GetProperty("id").GetGuid()).ToArray();
        combined.Should().OnlyHaveUniqueItems().And.BeEquivalentTo(ids);
        await Page("?pageSize=1", 1);
        var max = await Page("?pageSize=200", 52);
        max.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        var emptyCursor = Convert.ToBase64String(new byte[16]);
        (await Page("?pageSize=200&cursor=" + Uri.EscapeDataString(emptyCursor), 52)).GetRawText().Should().Be(max.GetRawText());
        foreach (var query in new[] { "pageSize=0", "pageSize=-1", "pageSize=201", "pageSize=2147483648",
            "pageSize=1.5", "pageSize=invalid", "cursor=bad", "cursor=YWJj" })
            await Problem(root + "?" + query, 400, "validation_error");
        foreach (var missing in new[] { Guid.Empty, Guid.NewGuid() })
            await Problem(root + "/" + missing, 404, "not_found");
        foreach (var path in new[] { root + "/not-a-guid", "/api/v1/projects/not-a-guid/labels",
            "/api/v1/projects/not-a-guid/labels/" + ids[0] })
        {
            using var invalidRoute = await client.GetAsync(path);
            invalidRoute.StatusCode.Should().Be(HttpStatusCode.NotFound);
            invalidRoute.Headers.ETag.Should().BeNull(); invalidRoute.Headers.Location.Should().BeNull();
            (await invalidRoute.Content.ReadAsStringAsync()).Should().NotContain("query-read-source").And.NotContain(scope.Checksum);
        }
        (await Snapshot()).Should().Be(before);

        async Task<JsonElement> Page(string query, int count)
        {
            using var response = await client.GetAsync(root + query);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("items", "nextCursor");
            body.GetProperty("items").GetArrayLength().Should().Be(count);
            foreach (var item in body.GetProperty("items").EnumerateArray()) ids.Should().Contain(item.GetProperty("id").GetGuid());
            return body;
        }
        async Task Problem(string path, int status, string code)
        {
            using var response = await client.GetAsync(path);
            ((int)response.StatusCode).Should().Be(status, "request {0}", path);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("status").GetInt32().Should().Be(status);
            body.GetProperty("code").GetString().Should().Be(code);
            body.GetProperty("instance").GetString().Should().Be(path.Split('?')[0]);
            body.GetProperty("correlationId").GetString().Should().Be(response.Headers.GetValues("X-Correlation-ID").Single());
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            body.GetRawText().Should().NotContain("query-read-source").And.NotContain("private-fixture").And.NotContain(scope.Checksum);
        }
        async Task<string> Snapshot()
        {
            await using var db = _sql.CreateDbContext();
            var heads = await db.Set<HuyTrainingLabelHead>().AsNoTracking().Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync();
            var revisions = await db.Set<HuyTrainingLabelRevision>().AsNoTracking().Where(x => ids.Contains(x.LabelId)).OrderBy(x => x.Id).ToListAsync();
            var reviews = await db.Set<HuyTrainingLabelReview>().AsNoTracking().Where(x => ids.Contains(x.LabelId)).OrderBy(x => x.Id).ToListAsync();
            var audits = await db.AuditLogs.AsNoTracking().Where(x => ids.Contains(x.EntityId)).OrderBy(x => x.Id).ToListAsync();
            var receipts = await db.IdempotencyRecords.AsNoTracking().Where(x => x.ProjectId == scope.ProjectId).OrderBy(x => x.Id).ToListAsync();
            return JsonSerializer.Serialize(new { heads, revisions, reviews, audits, receipts });
        }
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
