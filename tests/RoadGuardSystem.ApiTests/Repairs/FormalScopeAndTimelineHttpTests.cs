using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

public sealed class FormalScopeAndTimelineHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("NORMAL", "NORMAL")]
    [InlineData("NORMAL", "FAST_TRACK")]
    [InlineData("FAST_TRACK", "NORMAL")]
    [InlineData("FAST_TRACK", "FAST_TRACK")]
    public async Task FormalReservationPrecedesModeAndMatchesCurrentReadWithConcurrentRequests(string existingMode, string requestedMode)
    {
        await using var db = sql.CreateDbContext(); var source = await GenuineProtectedSource.Seed(db, sql);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await WebCookieTestClient.Login(factory, await db.Users.Where(x => x.Id == source.Pm).Select(x => x.Email!).SingleAsync());
        using var crew = await WebCookieTestClient.Login(factory, await db.Users.Where(x => x.Id == source.Crew).Select(x => x.Email!).SingleAsync());
        var root = $"/api/v1/projects/{source.Project}/repair-packages";
        var version = Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var scope = new { physicalRoadId = source.Road, routeVersionId = source.Route, segmentSetId = source.Set, fromMeters = 1m, toMeters = 2m, offsetFromMeters = 0m, offsetToMeters = 1m };
        object Input(string reason) => new { defectId = source.Defect, defectVersion = version, obligations = new[] { new { kind = "FORMAL_REPAIR", mandatory = true, scope, reason } }, reason };
        string Query(decimal from = 1, decimal to = 2) => root + $"/eligibility?defectId={source.Defect}&scope.physicalRoadId={source.Road}&scope.routeVersionId={source.Route}&scope.segmentSetId={source.Set}&scope.fromMeters={from}&scope.toMeters={to}&scope.offsetFromMeters=0&scope.offsetToMeters=1";
        var missing = await Body(await pm.GetAsync(root + $"/eligibility?defectId={source.Defect}"));
        Assert.Equal(JsonValueKind.Null, missing.GetProperty("eligible").ValueKind);
        Assert.True((await Body(await pm.GetAsync(Query()))).GetProperty("eligible").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await crew.GetAsync(Query())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pm.GetAsync(Query().Replace(source.Project.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal))).StatusCode);
        var key = Guid.NewGuid().ToString(); var input = Input("Reserve before mode " + requestedMode);
        var attempts = await Task.WhenAll(Post(pm, root, input, version, key), Post(pm, root, input, version));
        var accepted = Assert.Single(attempts.Where(r => r.StatusCode == HttpStatusCode.Created));
        var denied = Assert.Single(attempts.Where(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal("active_scope_conflict", (await Body(denied, 409)).GetProperty("code").GetString());
        var package = await Body(accepted, 201); var packageId = package.GetProperty("id").GetGuid();
        var obligation = await db.RepairObligations.Where(x => x.DefectId == source.Defect).SingleAsync();
        var beforeItem = await Body(await pm.GetAsync(Query())); Assert.False(beforeItem.GetProperty("eligible").GetBoolean());
        Assert.Equal(JsonValueKind.Null, beforeItem.GetProperty("itemId").ValueKind);
        Assert.Equal(packageId, beforeItem.GetProperty("packageId").GetGuid());
        // The race winner can be either key. Replay its actual durable command.
        var receiptKey = accepted.RequestMessage!.Headers.GetValues("Idempotency-Key").Single();
        Assert.Equal(packageId, (await Body(await Post(pm, root, input, version, receiptKey), 200)).GetProperty("id").GetGuid());
        Assert.Equal("idempotency_key_reused", (await Body(await Post(pm, root, Input("different"), version, receiptKey), 409)).GetProperty("code").GetString());
        var proposal = await Post(pm, root + $"/{packageId}/items", new { obligationId = obligation.Id, mode = existingMode, repairPlan = "Source plan", checklistVersion = "v1", reason = "First item of own obligation" }, accepted.Headers.ETag!.Tag.Trim('"'));
        Assert.Equal(HttpStatusCode.Created, proposal.StatusCode);
        var blocked = await Body(await pm.GetAsync(Query())); Assert.Equal(existingMode, blocked.GetProperty("mode").GetString());
        if (Environment.GetEnvironmentVariable("RG_SCOPE_HANDOFF_EVIDENCE") is { } directory)
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, $"formal-{existingMode}-{requestedMode}.json"), JsonSerializer.Serialize(new { request = input, noScope = missing, beforeItem, blocked }));
        }
        Assert.True((await Body(await pm.GetAsync(Query(3, 4)))).GetProperty("eligible").GetBoolean());
        Assert.Equal(1, await db.RepairObligations.CountAsync(x => x.DefectId == source.Defect));
        var disjointScope = new { physicalRoadId = source.Road, routeVersionId = source.Route, segmentSetId = source.Set, fromMeters = 3m, toMeters = 4m, offsetFromMeters = 0m, offsetToMeters = 1m };
        object Batch(int count) => new { defectId = source.Defect, defectVersion = version, obligations = Enumerable.Range(0, count).Select(_ => new { kind = "FORMAL_REPAIR", mandatory = true, scope = disjointScope, reason = "independent disjoint scope" }).ToArray(), reason = "batch scope admission" };
        Assert.Equal("active_scope_conflict", (await Body(await Post(pm, root, Batch(2), version), 409)).GetProperty("code").GetString());
        Assert.Equal(1, await db.RepairObligations.CountAsync(x => x.DefectId == source.Defect));
        await Body(await Post(pm, root, Batch(1), version), 201);
        Assert.Equal(2, await db.RepairObligations.CountAsync(x => x.DefectId == source.Defect));
    }

    [Fact]
    public async Task ProjectTimelineUsesStoredActorsAndStableSourceTieWithoutPrivatePayloads()
    {
        await using var db = sql.CreateDbContext(); var source = await GenuineProtectedSource.Seed(db, sql);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        async Task<HttpClient> Web(Guid id) => await WebCookieTestClient.Login(factory, await db.Users.Where(x => x.Id == id).Select(x => x.Email!).SingleAsync());
        using var pm = await Web(source.Pm); using var sup = await Web(source.Supervisor);
        var version = Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var root = $"/api/v1/projects/{source.Project}/repair-packages";
        var scope = new { physicalRoadId = source.Road, routeVersionId = source.Route, segmentSetId = source.Set, fromMeters = 1m, toMeters = 2m, offsetFromMeters = 0m, offsetToMeters = 1m };
        var created = await Post(pm, root, new { defectId = source.Defect, defectVersion = version, obligations = new[] { new { kind = "FORMAL_REPAIR", mandatory = true, scope, reason = "scope" } }, reason = "package" }, version);
        var package = (await Body(created, 201)).GetProperty("id").GetGuid();
        var obligation = await db.RepairObligations.SingleAsync(x => x.DefectId == source.Defect);
        await Body(await Post(pm, root + $"/{package}/items", new { obligationId = obligation.Id, mode = "FAST_TRACK", repairPlan = "plan", checklistVersion = "v1", reason = "private reason is not a timeline summary" }, created.Headers.ETag!.Tag.Trim('"')), 201);
        var persistedEvent = await db.Set<RepairItemLifecycleEvent>().SingleAsync(x => x.ObligationId == obligation.Id);
        var request = SurveyRequest.Create(Guid.NewGuid(), source.Project, source.Road, null, source.Pm, SurveyType.Periodic, SurveyRequestStatus.NewAssigned, persistedEvent.At);
        db.Add(request); await db.SaveChangesAsync();
        db.Add(AuditLog.Create(persistedEvent.Id, null, persistedEvent.At, "survey_task_created", "SurveyRequest", request.Id, null, null, "private legacy reason", "historical-survey", null));
        var user = await db.Users.SingleAsync(x => x.Id == source.Pm); user.DisplayName = "Current PM display"; await db.SaveChangesAsync();
        var path = $"/api/v1/projects/{source.Project}/reports/timeline?aggregateType=Project&aggregateId={source.Project}&pageSize=1";
        var all = new List<JsonElement>(); string? cursor = null;
        do
        {
            var page = await Body(await pm.GetAsync(path + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor))));
            Assert.Equal("PARTIAL", page.GetProperty("availability").GetProperty("availability").GetString());
            all.AddRange(page.GetProperty("items").EnumerateArray().Select(x => x.Clone())); cursor = page.GetProperty("nextCursor").GetString();
        } while (cursor is not null);
        Assert.Equal(all.Count, all.Select(x => (x.GetProperty("source").GetProperty("type").GetString(), x.GetProperty("eventId").GetGuid())).Distinct().Count());
        var human = Assert.Single(all.Where(x => x.GetProperty("action").GetString() == "REPAIR.PROPOSED"));
        Assert.Equal(source.Pm, human.GetProperty("actorId").GetGuid()); Assert.Equal("Current PM display", human.GetProperty("actorDisplay").GetString());
        Assert.Equal("CURRENT_ACCOUNT", human.GetProperty("actorDisplaySemantics").GetString());
        var legacy = Assert.Single(all.Where(x => x.GetProperty("action").GetString() == "survey_task_created"));
        Assert.Equal(human.GetProperty("eventId").GetGuid(), legacy.GetProperty("eventId").GetGuid());
        Assert.Equal("UNKNOWN", legacy.GetProperty("actorKind").GetString()); Assert.Equal(JsonValueKind.Null, legacy.GetProperty("actorId").ValueKind);
        Assert.DoesNotContain("private", JsonSerializer.Serialize(all), StringComparison.OrdinalIgnoreCase);
        user.Status = UserStatus.Suspended; await db.SaveChangesAsync();
        var supervisorRead = await Body(await sup.GetAsync(path.Replace("pageSize=1", "pageSize=100", StringComparison.Ordinal)));
        var hidden = supervisorRead.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("action").GetString() == "REPAIR.PROPOSED");
        Assert.Equal(JsonValueKind.Null, hidden.GetProperty("actorId").ValueKind); Assert.Equal("ACCOUNT_UNAVAILABLE", hidden.GetProperty("actorUnavailableReason").GetString());
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object body, string version, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString()); request.Headers.Add("If-Match", '"' + version + '"'); return client.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response, int status = 200)
    {
        var raw = await response.Content.ReadAsStringAsync(); Assert.True((int)response.StatusCode == status, $"Expected {status}, got {(int)response.StatusCode}: {raw}"); return JsonDocument.Parse(raw).RootElement.Clone();
    }
}
