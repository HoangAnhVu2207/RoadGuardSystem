using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H4RepairWorkflowHttpAdmissionTests(AuthenticationSqlServerFixture fixture)
{
    private static string Path() => $"/api/v1/projects/{Guid.NewGuid()}/repair-packages/{Guid.NewGuid()}/items/{Guid.NewGuid()}/corrections";
    private static RepairCorrectionInput Input() => new(Guid.NewGuid(), "UNREPAIRED", "physical result needs correction", new("recorded source basis", []));

    [Fact]
    public async Task AnonymousCorrectionCannotEnterProtectedProducer()
    {
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Path(), Input())).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.PreconditionRequired)]
    [InlineData("\"AQIDBA==\"", HttpStatusCode.BadRequest)]
    public async Task BearerHttpRequiresExactEightByteVersionBeforeReceipt(string? version, HttpStatusCode expected)
    {
        var actor = await fixture.CreateUserAsync($"repair-http-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "http-correction-1");
        if (version is not null) client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", version);
        var response = await client.PostAsJsonAsync(Path(), Input());
        Assert.Equal(expected, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected == HttpStatusCode.PreconditionRequired ? "precondition_required" : "validation_error", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("correlationId").GetString()));
        await using var db = fixture.CreateDbContext(); Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }

    [Fact]
    public async Task CookieCorrectionRequiresCsrfBeforeProducerAndReceipt()
    {
        var actor = await fixture.CreateUserAsync($"repair-cookie-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "http-correction-cookie");
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"AQIDBAUGBwg=\"");
        var response = await client.PostAsJsonAsync(Path(), Input()); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext(); Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }

    [Fact]
    public async Task ActualHttpCorrectionChangesCurrentViewAndRequestOnlyAppendsHistory()
    {
        var actor = await fixture.CreateUserAsync($"repair-producer-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var original = await fixture.CreateUserAsync($"repair-original-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var facts = await SeedAccepted(actor.Id, original.Id);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var itemPath = $"/api/v1/projects/{facts.Project}/repair-packages/{facts.Package}/items/{facts.Item}";
        var before = await client.GetAsync(itemPath); Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        var priorTag = before.Headers.ETag!.ToString();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "actual-http-correction");
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", priorTag);
        var input = Input() with { SupersedesDecisionId = facts.Decision };
        var response = await client.PostAsJsonAsync(itemPath + "/corrections", input);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); Assert.Equal(itemPath, response.Headers.Location!.OriginalString);
        var corrected = await response.Content.ReadFromJsonAsync<RepairCorrectionView>(); Assert.NotNull(corrected);
        Assert.False(corrected.ObligationResolved); Assert.NotEqual(facts.Decision, corrected.Id);
        var after = await client.GetAsync(itemPath); Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        var view = await after.Content.ReadFromJsonAsync<RepairItemView>(); Assert.NotNull(view);
        Assert.Equal(corrected.Id, view.EffectiveDecisionId); Assert.Equal("UNREPAIRED", view.Result); Assert.Equal("CorrectionRequired", view.State);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(itemPath + "/corrections", input)).StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "actual-http-review-request");
        client.DefaultRequestHeaders.Remove("If-Match"); client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", after.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(itemPath + "/review-requests", new RepairReviewRequestInput("Request an independent reconsideration"))).StatusCode);
        var historyResponse = await client.GetAsync(itemPath + "/history"); Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, history.GetProperty("decisions").GetArrayLength()); Assert.Equal(1, history.GetProperty("reviewRequests").GetArrayLength());
        Assert.Equal(corrected.Id, history.GetProperty("item").GetProperty("effectiveDecisionId").GetGuid());
        var originalDecision = history.GetProperty("decisions").EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == facts.Decision);
        Assert.Equal(JsonValueKind.String, originalDecision.GetProperty("result").ValueKind);
        Assert.Equal("CONFIRMED", originalDecision.GetProperty("result").GetString());
        Assert.Equal("FastTrack", originalDecision.GetProperty("mode").GetString());
        Assert.Equal("ProjectManager", originalDecision.GetProperty("role").GetString());
        Assert.False(originalDecision.TryGetProperty("accepted", out _));
        Assert.Equal("ProjectManager", history.GetProperty("reviewRequests")[0].GetProperty("role").GetString());
        await using (var db = fixture.CreateDbContext())
        {
            var member = await db.ProjectMembers.SingleAsync(row => row.Id == facts.Membership); member.Status = ProjectMemberStatus.Ended;
            await db.SaveChangesAsync();
            Assert.Single(await db.Set<RepairReviewRequest>().Where(row => row.ItemId == facts.Item).ToListAsync());
            Assert.Equal(RepairPresentationState.Confirmed, (await db.Set<RepairDecision>().SingleAsync(row => row.Id == facts.Decision)).Result);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(itemPath)).StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "actual-http-correction");
        client.DefaultRequestHeaders.Remove("If-Match"); client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", priorTag);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(itemPath + "/corrections", input)).StatusCode);
    }

    [Fact]
    public async Task RequestChangesHistoryRepresentationEtagWithoutChangingCommandItemEtag()
    {
        var actor = await fixture.CreateUserAsync($"repair-history-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var original = await fixture.CreateUserAsync($"repair-history-original-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var facts = await SeedAccepted(actor.Id, original.Id);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var path = $"/api/v1/projects/{facts.Project}/repair-packages/{facts.Package}/items/{facts.Item}";
        var itemBefore = await client.GetAsync(path); var historyBefore = await client.GetAsync(path + "/history");
        Assert.Equal(HttpStatusCode.OK, itemBefore.StatusCode); Assert.Equal(HttpStatusCode.OK, historyBefore.StatusCode);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "history-request-1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", itemBefore.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(path + "/review-requests", new RepairReviewRequestInput("New immutable request"))).StatusCode);
        var historyAfter = await client.GetAsync(path + "/history"); var itemAfter = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, historyAfter.StatusCode); Assert.Equal(HttpStatusCode.OK, itemAfter.StatusCode);
        Assert.NotEqual(historyBefore.Headers.ETag!.ToString(), historyAfter.Headers.ETag!.ToString());
        Assert.Equal(itemBefore.Headers.ETag!.ToString(), itemAfter.Headers.ETag!.ToString());
        Assert.StartsWith("\"h4-history-v1-", historyAfter.Headers.ETag!.ToString(), StringComparison.Ordinal);
        var repeated = await client.GetAsync(path + "/history"); Assert.Equal(historyAfter.Headers.ETag, repeated.Headers.ETag);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "wrong-representation-token");
        client.DefaultRequestHeaders.Remove("If-Match"); client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", historyAfter.Headers.ETag.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path + "/corrections", Input() with { SupersedesDecisionId = facts.Decision })).StatusCode);
    }

    private sealed record AcceptedSource(Guid Project, Guid Package, Guid Item, Guid Decision, Guid Membership);
    private async Task<AcceptedSource> SeedAccepted(Guid actor, Guid originalActor)
    {
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow.AddMinutes(-5);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Controlled historical repair for actual HTTP producer", null, null, null, null, now);
        var member = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1));
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "R"); var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "Controlled identity fixture; no survey accuracy assertion");
        var type = DefectType.Create("H4" + Guid.NewGuid().ToString("N"), "HTTP repair fixture");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low,
            DefectStatus.Open, geometry.CreatePoint(new Coordinate(5, 0)), now);
        db.AddRange(project, member, road, route, type, defect); await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "actual-source-frame-v1", "R", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.FastTrack, actor, UserRoleCode.ProjectManager,
            now, new("Historical plan fixture", "checklist-v1")); package.AddItem(item); db.Add(package); await db.SaveChangesAsync();
        db.ChangeTracker.Clear(); var decision = Guid.NewGuid();
        // Historical accepted facts isolate the real correction endpoint; this fixture is not an FT eligibility grant.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairDecisions (Id,ItemId,ObligationId,DefectId,Mode,ActorId,Role,Reason,At,Result) VALUES ({decision},{item.Id},{obligation.Id},{defect.Id},2,{originalActor},2,'controlled historical acceptance',{now},3)");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET EffectiveDecisionId={decision},State=7 WHERE Id={item.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET EffectiveResolutionHeadDecisionId={decision},EffectiveResolutionDecisionId={decision} WHERE Id={obligation.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairObligationResolutionEvents (DecisionId,Accepted,At,ObligationId) VALUES ({decision},1,{now},{obligation.Id})");
        return new(project.Id, package.Id, item.Id, decision, member.Id);
    }
}
