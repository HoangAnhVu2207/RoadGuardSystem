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


// Direct domain/repository fixtures prove GET projection and current authority only.
[Collection(AuthenticationApiFixture.Name)]
public sealed class W04LabelReadWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private readonly AuthenticationSqlServerFixture _sql = sql;
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LabelReadsHaveExactWirePaginationStateAndCurrentAuthority(bool cookie)
    {
        var scope = await SeedScopeAsync();
        var job = await CreateJobAsync(scope);
        var outsider = await _sql.CreateUserAsync("label-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var other = Project.Create(Guid.NewGuid(), "LABEL-OTHER-" + Guid.NewGuid().ToString("N"), "Other owned project", null, null, null, null, DateTimeOffset.UtcNow);
        var membership = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var sources = ids.ToDictionary(x => x, _ => Guid.NewGuid());
        var states = new[] { "PENDING", "APPROVED", "REJECTED" };
        var expected = ids.Select((id, i) => (id, state: states[i])).ToDictionary(x => x.id, x => x.state);
        await using (var setup = _sql.CreateDbContext())
        {
            setup.Add(other);
            setup.Add(new ProjectMember
            {
                Id = membership,
                ProjectId = scope.ProjectId,
                UserId = scope.ActorId,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            });
            setup.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = other.Id,
                UserId = outsider.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            });
            foreach (var id in ids) setup.Add(AIDetection.Create(sources[id], job.Id, scope.ModelId, null, null, "POTHOLE", 0.9m, null, null, "{}"));
            await setup.SaveChangesAsync();
            var repository = new TrainingLabelRepository(setup);
            for (var i = 0; i < ids.Length; i++)
            {
                var label = TrainingLabel.Create(ids[i], scope.ProjectId, sources[ids[i]], "AI_DETECTION", "owned-source-version",
                    scope.RawFileId, scope.Checksum, 0.1m, 0.2m, 0.3m, 0.4m, "POTHOLE", "Read-only fixture");
                var created = await repository.CreateAsync(scope.ActorId, label, "Read-only fixture", null, default);
                if (i > 0) await repository.ReviewAsync(scope.ActorId, scope.ProjectId, ids[i], created.Version,
                    i == 1 ? TrainingLabelReviewStatus.Approved : TrainingLabelReviewStatus.Rejected,
                    "Owned fixture review", null, default);
            }
        }
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString,
            configureTestServices: services => services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>());
        string email;
        await using (var db = _sql.CreateDbContext()) email = (await db.Users.AsNoTracking().SingleAsync(x => x.Id == scope.ActorId)).Email!;
        using var client = await Login(email);
        using var foreign = await Login(outsider.Email!);
        var root = $"/api/v1/projects/{scope.ProjectId}/labels";
        var before = await Snapshot();
        foreach (var id in ids)
        {
            using var response = await client.GetAsync(root + "/" + id);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Check(body);
            body.GetProperty("id").GetGuid().Should().Be(id);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            response.Headers.ETag!.IsWeak.Should().BeFalse();
            response.Headers.ETag.Tag.Should().Be('"' + body.GetProperty("version").GetString() + '"');
            await using var db = _sql.CreateDbContext();
            var version = await db.Set<HuyTrainingLabelHead>().Where(x => x.Id == id).Select(x => x.RowVersion).SingleAsync();
            body.GetProperty("version").GetString().Should().Be(Convert.ToBase64String(version));
            response.Headers.Location.Should().BeNull();
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        }
        var seen = new List<Guid>(); string? cursor = null;
        for (var page = 0; page < 3; page++)
        {
            using var response = await client.GetAsync(root + "?pageSize=1" + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("items", "nextCursor");
            var item = body.GetProperty("items").EnumerateArray().Should().ContainSingle().Subject;
            Check(item); seen.Add(item.GetProperty("id").GetGuid());
            cursor = body.GetProperty("nextCursor").GetString();
            if (page < 2) new Guid(Convert.FromBase64String(cursor!)).Should().Be(seen[^1]); else cursor.Should().BeNull();
        }
        seen.Should().OnlyHaveUniqueItems().And.BeEquivalentTo(ids);
        using (var empty = await foreign.GetAsync($"/api/v1/projects/{other.Id}/labels"))
        {
            empty.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await empty.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("items").EnumerateArray().Should().BeEmpty();
            body.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        }
        foreach (var path in new[] { root, root + "/" + ids[0] }) await Problem(foreign, path, 403, "access_forbidden");
        foreach (var role in Enum.GetValues<UserRoleCode>().Where(x => x is not (UserRoleCode.Unknown or UserRoleCode.ProjectManager)))
        {
            var actor = await _sql.CreateUserAsync("label-role-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            using var denied = await Login(actor.Email!);
            foreach (var path in new[] { root, root + "/" + ids[0] }) await Problem(denied, path, 403, "access_forbidden");
        }
        await Problem(foreign, $"/api/v1/projects/{other.Id}/labels/{ids[0]}", 404, "not_found");
        await Problem(client, root + "/" + Guid.NewGuid(), 404, "not_found");
        foreach (var query in new[] { "pageSize=0", "pageSize=201", "pageSize=invalid", "cursor=bad", "cursor=YWJj" })
            await Problem(client, root + "?" + query, 400, "validation_error");
        await using (var db = _sql.CreateDbContext())
            await db.ProjectMembers.Where(x => x.Id == membership).ExecuteUpdateAsync(x => x.SetProperty(v => v.ValidTo, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
        foreach (var path in new[] { root, root + "/" + ids[0] }) await Problem(client, path, 403, "access_forbidden");
        (await Snapshot()).Should().Be(before);

        void Check(JsonElement body)
        {
            body.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("id", "projectId", "revision", "status", "sourceKind", "sourceId", "sourceVersion", "fileId", "fileVersion", "annotation", "defectTypeCode", "reviewedBy", "reviewedAt", "reviewReason", "version");
            var id = body.GetProperty("id").GetGuid(); ids.Should().Contain(id);
            body.GetProperty("projectId").GetGuid().Should().Be(scope.ProjectId);
            body.GetProperty("revision").GetInt32().Should().Be(1);
            body.GetProperty("status").GetString().Should().Be(expected[id]);
            body.GetProperty("sourceKind").GetString().Should().Be("AI_DETECTION");
            body.GetProperty("sourceId").GetGuid().Should().Be(sources[id]);
            body.GetProperty("sourceVersion").GetString().Should().Be("owned-source-version");
            body.GetProperty("fileId").GetGuid().Should().Be(scope.RawFileId);
            body.GetProperty("fileVersion").GetString().Should().Be(scope.Checksum);
            body.GetProperty("defectTypeCode").GetString().Should().Be("POTHOLE");
            Convert.FromBase64String(body.GetProperty("version").GetString()!).Should().HaveCount(8);
            var annotation = body.GetProperty("annotation");
            annotation.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("kind", "coordinateSpace", "x", "y", "width", "height");
            annotation.GetProperty("kind").GetString().Should().Be("BBOX"); annotation.GetProperty("coordinateSpace").GetString().Should().Be("NORMALIZED");
            annotation.GetProperty("x").GetDecimal().Should().Be(0.1m); annotation.GetProperty("y").GetDecimal().Should().Be(0.2m);
            annotation.GetProperty("width").GetDecimal().Should().Be(0.3m); annotation.GetProperty("height").GetDecimal().Should().Be(0.4m);
            if (expected[id] == "PENDING")
            {
                foreach (var field in new[] { "reviewedBy", "reviewedAt", "reviewReason" }) body.GetProperty(field).ValueKind.Should().Be(JsonValueKind.Null);
            }
            else
            {
                body.GetProperty("reviewedBy").GetGuid().Should().Be(scope.ActorId);
                body.GetProperty("reviewedAt").GetDateTimeOffset().Should().BeBefore(DateTimeOffset.UtcNow);
                body.GetProperty("reviewReason").GetString().Should().Be("Owned fixture review");
            }
        }
        async Task<HttpClient> Login(string account)
        {
            if (cookie) return await WebCookieTestClient.Login(factory, account);
            var http = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            using var response = await http.PostAsJsonAsync("/api/v1/auth/login", new { email = account, password = "Current1!" });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            return http;
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
    private static async Task Problem(HttpClient client, string path, int status, string code)
    {
        using var response = await client.GetAsync(path);
        ((int)response.StatusCode).Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(status); body.GetProperty("code").GetString().Should().Be(code);
        var correlation = response.Headers.GetValues("X-Correlation-ID").Single(); Guid.TryParse(correlation, out _).Should().BeTrue();
        body.GetProperty("correlationId").GetString().Should().Be(correlation);
        response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
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
