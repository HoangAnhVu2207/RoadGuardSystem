using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LD07RoadCoverageHttpTests(AuthenticationSqlServerFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task ProductionDiRequiresSupervisorExactSourceScopeVersionAndCurrentAuthorityBeforeReplay()
    {
        var pm = await fixture.CreateUserAsync("ld07-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await fixture.CreateUserAsync("ld07-sup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        // Controlled TEST_ONLY geometry/obligation isolates the actual production mapping endpoint.
        // The SQL acceptance separately produces the real Report -> repair binding -> eligibility chain.
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "TEST_ONLY mapping HTTP", null, null, null, null, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2)
        };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "TEST_ONLY road");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "TEST_ONLY; no real CRS claim");
        var type = DefectType.Create("LD07" + Guid.NewGuid().ToString("N"), "TEST_ONLY");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low, DefectStatus.Open,
            geometry.CreatePoint(new Coordinate(5, 0)), now);
        db.AddRange(project, member, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2)), road, route, type, defect);
        await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "TEST_ONLY_FRAME_V1", "TEST_ONLY", 1, 2, 0, 1));
        db.Add(RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation])); await db.SaveChangesAsync();
        var file = StoredFile.Create(Guid.NewGuid(), "TEST_ONLY/ld07-http/" + Guid.NewGuid(), "basis.pdf", "application/pdf", 4, new string('f', 64), supervisor.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, supervisor.Id, file.StorageUri, "TEST_ONLY_COVERAGE", file.MimeType, 4, file.Checksum, 8388608, now.AddHours(24));
        upload.StartUploading("TEST_ONLY", now); db.AddRange(file, upload, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, project.Id, supervisor.Id, "TEST_ONLY_COVERAGE", now));
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var handover = HandoverDocument.Create(Guid.NewGuid(), project.Id, "TEST_ONLY-" + Guid.NewGuid(), DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2), supervisor.Id, file.Id, "TEST_ONLY");
        db.Add(handover); await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(); using var pmClient = factory.CreateClient();
        await Login(client, supervisor.Email!); await Login(pmClient, pm.Email!);
        var path = $"/api/v1/projects/{project.Id}/road-coverage";
        var initial = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var view = (await initial.Content.ReadFromJsonAsync<RoadCoverageReadFact>())!;
        var handoverPin = view.Sources.Single(row => row.Kind == "HANDOVER" && row.Id == handover.Id);
        var sourcePin = view.Sources.Single(row => row.Kind == "MAINTENANCE_BASIS" && row.Id == file.Id);
        var input = new RoadCoverageInputDto(obligation.Id, Assert.Single(view.Scopes).ScopeHash, handover.Id, handoverPin.Version,
            sourcePin.Kind, file.Id, sourcePin.Version, now.AddMinutes(-1), now.AddDays(1), "TEST_ONLY", "TEST_ONLY exact source-linked HTTP confirmation");
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await client.PostAsJsonAsync(path, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(pmClient, path, input, Guid.NewGuid().ToString(), initial.Headers.ETag!.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/road-coverage")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input with { HandoverVersion = new string('0', 64) }, Guid.NewGuid().ToString(), initial.Headers.ETag!.ToString())).StatusCode);
        var key = Guid.NewGuid().ToString();
        var confirmed = await Send(client, path, input, key, initial.Headers.ETag!.ToString()); Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        var mapping = Assert.Single((await confirmed.Content.ReadFromJsonAsync<RoadCoverageReadFact>())!.History);
        Assert.Equal(sourcePin.Version, mapping.SourceVersion); Assert.Equal(handoverPin.Version, mapping.HandoverVersion); Assert.Equal("TEST_ONLY", mapping.Provenance);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, path, input, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input with { Reason = "changed" }, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input, Guid.NewGuid().ToString(), initial.Headers.ETag.ToString())).StatusCode);
        await db.ProjectMembers.Where(row => row.Id == member.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, path, input, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Single(await db.Set<RoadCoverageMapping>().Where(row => row.ProjectId == project.Id).ToArrayAsync());
        output.WriteLine(JsonSerializer.Serialize(new
        {
            proof = "SWG-194",
            environment = "TEST_HOST_E2E",
            status = 201,
            actorId = supervisor.Id,
            role = "Supervisor",
            projectId = project.Id,
            obligationId = obligation.Id,
            handoverId = handover.Id,
            sourceFileId = file.Id,
            mappingId = mapping.Id,
            producer = "TEST_ONLY isolated SQL handover, verified upload and file scope prerequisite",
            request = input,
            persistedMappings = 1,
            provenance = "TEST_ONLY"
        }));
    }

    [Fact]
    public async Task CookieConfirmationRequiresCsrfBeforeAnyMappingOrReceipt()
    {
        var actor = await fixture.CreateUserAsync("ld07-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/road-coverage", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext(); Assert.False(await db.Set<RoadCoverageMapping>().AnyAsync(row => row.ActorId == actor.Id));
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }
    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, string path, object input, string key, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key); request.Headers.TryAddWithoutValidation("If-Match", version); return client.SendAsync(request);
    }
}
