using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Implementations.Cases;
using RoadGuardSystem.Services.Integration;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

// Isolated SQL and genuine verified lineage. Source preparation uses services;
// the three publication operations below traverse actual HTTP middleware.
public sealed class PublicPublicationWireHttpTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApprovedPublicationWireMatchesPersistedSnapshotAndWithdrawal(bool web)
    {
        var source = await Prepare();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        await using var db = sql.CreateDbContext();
        var email = await db.Users.Where(x => x.Id == source.Pm).Select(x => x.Email).SingleAsync();
        using var pm = web ? await WebCookieTestClient.Login(factory, email!) : factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (!web)
        {
            var login = await pm.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            pm.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var createRoute = $"/api/v1/projects/{source.Project}/defects/{source.Defect}/publications";
        var key = Guid.NewGuid().ToString();
        var created = await Post(pm, createRoute, key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        AssertJsonNoStore(created);
        var snapshot = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = snapshot.GetProperty("publicId").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/api/v1/public/publications/{id}", created.Headers.Location!.ToString());
        var persisted = await db.Set<PublicPublication>().AsNoTracking().SingleAsync(x => x.Id == id);
        AssertDto(snapshot, id, persisted.PublishedAt, "PUBLISHED");
        var auditBeforeRead = await db.AuditLogs.CountAsync();
        var receiptsBeforeRead = await db.IdempotencyRecords.CountAsync();

        var read = await anonymous.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        AssertJsonNoStore(read);
        AssertDto(await read.Content.ReadFromJsonAsync<JsonElement>(), id, persisted.PublishedAt, "PUBLISHED");
        Assert.False(read.Headers.Contains("Set-Cookie"));
        var repeatedRead = await anonymous.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, repeatedRead.StatusCode);
        AssertJsonNoStore(repeatedRead);
        Assert.Equal(await read.Content.ReadAsStringAsync(), await repeatedRead.Content.ReadAsStringAsync());
        Assert.Equal(auditBeforeRead, await db.AuditLogs.CountAsync());
        Assert.Equal(receiptsBeforeRead, await db.IdempotencyRecords.CountAsync());
        Assert.Single(await db.Set<PublicPublication>().AsNoTracking().Where(x => x.DefectId == source.Defect).ToArrayAsync());
        var replay = await Post(pm, createRoute, key);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        AssertJsonNoStore(replay);
        Assert.Equal(snapshot.GetRawText(), (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());

        var withdrawn = await Post(pm, $"/api/v1/projects/{source.Project}/publications/{id}/withdraw", "withdraw-" + key);
        Assert.Equal(HttpStatusCode.OK, withdrawn.StatusCode);
        AssertJsonNoStore(withdrawn);
        AssertDto(await withdrawn.Content.ReadFromJsonAsync<JsonElement>(), id, persisted.PublishedAt, "WITHDRAWN");
        var gone = await anonymous.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.True(gone.Headers.CacheControl!.NoStore);
        await using var check = sql.CreateDbContext();
        var final = await check.Set<PublicPublication>().AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.NotNull(final.WithdrawnAt);
        Assert.Equal(persisted.PublishedAt, final.PublishedAt);
        Assert.Equal(2, await check.AuditLogs.CountAsync(x => x.EntityId == id));
    }

    private static void AssertJsonNoStore(HttpResponseMessage response)
    {
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    private static void AssertDto(JsonElement value, Guid id, DateTimeOffset at, string status)
    {
        Assert.Equal(new[] { "damageCategory", "publicId", "publicStatus", "publishedAt" }, value.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal(id, value.GetProperty("publicId").GetGuid());
        Assert.Equal("ROAD_SURFACE_DAMAGE", value.GetProperty("damageCategory").GetString());
        Assert.Equal(status, value.GetProperty("publicStatus").GetString());
        Assert.Equal(at, value.GetProperty("publishedAt").GetDateTimeOffset());
    }

    private async Task<GenuineProtectedSource.Source> Prepare()
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        if (!await db.DefectTypes.AnyAsync(x => x.Code == "CRACK")) db.DefectTypes.Add(DefectType.Create("CRACK", "Approved category test"));
        await db.SaveChangesAsync();
        await db.DefectTypes.Where(x => x.Code == "CRACK").ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, true));
        var incident = await db.IncidentCases.SingleAsync(x => x.ProjectId == source.Project);
        var report = await db.Reports.Include(x => x.OriginalEvidence).SingleAsync(x => x.Id == incident.ActiveReportIds.Single());
        var evidence = report.OriginalEvidence.Single();
        var defect = await db.Defects.SingleAsync(x => x.Id == source.Defect);
        db.DefectVerificationLogs.Add(defect.Assess("CRACK", null, DefectSeverity.Low, source.Pm, "Approved assessment", [evidence.Id]));
        db.DefectVerificationLogs.Add(defect.DecideFromExistingEvidence(DefectVerificationAction.Confirm, [evidence.Id], [evidence.Id], source.Pm, "Verified retained evidence"));
        await db.SaveChangesAsync();
        var cases = new CaseWorkflowService(new CaseWorkflowRepository(db), new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System),
            new AnhHuyProducerService(new AnhHuyFactsRepository(db), null!, null!), new IdempotencyOperationService(db));
        var current = await cases.ReadAsync(source.Pm, UserRoleCode.ProjectManager, incident.Id, default);
        var concluded = await cases.CommandAsync(source.Pm, UserRoleCode.ProjectManager,
            new(incident.Id, current.Case!.Version, "conclude", "Verified publication source", Outcome: CaseConclusionOutcome.Confirmed, DefectIds: [defect.Id], EvidenceIds: [evidence.Id]),
            Guid.NewGuid().ToString(), null, default);
        Assert.Equal(200, concluded.Status);
        return source;
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string route, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
}
