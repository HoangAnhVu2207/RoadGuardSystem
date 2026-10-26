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



// Populated read-only repository fixture, not a label eligibility/mutation journey.
[Collection(AuthenticationApiFixture.Name)]
public sealed class W04LabelReadCatalogAuthorityAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private readonly AuthenticationSqlServerFixture _sql = sql;
    [Theory]
    [InlineData("role-inactive", false)]
    [InlineData("role-inactive", true)]
    public async Task PopulatedLabelReadsRejectDeactivatedRoleCatalog(string change, bool cookie)
    {
        var scope = await SeedScopeAsync(); var job = await CreateJobAsync(scope);
        var id = Guid.NewGuid(); var detection = Guid.NewGuid(); string email;
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
            await new TrainingLabelRepository(db).CreateAsync(scope.ActorId,
                TrainingLabel.Create(id, scope.ProjectId, detection, "AI_DETECTION", "read-authority-fixture", scope.RawFileId,
                    scope.Checksum, 0.1m, 0.2m, 0.3m, 0.4m, "POTHOLE", "read fixture"), "read fixture", null, default);
        }
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var host = new AuthenticationWebApplicationFactory(_sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
                services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock);
            });
        using var client = cookie ? await WebCookieTestClient.Login(host, email)
            : host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        if (!cookie)
        {
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var paths = new[] { $"/api/v1/projects/{scope.ProjectId}/labels", $"/api/v1/projects/{scope.ProjectId}/labels/{id}" };
        foreach (var path in paths)
        {
            using var admitted = await client.GetAsync(path);
            admitted.StatusCode.Should().Be(HttpStatusCode.OK);
            (await admitted.Content.ReadAsStringAsync()).Should().Contain(id.ToString());
        }
        var before = await Snapshot();
        try
        {
            await using (var db = _sql.CreateDbContext())
                await db.Roles.Where(x => x.Code == UserRoleCode.ProjectManager).ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, false));
            if (change == "ticket-expired") clock.Now += TimeSpan.FromHours(13);
            foreach (var path in paths)
            {
                using var denied = await client.GetAsync(path);
                denied.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                denied.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
                var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
                problem.GetProperty("status").GetInt32().Should().Be(401);
                problem.GetProperty("code").GetString().Should().Be("auth_unauthorized");
                problem.GetProperty("instance").GetString().Should().Be(path);
                problem.TryGetProperty("items", out _).Should().BeFalse(); problem.TryGetProperty("annotation", out _).Should().BeFalse();
                problem.GetRawText().Should().NotContain(detection.ToString()).And.NotContain(scope.RawFileId.ToString()).And.NotContain(email);
                var correlation = denied.Headers.GetValues("X-Correlation-ID").Single();
                Guid.TryParse(correlation, out _).Should().BeTrue();
                problem.GetProperty("correlationId").GetString().Should().Be(correlation);
                denied.Headers.ETag.Should().BeNull(); denied.Headers.Location.Should().BeNull();
            }
            (await Snapshot()).Should().Be(before);
        }
        finally
        {
            await using var db = _sql.CreateDbContext();
            await db.Roles.Where(x => x.Code == UserRoleCode.ProjectManager).ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, true));
        }
        async Task<string> Snapshot()
        {
            await using var db = _sql.CreateDbContext();
            var heads = await db.Set<HuyTrainingLabelHead>().AsNoTracking().Where(x => x.Id == id).ToArrayAsync();
            var revisions = await db.Set<HuyTrainingLabelRevision>().AsNoTracking().Where(x => x.LabelId == id).ToArrayAsync();
            var reviews = await db.Set<HuyTrainingLabelReview>().AsNoTracking().Where(x => x.LabelId == id).ToArrayAsync();
            var audits = await db.AuditLogs.AsNoTracking().Where(x => x.EntityId == id).OrderBy(x => x.Id).ToArrayAsync();
            var receipts = await db.IdempotencyRecords.AsNoTracking().Where(x => x.ProjectId == scope.ProjectId).OrderBy(x => x.Id).ToArrayAsync();
            return JsonSerializer.Serialize(new { heads, revisions, reviews, audits, receipts });
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
