using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Implementations.Cases;
using RoadGuardSystem.Services.Integration;
using Xunit;
namespace RoadGuardSystem.ApiTests.Cases;

// Controlled genuine Reporter/Case/Defect lineage fixture. This is SQL+HTTP contract acceptance,
// not a claim that all FE workflow transitions were executed over HTTP.
public sealed class PublicPublicationSqlTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    private const string Password = "Current1!";
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicationEnvelopeRejectsMissingAndOversizedKeysWithoutDurableEffects(bool web)
    {
        var s = await Seed("CRACK");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await Client(factory, s.Source.Pm, web);
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var route = CreateRoute(s);
        using (var request = new HttpRequestMessage(HttpMethod.Post, route))
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.SendAsync(request)).StatusCode);
        using (var request = new HttpRequestMessage(HttpMethod.Post, route))
            Assert.Equal(428, (int)(await pm.SendAsync(request)).StatusCode);
        var invalid = await Post(pm, route, new string('x', 201));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("validation_error", (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using (var db = sql.CreateDbContext())
        {
            Assert.False(await db.Set<PublicPublication>().AnyAsync(x => x.DefectId == s.Source.Defect));
            Assert.False(await db.IdempotencyRecords.AnyAsync(x => x.ProjectId == s.Source.Project && x.Operation.StartsWith("c1.")));
            Assert.False(await db.AuditLogs.AnyAsync(x => x.ActorUserId == s.Source.Pm && x.EventType.StartsWith("public_publication")));
        }
        var created = await Post(pm, route, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = dto.GetProperty("publicId").GetGuid();
        Assert.Equal($"/api/v1/public/publications/{id}", created.Headers.Location!.ToString());
        await using (var db = sql.CreateDbContext())
            Assert.Equal((await db.Set<PublicPublication>().SingleAsync(x => x.Id == id)).PublishedAt,
                dto.GetProperty("publishedAt").GetDateTimeOffset());
        var withdraw = $"/api/v1/projects/{s.Source.Project}/publications/{id}/withdraw";
        using (var request = new HttpRequestMessage(HttpMethod.Post, withdraw))
            Assert.Equal(428, (int)(await pm.SendAsync(request)).StatusCode);
        var rejected = await Post(pm, withdraw, new string('x', 201));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("validation_error", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using (var db = sql.CreateDbContext())
        {
            Assert.Null((await db.Set<PublicPublication>().SingleAsync(x => x.Id == id)).WithdrawnAt);
            Assert.Single(await db.AuditLogs.Where(x => x.EntityId == id).ToArrayAsync());
            Assert.Single(await db.IdempotencyRecords.Where(x => x.ProjectId == s.Source.Project && x.Operation.StartsWith("c1.")).ToArrayAsync());
        }
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/v1/public/publications/{id}")).StatusCode);
    }
    [Theory]
    [InlineData("CRACK", false)]
    [InlineData("POTHOLE", false)]
    [InlineData("CRACK", true)]
    [InlineData("POTHOLE", true)]
    public async Task AuthorizedSnapshotHasExactlyFourFieldsAndWithdrawalIsDurable(string code, bool web)
    {
        var s = await Seed(code); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await Client(factory, s.Source.Pm, web); using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var key = Guid.NewGuid().ToString(); var create = await Post(pm, CreateRoute(s), key);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var value = await create.Content.ReadFromJsonAsync<JsonElement>(); var id = value.GetProperty("publicId").GetGuid();
        Assert.Equal("ROAD_SURFACE_DAMAGE", value.GetProperty("damageCategory").GetString());
        var read = await anonymous.GetAsync($"/api/v1/public/publications/{id}"); Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.True(read.Headers.CacheControl!.NoStore);
        var body = await read.Content.ReadAsStringAsync(); var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal(new[] { "damageCategory", "publicId", "publicStatus", "publishedAt" }, json.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal("PUBLISHED", json.GetProperty("publicStatus").GetString());
        Assert.DoesNotContain(s.Source.Defect.ToString(), body); Assert.DoesNotContain(s.Source.Project.ToString(), body);
        var replay = await Post(pm, CreateRoute(s), key); Assert.Equal(value.GetRawText(), (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        var withdraw = $"/api/v1/projects/{s.Source.Project}/publications/{id}/withdraw";
        Assert.Equal(HttpStatusCode.OK, (await Post(pm, withdraw, "withdraw-" + key)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(pm, withdraw, "withdraw-" + key)).StatusCode);
        var gone = await anonymous.GetAsync($"/api/v1/public/publications/{id}"); var missing = await anonymous.GetAsync($"/api/v1/public/publications/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode); Assert.Equal(missing.StatusCode, gone.StatusCode); Assert.True(gone.Headers.CacheControl!.NoStore);
        var missingProblem = await missing.Content.ReadFromJsonAsync<JsonElement>(); var goneProblem = await gone.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(missingProblem.GetProperty("title").GetString(), goneProblem.GetProperty("title").GetString());
        Assert.Equal(missingProblem.GetProperty("status").GetInt32(), goneProblem.GetProperty("status").GetInt32());
        await using var check = sql.CreateDbContext();
        Assert.Single(await check.Set<PublicPublication>().Where(x => x.DefectId == s.Source.Defect).ToArrayAsync());
        Assert.Equal(2, await check.AuditLogs.CountAsync(x => x.EntityId == id));
        Assert.Equal(1, await check.Set<PublicPublication>().CountAsync(x => x.Id == id && x.WithdrawnAt != null));
        Assert.True(await check.Files.AnyAsync(x => x.Id == s.File));
    }
    [Theory]
    [InlineData("fixture", 409)]
    [InlineData("other", 409)]
    [InlineData("inactive-catalog", 409)]
    [InlineData("open-defect", 409)]
    [InlineData("no-provenance", 409)]
    [InlineData("open-case", 409)]
    [InlineData("unconfirmed-case", 409)]
    [InlineData("pending-evidence", 409)]
    [InlineData("stale-evidence", 412)]
    [InlineData("unlinked-source", 409)]
    [InlineData("revoked", 403)]
    [InlineData("wrong-role", 403)]
    [InlineData("wrong-project", 404)]
    public async Task IneligibleOrUnauthorizedSourcesDenyWithoutPublicationReceiptOrAudit(string mutation, int expected)
    {
        var s = await Seed("CRACK", mutation); await using (var db = sql.CreateDbContext())
        {
            if (mutation is "fixture" or "other")
            {
                var code = mutation == "fixture" ? "RG_POSTMAN_POTHOLE" : "UNAPPROVED_" + Guid.NewGuid().ToString("N");
                if (!await db.DefectTypes.AnyAsync(x => x.Code == code)) { db.DefectTypes.Add(DefectType.Create(code, "Denied fixture")); await db.SaveChangesAsync(); }
                await db.Defects.Where(x => x.Id == s.Source.Defect).ExecuteUpdateAsync(x => x.SetProperty(v => v.DefectTypeCode, code));
            }
            if (mutation == "inactive-catalog") await db.DefectTypes.Where(x => x.Code == "CRACK").ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, false));
            if (mutation == "open-defect") await db.Defects.Where(x => x.Id == s.Source.Defect).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, DefectStatus.Open));
            if (mutation == "open-case") await db.IncidentCases.Where(x => x.Id == s.Case).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, IncidentCaseStatus.Open));
            if (mutation == "pending-evidence") await db.UploadSessions.Where(x => x.FileId == s.File).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, UploadSessionStatus.Pending));
            if (mutation == "stale-evidence") await db.UploadSessions.Where(x => x.FileId == s.File).ExecuteUpdateAsync(x => x.SetProperty(v => v.StorageUploadId, Guid.NewGuid().ToString()));
            if (mutation == "unlinked-source") await db.Set<HuyDefectSourceLink>().Where(x => x.DefectId == s.Source.Defect).ExecuteUpdateAsync(x => x.SetProperty(v => v.EndedAt, DateTimeOffset.UtcNow));
            if (mutation == "revoked") await db.ProjectMembers.Where(x => x.ProjectId == s.Source.Project && x.UserId == s.Source.Pm).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var actor = mutation == "wrong-role" ? s.Source.Crew : s.Source.Pm;
        using var client = await Client(factory, actor);
        var route = mutation == "wrong-project" ? $"/api/v1/projects/{Guid.NewGuid()}/defects/{s.Source.Defect}/publications" : CreateRoute(s);
        var response = await Post(client, route, Guid.NewGuid().ToString());
        Assert.Equal(expected, (int)response.StatusCode);
        await using var check = sql.CreateDbContext(); Assert.False(await check.Set<PublicPublication>().AnyAsync(x => x.DefectId == s.Source.Defect));
        Assert.False(await check.IdempotencyRecords.AnyAsync(x => x.ProjectId == s.Source.Project && x.Operation.StartsWith("c1.")));
        Assert.False(await check.AuditLogs.AnyAsync(x => x.ActorUserId == s.Source.Pm && x.EventType.StartsWith("public_publication")));
    }
    [Fact]
    public async Task ConcurrentCreateAndWithdrawReplayHaveOneEffectAndRevocationDeniesReceipt()
    {
        var s = await Seed("CRACK"); var key = Guid.NewGuid().ToString();
        async Task<RoadGuardSystem.Services.Cases.PublicPublicationResult> Create()
        { await using var db = sql.CreateDbContext(); return await Service(db).CreateAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, s.Source.Defect, key, default); }
        var results = await Task.WhenAll(Create(), Create()); Assert.All(results, x => Assert.Equal(201, x.Status));
        Assert.Equal(results[0].Publication, results[1].Publication); var id = results[0].Publication!.PublicId;
        async Task<RoadGuardSystem.Services.Cases.PublicPublicationResult> Withdraw()
        { await using var db = sql.CreateDbContext(); return await Service(db).WithdrawAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, id, "withdraw-" + key, default); }
        Assert.All(await Task.WhenAll(Withdraw(), Withdraw()), x => Assert.Equal(200, x.Status));
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(x => x.ProjectId == s.Source.Project).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        Assert.Equal(403, (await Create()).Status); Assert.Equal(403, (await Withdraw()).Status);
        await using var check = sql.CreateDbContext(); Assert.Equal(1, await check.Set<PublicPublication>().CountAsync(x => x.DefectId == s.Source.Defect));
        Assert.Equal(2, await check.AuditLogs.CountAsync(x => x.EntityId == id));
    }
    [Fact]
    public async Task FailureAfterSnapshotAndAuditSaveRollsBackReceiptAndAllEffects()
    {
        var s = await Seed("CRACK"); var key = Guid.NewGuid().ToString();
        await using (var db = sql.CreateDbContext())
        {
            var service = new PublicPublicationService(new FaultingRepository(new PublicPublicationRepository(db, TimeProvider.System)),
                new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System), new IdempotencyOperationService(db));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, s.Source.Defect, key, default));
        }
        await using (var db = sql.CreateDbContext())
        {
            Assert.False(await db.Set<PublicPublication>().AnyAsync(x => x.DefectId == s.Source.Defect));
            Assert.False(await db.AuditLogs.AnyAsync(x => x.ActorUserId == s.Source.Pm && x.EventType == "public_publication_created"));
            Assert.False(await db.IdempotencyRecords.AnyAsync(x => x.ProjectId == s.Source.Project && x.Operation.StartsWith("c1.")));
            Assert.Equal(201, (await Service(db).CreateAsync(s.Source.Pm, UserRoleCode.ProjectManager, s.Source.Project, s.Source.Defect, key, default)).Status);
        }
    }
    [Fact]
    public async Task PublicSnapshotRemainsImmutableAfterPrivateMutationAndWrongActorsCannotWithdraw()
    {
        var s = await Seed("CRACK");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await Client(factory, s.Source.Pm); using var anon = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var create = await Post(pm, CreateRoute(s), Guid.NewGuid().ToString()); Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var snapshot = await create.Content.ReadAsStringAsync(); var id = JsonDocument.Parse(snapshot).RootElement.GetProperty("publicId").GetGuid();
        var route = $"/api/v1/projects/{s.Source.Project}/publications/{id}/withdraw";
        using (var wrong = await Client(factory, s.Source.Crew)) Assert.Equal(HttpStatusCode.Forbidden, (await Post(wrong, route, Guid.NewGuid().ToString())).StatusCode);
        await using (var db = sql.CreateDbContext())
        {
            await db.Defects.Where(x => x.Id == s.Source.Defect).ExecuteUpdateAsync(x => x.SetProperty(v => v.DefectTypeCode, "POTHOLE"));
        }
        var read = await anon.GetAsync($"/api/v1/public/publications/{id}"); Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(snapshot, await read.Content.ReadAsStringAsync());
        var wrongProject = $"/api/v1/projects/{Guid.NewGuid()}/publications/{id}/withdraw";
        Assert.Equal(HttpStatusCode.NotFound, (await Post(pm, wrongProject, Guid.NewGuid().ToString())).StatusCode);
        await using (var db = sql.CreateDbContext()) Assert.Single(await db.AuditLogs.Where(x => x.EntityId == id).ToArrayAsync());
        Assert.Equal(HttpStatusCode.OK, (await Post(pm, route, Guid.NewGuid().ToString())).StatusCode);
    }
    [Fact]
    public async Task WebCookieWritesRequireCsrfCurrentProjectAndLiveSession()
    {
        var s = await Seed("CRACK"); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await Client(factory, s.Source.Pm, true); using var wrong = await Client(factory, s.Source.Crew, true);
        var route = CreateRoute(s);
        pm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var denied = await Post(pm, route, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("csrf_failed", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        pm.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid");
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(pm, route, Guid.NewGuid().ToString())).StatusCode);
        await using (var db = sql.CreateDbContext()) Assert.False(await db.Set<PublicPublication>().AnyAsync(x => x.DefectId == s.Source.Defect));
        await WebCookieTestClient.RefreshCsrf(pm);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(wrong, route, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(pm, $"/api/v1/projects/{Guid.NewGuid()}/defects/{s.Source.Defect}/publications", Guid.NewGuid().ToString())).StatusCode);
        var created = await Post(pm, route, Guid.NewGuid().ToString()); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("publicId").GetGuid();
        var withdraw = $"/api/v1/projects/{s.Source.Project}/publications/{id}/withdraw";
        pm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(pm, withdraw, Guid.NewGuid().ToString())).StatusCode);
        await WebCookieTestClient.RefreshCsrf(pm);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(wrong, withdraw, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(pm, $"/api/v1/projects/{Guid.NewGuid()}/publications/{id}/withdraw", Guid.NewGuid().ToString())).StatusCode);
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(x => x.ProjectId == s.Source.Project && x.UserId == s.Source.Pm).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(pm, route, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(pm, withdraw, Guid.NewGuid().ToString())).StatusCode);
        await using (var db = sql.CreateDbContext()) await db.Sessions.Where(x => x.UserId == s.Source.Pm).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(pm, route, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(pm, withdraw, Guid.NewGuid().ToString())).StatusCode);
        using var anon = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/api/v1/public/publications/{id}")).StatusCode);
        await using (var db = sql.CreateDbContext()) Assert.Null((await db.Set<PublicPublication>().SingleAsync(x => x.Id == id)).WithdrawnAt);
    }

    private sealed class FaultingRepository(IPublicPublicationRepository inner) : IPublicPublicationRepository
    {
        public Task GuardAsync(Guid actor, UserRoleCode role, Guid project, Guid target, bool withdraw, Func<Guid, CancellationToken, Task<bool>> access, CancellationToken ct)
            => inner.GuardAsync(actor, role, project, target, withdraw, access, ct);
        public async Task<PublicPublication> CreateAsync(Guid actor, Guid project, Guid defect, CancellationToken ct)
        { await inner.CreateAsync(actor, project, defect, ct); throw new InvalidOperationException("Controlled post-save rollback fault"); }
        public Task<PublicPublication> WithdrawAsync(Guid actor, Guid project, Guid id, CancellationToken ct) => inner.WithdrawAsync(actor, project, id, ct);
        public Task<PublicPublication?> ReadAsync(Guid id, CancellationToken ct) => inner.ReadAsync(id, ct);
    }
    private async Task<Setup> Seed(string code, string? mutation = null)
    {
        await using var db = sql.CreateDbContext(); var source = await GenuineProtectedSource.Seed(db, sql);
        if (!await db.DefectTypes.AnyAsync(x => x.Code == code)) { db.DefectTypes.Add(DefectType.Create(code, "Owner allowlisted test catalog")); await db.SaveChangesAsync(); }
        await db.DefectTypes.Where(x => x.Code == code).ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, true));
        var incident = await db.IncidentCases.SingleAsync(x => x.ProjectId == source.Project);
        var report = await db.Reports.Include(x => x.OriginalEvidence).SingleAsync(x => x.Id == incident.ActiveReportIds.Single());
        var evidence = report.OriginalEvidence.Single(); var defect = await db.Defects.SingleAsync(x => x.Id == source.Defect);
        db.DefectVerificationLogs.Add(defect.Assess(code, null, DefectSeverity.Low, source.Pm, "Allowlisted assessment", [evidence.Id]));
        if (mutation != "no-provenance") db.DefectVerificationLogs.Add(defect.DecideFromExistingEvidence(DefectVerificationAction.Confirm, [evidence.Id], [evidence.Id], source.Pm, "Confirmed retained source"));
        await db.SaveChangesAsync();
        if (mutation == "no-provenance") await db.Defects.Where(x => x.Id == defect.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, DefectStatus.Verified));
        var caseService = new CaseWorkflowService(new CaseWorkflowRepository(db), new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System),
            new AnhHuyProducerService(new AnhHuyFactsRepository(db), null!, null!), new IdempotencyOperationService(db));
        var read = await caseService.ReadAsync(source.Pm, UserRoleCode.ProjectManager, incident.Id, default);
        var concluded = await caseService.CommandAsync(source.Pm, UserRoleCode.ProjectManager, new(incident.Id, read.Case!.Version, "conclude", "Confirmed publication source",
            Outcome: mutation == "unconfirmed-case" ? CaseConclusionOutcome.NoDefect : CaseConclusionOutcome.Confirmed, DefectIds: mutation == "unconfirmed-case" ? [] : [defect.Id], EvidenceIds: [evidence.Id]), Guid.NewGuid().ToString(), null, default);
        Assert.Equal(200, concluded.Status); return new(source, incident.Id, evidence.FileId);
    }
    private static PublicPublicationService Service(RoadGuardDbContext db) => new(new PublicPublicationRepository(db, TimeProvider.System),
        new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System), new IdempotencyOperationService(db));
    private async Task<HttpClient> Client(AuthenticationWebApplicationFactory factory, Guid actor, bool web = false)
    {
        await using var db = sql.CreateDbContext(); var email = await db.Users.Where(x => x.Id == actor).Select(x => x.Email).SingleAsync();
        if (web) return await WebCookieTestClient.Login(factory, email!);
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password }); login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString()); return client;
    }
    private static string CreateRoute(Setup s) => $"/api/v1/projects/{s.Source.Project}/defects/{s.Source.Defect}/publications";
    private static Task<HttpResponseMessage> Post(HttpClient client, string route, string key)
    { var request = new HttpRequestMessage(HttpMethod.Post, route); request.Headers.Add("Idempotency-Key", key); return client.SendAsync(request); }
    private sealed record Setup(GenuineProtectedSource.Source Source, Guid Case, Guid File);
}
