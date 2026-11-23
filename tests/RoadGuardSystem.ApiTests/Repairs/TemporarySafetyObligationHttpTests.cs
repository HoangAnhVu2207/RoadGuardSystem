using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class TemporarySafetyObligationHttpTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task DiscoverAppendAndCreateMeasurePreserveSourceAndCurrentResponsibility(bool transferred, bool initialSafety)
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var receiver = await GenuineProtectedSource.Seed(db, sql);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        async Task<string> Email(Guid actor) => (await db.Users.Where(x => x.Id == actor).Select(x => x.Email).SingleAsync())!;
        using var pm = await WebCookieTestClient.Login(factory, await Email(source.Pm));
        using var receivingPm = await WebCookieTestClient.Login(factory, await Email(receiver.Pm));
        using var sup = await WebCookieTestClient.Login(factory, await Email(source.Supervisor));
        using var receiverSup = await WebCookieTestClient.Login(factory, await Email(receiver.Supervisor));
        using var crew = factory.CreateClient();
        using var login = await crew.PostAsJsonAsync("/api/v1/auth/login", new { email = await Email(source.Crew), password = "Current1!" });
        crew.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("accessToken").GetString());
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var scope = new
        {
            physicalRoadId = source.Road,
            routeVersionId = source.Route,
            segmentSetId = source.Set,
            layoutRevisionId = (Guid?)null,
            slabId = (string?)null,
            fromMeters = 1m,
            toMeters = 2m,
            offsetFromMeters = 0m,
            offsetToMeters = 1m
        };
        var obligations = new List<object> { new { kind = "FORMAL_REPAIR", mandatory = true, scope, reason = "actual formal scope" } };
        if (initialSafety) obligations.Add(new { kind = "TEMPORARY_SAFETY", mandatory = true, scope, reason = "initial safety scope" });
        using var created = await Post(pm, $"/api/v1/projects/{source.Project}/repair-packages",
            new { defectId = source.Defect, defectVersion, obligations, reason = "HTTP safety package" }, '"' + defectVersion + '"');
        Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var package = (await Json(created)).GetProperty("id").GetGuid();
        var formal = await db.RepairObligations.Where(x => EF.Property<Guid?>(x, "PackageId") == package && x.Kind == RepairObligationKind.FormalRepair).Select(x => x.Id).SingleAsync();
        var root = $"/api/v1/projects/{source.Project}/repair-packages/{package}";
        using var proposed = await Post(pm, root + "/items", new { obligationId = formal, mode = "NORMAL", repairPlan = "barrier and scoped repair", checklistVersion = "safety-v1", reason = "proposal" }, created.Headers.ETag!.Tag);
        Assert.True(proposed.StatusCode == HttpStatusCode.Created, await proposed.Content.ReadAsStringAsync());
        var item = (await Json(proposed)).GetProperty("id").GetGuid();
        using var approved = await Post(sup, root + $"/items/{item}/approve", new { reason = "approved actual proposal" }, proposed.Headers.ETag!.Tag);
        Assert.True(approved.StatusCode == HttpStatusCode.Created, await approved.Content.ReadAsStringAsync());
        defectVersion = Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        using var assigned = await Post(pm, root + $"/items/{item}/assign", new
        {
            task = new
            {
                defectId = source.Defect,
                defectVersion,
                sourceKind = "REPORTER",
                routeVersionId = source.Route,
                segmentSetId = source.Set,
                purpose = "POST_REPAIR",
                requiredMeasurementType = 5,
                measurementScope = "{}",
                assignedToUserId = source.Crew,
                dueAt = DateTimeOffset.UtcNow.AddDays(1)
            },
            reason = "bind approved normal task"
        }, approved.Headers.ETag!.Tag);
        Assert.True(assigned.StatusCode == HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        var beforeItem = await db.RepairItems.AsNoTracking().SingleAsync(x => x.Id == item);
        var effective = source.Project;
        var manager = pm;
        if (transferred)
        {
            using var current = await sup.GetAsync($"/api/v1/projects/{source.Project}/lifecycle");
            var physical = await db.RepairObligations.AsNoTracking().Include(x => x.Scope).SingleAsync(x => x.Id == formal);
            using var issue = await Post(sup, $"/api/v1/projects/{source.Project}/lifecycle/obligation-transfers",
                new
                {
                    reason = "transfer exact formal",
                    evidenceFileIds = Array.Empty<Guid>(),
                    obligationId = formal,
                    receivingProjectId = receiver.Project,
                    scopeHash = LD06LifecycleAction.HashScope(physical)
                }, current.Headers.ETag!.Tag);
            Assert.True(issue.StatusCode == HttpStatusCode.Created, await issue.Content.ReadAsStringAsync());
            var issueId = (await Json(issue)).GetProperty("actions").EnumerateArray().Single(x => x.GetProperty("kind").GetString() == "IssueTransfer").GetProperty("id").GetGuid();
            using var receiving = await receiverSup.GetAsync($"/api/v1/projects/{receiver.Project}/lifecycle");
            using var accepted = await Post(receiverSup, $"/api/v1/projects/{receiver.Project}/lifecycle/obligation-transfer-acceptances",
                new
                {
                    reason = "receive formal",
                    evidenceFileIds = Array.Empty<Guid>(),
                    sourceActionId = issueId,
                    obligationId = formal,
                    scopeHash = LD06LifecycleAction.HashScope(physical)
                }, receiving.Headers.ETag!.Tag);
            Assert.True(accepted.StatusCode == HttpStatusCode.Created, await accepted.Content.ReadAsStringAsync());
            effective = receiver.Project; manager = receivingPm;
        }
        var path = $"/api/v1/projects/{effective}/repair-packages/{package}/items/{item}/temporary-safety-obligations";
        using var discovery = await manager.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, discovery.StatusCode);
        Assert.Equal(initialSafety ? 1 : 0, (await Json(discovery)).GetProperty("items").GetArrayLength());
        var safetyScope = new
        {
            physicalRoadId = source.Road,
            routeVersionId = source.Route,
            segmentSetId = source.Set,
            layoutRevisionId = (Guid?)null,
            slabId = (string?)null,
            fromMeters = 1m,
            toMeters = transferred ? 1.4m : 2m,
            offsetFromMeters = 0m,
            offsetToMeters = 1m
        };
        var input = new { scope = safetyScope, mandatory = true, reason = "add actual temporary safety" };
        var key = Guid.NewGuid().ToString();
        Guid safety;
        string itemVersion;
        if (!initialSafety)
        {
            var savedCsrf = manager.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
            manager.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            using var noCsrf = await Post(manager, path, input, discovery.Headers.ETag!.Tag);
            Assert.Equal(HttpStatusCode.Forbidden, noCsrf.StatusCode);
            manager.DefaultRequestHeaders.Add("X-CSRF-TOKEN", savedCsrf);
            using var stale = await Post(manager, path, input, "\"AAAAAAAAAAA=\"");
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            var secondKey = transferred ? key : Guid.NewGuid().ToString();
            var race = await Task.WhenAll(Post(manager, path, input, discovery.Headers.ETag!.Tag, key), Post(manager, path, input, discovery.Headers.ETag!.Tag, secondKey));
            Assert.Single(race.Where(x => x.StatusCode == HttpStatusCode.Created));
            Assert.Equal(transferred ? HttpStatusCode.OK : HttpStatusCode.Conflict, race.Single(x => x.StatusCode != HttpStatusCode.Created).StatusCode);
            if (race[0].StatusCode != HttpStatusCode.Created) key = secondKey;
            var added = race.Single(x => x.StatusCode == HttpStatusCode.Created);
            var view = await Json(added);
            safety = Assert.Single(view.GetProperty("items").EnumerateArray()).GetProperty("safetyObligationId").GetGuid();
            itemVersion = view.GetProperty("itemVersion").GetString()!;
            Assert.Equal(effective, view.GetProperty("responsibilityProjectId").GetGuid());
            Assert.Equal(source.Project, view.GetProperty("items")[0].GetProperty("originProjectId").GetGuid());
            using var replay = await Post(manager, path, input, discovery.Headers.ETag!.Tag, key);
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Equal(safety, (await Json(replay)).GetProperty("items")[0].GetProperty("safetyObligationId").GetGuid());
            using var changed = await Post(manager, path, new { scope = safetyScope, mandatory = true, reason = "changed fingerprint" }, discovery.Headers.ETag!.Tag, key);
            Assert.Equal("idempotency_key_reused", (await Json(changed)).GetProperty("code").GetString());
        }
        else
        {
            var view = await Json(discovery);
            safety = view.GetProperty("items")[0].GetProperty("safetyObligationId").GetGuid();
            itemVersion = view.GetProperty("itemVersion").GetString()!;
            using var duplicate = await Post(manager, path, input, discovery.Headers.ETag!.Tag);
            Assert.Equal("active_scope_conflict", (await Json(duplicate)).GetProperty("code").GetString());
        }
        using var reload = await WebCookieTestClient.Login(factory, await Email(transferred ? receiver.Pm : source.Pm));
        using var readback = await reload.GetAsync(path);
        Assert.Equal(safety, (await Json(readback)).GetProperty("items")[0].GetProperty("safetyObligationId").GetGuid());
        var evidence = Environment.GetEnvironmentVariable("RG_TEMPORARY_SAFETY_HTTP_EVIDENCE");
        if (evidence is not null && transferred) await File.WriteAllTextAsync(evidence, await readback.Content.ReadAsStringAsync());
        using var invalidScope = await Post(manager, path, new
        {
            scope = new
            {
                physicalRoadId = source.Road,
                routeVersionId = source.Route,
                segmentSetId = source.Set,
                fromMeters = 0m,
                toMeters = 3m,
                offsetFromMeters = 0m,
                offsetToMeters = 1m
            },
            mandatory = true,
            reason = "outside formal"
        }, readback.Headers.ETag!.Tag);
        Assert.Equal("safety_obligation_source_conflict", (await Json(invalidScope)).GetProperty("code").GetString());
        var measurePath = $"/api/v1/projects/{effective}/repair-packages/{package}/items/{item}/safety-measures";
        using var wrongKind = await Post(manager, measurePath, new
        {
            formalRepairObligationId = formal,
            safetyObligationId = formal,
            responsibleActorId = source.Crew,
            checkSchedule = "each shift",
            replacementCondition = "if displaced",
            removalCondition = "after formal repair",
            reason = "wrong kind"
        }, '"' + itemVersion + '"');
        Assert.Equal("safety_obligation_source_conflict", (await Json(wrongKind)).GetProperty("code").GetString());
        using var wrongCrew = await Post(manager, measurePath, new
        {
            formalRepairObligationId = formal,
            safetyObligationId = safety,
            responsibleActorId = receiver.Crew,
            checkSchedule = "each shift",
            replacementCondition = "if displaced",
            removalCondition = "after formal repair",
            reason = "Crew from another project"
        }, '"' + itemVersion + '"');
        Assert.Equal(HttpStatusCode.Forbidden, wrongCrew.StatusCode);
        Assert.Equal("safety_scope_not_authorized", (await Json(wrongCrew)).GetProperty("code").GetString());
        using var measure = await Post(manager, measurePath, new
        {
            formalRepairObligationId = formal,
            safetyObligationId = safety,
            responsibleActorId = source.Crew,
            checkSchedule = "each shift",
            replacementCondition = "if displaced",
            removalCondition = "after formal repair",
            reason = "configure actual safety"
        }, '"' + itemVersion + '"');
        Assert.True(measure.StatusCode == HttpStatusCode.Created, await measure.Content.ReadAsStringAsync());
        var measureId = (await Json(measure)).GetProperty("id").GetGuid();
        var crewPath = $"/api/v1/projects/{source.Project}/repair-packages/{package}/items/{item}/safety-measures";
        using var crewList = await crew.GetAsync(crewPath);
        Assert.Contains(measureId, (await Json(crewList)).GetProperty("measureIds").EnumerateArray().Select(x => x.GetGuid()));
        using var crewRead = await crew.GetAsync(crewPath + "/" + measureId);
        Assert.Equal(HttpStatusCode.OK, crewRead.StatusCode);
        Assert.Equal(safety, (await Json(crewRead)).GetProperty("safetyObligationId").GetGuid());
        using var used = await manager.GetAsync(path);
        Assert.Empty((await Json(used)).GetProperty("items").EnumerateArray());
        using var crewCannotChoose = await crew.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, crewCannotChoose.StatusCode);
        if (transferred)
        {
            using var oldPm = await pm.GetAsync(path.Replace(effective.ToString(), source.Project.ToString(), StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.NotFound, oldPm.StatusCode);
            var owner = await db.Set<ObligationResponsibility>().AsNoTracking().SingleAsync(x => x.ObligationId == safety);
            Assert.Equal(source.Project, owner.OriginProjectId); Assert.Equal(receiver.Project, owner.CurrentProjectId);
            var creation = await db.Set<LD06LifecycleAction>().SingleAsync(x => x.Id == owner.AcceptanceActionId);
            Assert.Equal(LD06ActionKind.InitializeTemporarySafetyResponsibility, creation.Kind);
            Assert.Equal(safety, creation.ObligationId);
            Assert.False(await db.Set<LD06LifecycleAction>().AnyAsync(x => x.ObligationId == safety && x.Kind == LD06ActionKind.AcceptTransfer));
            using var lifecycle = await receivingPm.GetAsync($"/api/v1/projects/{receiver.Project}/lifecycle");
            var safetyOwner = (await Json(lifecycle)).GetProperty("responsibilities").EnumerateArray().Single(x => x.GetProperty("obligationId").GetGuid() == safety);
            Assert.Equal(JsonValueKind.Null, safetyOwner.GetProperty("acceptanceActionId").ValueKind);
            Assert.Equal(creation.Id, safetyOwner.GetProperty("initialResponsibilityActionId").GetGuid());

            // Formal transfer and append serialize against the same authority; existing safety never follows it.
            var third = await GenuineProtectedSource.Seed(db, sql);
            using var thirdSup = await WebCookieTestClient.Login(factory, await Email(third.Supervisor));
            using var ownerState = await receiverSup.GetAsync($"/api/v1/projects/{receiver.Project}/lifecycle");
            var formalSource = await db.RepairObligations.AsNoTracking().Include(x => x.Scope).SingleAsync(x => x.Id == formal);
            using var nextIssue = await Post(receiverSup, $"/api/v1/projects/{receiver.Project}/lifecycle/obligation-transfers",
                new
                {
                    reason = "transfer while safety append competes",
                    evidenceFileIds = Array.Empty<Guid>(),
                    obligationId = formal,
                    receivingProjectId = third.Project,
                    scopeHash = LD06LifecycleAction.HashScope(formalSource)
                }, ownerState.Headers.ETag!.Tag);
            Assert.Equal(HttpStatusCode.Created, nextIssue.StatusCode);
            var nextIssueId = (await Json(nextIssue)).GetProperty("actions").EnumerateArray().Single(x => x.GetProperty("kind").GetString() == "IssueTransfer" && x.GetProperty("receivingProjectId").GetGuid() == third.Project).GetProperty("id").GetGuid();
            using var thirdState = await thirdSup.GetAsync($"/api/v1/projects/{third.Project}/lifecycle");
            using var latestPackage = await manager.GetAsync(path);
            var append = Post(manager, path, new
            {
                scope = new
                {
                    physicalRoadId = source.Road,
                    routeVersionId = source.Route,
                    segmentSetId = source.Set,
                    fromMeters = 1.6m,
                    toMeters = 2m,
                    offsetFromMeters = 0m,
                    offsetToMeters = 1m
                },
                mandatory = true,
                reason = "disjoint safety racing owner change"
            }, latestPackage.Headers.ETag!.Tag);
            var accept = Post(thirdSup, $"/api/v1/projects/{third.Project}/lifecycle/obligation-transfer-acceptances",
                new
                {
                    reason = "receive exact formal",
                    evidenceFileIds = Array.Empty<Guid>(),
                    sourceActionId = nextIssueId,
                    obligationId = formal,
                    scopeHash = LD06LifecycleAction.HashScope(formalSource)
                }, thirdState.Headers.ETag!.Tag);
            await Task.WhenAll(append, accept);
            using var appendResult = await append; using var acceptResult = await accept;
            Assert.Equal(HttpStatusCode.Created, acceptResult.StatusCode);
            Assert.True(appendResult.StatusCode is HttpStatusCode.Created or HttpStatusCode.NotFound, await appendResult.Content.ReadAsStringAsync());
            Assert.Equal(third.Project, await db.Set<ObligationResponsibility>().Where(x => x.ObligationId == formal).Select(x => x.CurrentProjectId).SingleAsync());
            Assert.Equal(receiver.Project, await db.Set<ObligationResponsibility>().Where(x => x.ObligationId == safety).Select(x => x.CurrentProjectId).SingleAsync());
            if (appendResult.StatusCode == HttpStatusCode.Created)
            {
                var extra = (await Json(appendResult)).GetProperty("items")[0].GetProperty("safetyObligationId").GetGuid();
                Assert.Equal(receiver.Project, await db.Set<ObligationResponsibility>().Where(x => x.ObligationId == extra).Select(x => x.CurrentProjectId).SingleAsync());
            }
            var blockedDown = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261123093000_FieldTaskMeasurementDimensions"));
            Assert.Equal(51991, blockedDown.Number);
            Assert.Contains("20261123093100_InitialTemporarySafetyResponsibility", await db.Database.GetAppliedMigrationsAsync());
            using var noLongerOwner = await manager.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, noLongerOwner.StatusCode);

        }
        var afterItem = await db.RepairItems.AsNoTracking().SingleAsync(x => x.Id == item);
        Assert.Equal(beforeItem.State, afterItem.State); Assert.Equal(beforeItem.CurrentBindingId, afterItem.CurrentBindingId);
        Assert.Equal(beforeItem.ApprovedPlanHash, afterItem.ApprovedPlanHash);
        Assert.Equal(1, await db.Set<RepairSafetyMonitoring>().CountAsync(x => x.SafetyObligationId == safety));
        Assert.Equal(initialSafety ? 0 : 1, await db.AuditLogs.CountAsync(x => x.EntityId == safety && x.EventType == "repair_temporary_safety_added"));
        await db.ProjectMembers.Where(x => x.ProjectId == effective && x.UserId == (transferred ? receiver.Pm : source.Pm))
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        using var revoked = await manager.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        if (!initialSafety)
        {
            using var revokedReplay = await Post(manager, path, input, discovery.Headers.ETag!.Tag, key);
            Assert.Equal(HttpStatusCode.Forbidden, revokedReplay.StatusCode);
        }
    }

    [Fact]
    public async Task SwaggerDescribesSafetyVersionsCookieAlternativesAndAiReadback()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var swagger = factory.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
        using var document = JsonDocument.Parse(swagger.SerializeAsJson(Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0));
        var paths = document.RootElement.GetProperty("paths");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.GetProperty("AiMockRunView").GetProperty("properties").GetProperty("failure").GetProperty("nullable").GetBoolean());
        Assert.True(schemas.GetProperty("TemporarySafetyObligationSourcesView").GetProperty("properties").GetProperty("binding").GetProperty("nullable").GetBoolean());
        var safety = paths.GetProperty("/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/temporary-safety-obligations");
        Assert.Equal(2, safety.GetProperty("get").GetProperty("security").GetArrayLength());
        var post = safety.GetProperty("post");
        Assert.Equal(2, post.GetProperty("security").GetArrayLength());
        Assert.True(post.GetProperty("security")[1].TryGetProperty("CsrfToken", out _));
        Assert.Contains(post.GetProperty("parameters").EnumerateArray(), x => x.GetProperty("name").GetString() == "If-Match" && x.GetProperty("required").GetBoolean());
        Assert.Contains(post.GetProperty("parameters").EnumerateArray(), x => x.GetProperty("name").GetString() == "Idempotency-Key" && x.GetProperty("required").GetBoolean());
        var candidate = paths.GetProperty("/api/v1/projects/{projectId}/ai-candidates/{detectionId}").GetProperty("get");
        Assert.Equal(2, candidate.GetProperty("security").GetArrayLength());
        Assert.Contains("disposition.decisionId", candidate.GetProperty("description").GetString());
    }

    private static async Task<JsonElement> Json(HttpResponseMessage message) { using var document = JsonDocument.Parse(await message.Content.ReadAsStringAsync()); return document.RootElement.Clone(); }
    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object input, string version, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.TryAddWithoutValidation("If-Match", version); request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }
}
