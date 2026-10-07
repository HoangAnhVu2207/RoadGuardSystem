using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Anh01GeometryWorkflowTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task Metric_draft_confirm_publish_replay_and_historical_read_are_durable()
    {
        var supervisor = await sql.CreateUserAsync($"geometry_supervisor_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync($"geometry_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var projectResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"P-{Guid.NewGuid():N}",
            name = "Geometry workflow test",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        projectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await projectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
        var prefix = $"/api/v1/projects/{project}";
        await Login(client, manager.UserName!);
        var input = new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            stationOriginMeters = 1000d,
            changeReason = "surveyed alignment",
            coordinates = new[] { new { x = 500000d, y = 1200000d }, new { x = 500250d, y = 1200000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 250d, widthMeters = 7d } },
            surveyWidthMeters = 9d,
            roadCode = "ANH-GEOM"
        };
        var missing = await client.PostAsJsonAsync(prefix + "/road-geometry-drafts", input); missing.StatusCode.Should().Be((HttpStatusCode)428);
        var createKey = Guid.NewGuid().ToString("N");
        var create = await Send(client, HttpMethod.Post, prefix + "/road-geometry-drafts", input, createKey);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var draft = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var draftPath = prefix + $"/road-geometry-drafts/{draft}";
        var createReplay = await Send(client, HttpMethod.Post, prefix + "/road-geometry-drafts", input, createKey);
        (await createReplay.Content.ReadAsStringAsync()).Should().Be(await create.Content.ReadAsStringAsync());
        var conflictInput = JsonSerializer.SerializeToNode(input)!; conflictInput["roadCode"] = "CHANGED-CODE";
        var keyConflict = await Send(client, HttpMethod.Post, prefix + "/road-geometry-drafts", conflictInput, createKey);
        keyConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var preview = await client.PostAsync(draftPath + "/preview", null); preview.StatusCode.Should().Be(HttpStatusCode.OK);
        (await preview.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("lengthMeters").GetDouble().Should().Be(250);
        var confirmation = new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = "2026-10-02T00:00:00Z", reason = "Supervisor approved" };
        var forbidden = await Send(client, HttpMethod.Post, draftPath + "/confirm", confirmation, Guid.NewGuid().ToString(), create.Headers.ETag!.ToString());
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Login(client, supervisor.UserName!);
        var confirmKey = Guid.NewGuid().ToString();
        var confirmed = await Send(client, HttpMethod.Post, draftPath + "/confirm", confirmation, confirmKey, create.Headers.ETag!.ToString());
        confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
        var route = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        var section = route.GetProperty("roadSectionId").GetGuid(); var version = route.GetProperty("routeVersionId").GetGuid();
        var replay = await Send(client, HttpMethod.Post, draftPath + "/confirm", confirmation, confirmKey, create.Headers.ETag!.ToString());
        (await replay.Content.ReadAsStringAsync()).Should().Be(await confirmed.Content.ReadAsStringAsync());
        await Login(client, manager.UserName!);
        var edit = await Send(client, HttpMethod.Put, draftPath, input, Guid.NewGuid().ToString(), create.Headers.ETag.ToString()); edit.StatusCode.Should().Be((HttpStatusCode)412);
        var setPath = prefix + $"/road-sections/{section}/versions/{version}/segment-sets";
        var definition = new { targetLengthMeters = 100d, remainderMode = "KEEP" };
        var set = await Send(client, HttpMethod.Post, setPath, definition, Guid.NewGuid().ToString()); set.StatusCode.Should().Be(HttpStatusCode.Created);
        var setBody = await set.Content.ReadFromJsonAsync<JsonElement>(); var setId = setBody.GetProperty("id").GetGuid();
        setBody.GetProperty("segments").GetArrayLength().Should().Be(3);
        var editKey = Guid.NewGuid().ToString();
        var noOp = await Send(client, HttpMethod.Put, setPath + $"/{setId}", definition, editKey, set.Headers.ETag!.ToString());
        noOp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await noOp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("segments").EnumerateArray().Select(x => x.GetProperty("id").GetGuid())
            .Should().Equal(setBody.GetProperty("segments").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()), "no-op edits preserve segment references");
        noOp.Headers.ETag.Should().Be(set.Headers.ETag);
        var noOpReplay = await Send(client, HttpMethod.Put, setPath + $"/{setId}", definition, editKey, set.Headers.ETag.ToString());
        (await noOpReplay.Content.ReadAsStringAsync()).Should().Be(await noOp.Content.ReadAsStringAsync());
        var changed = await Send(client, HttpMethod.Put, setPath + $"/{setId}", new { targetLengthMeters = 125d, remainderMode = "KEEP" }, Guid.NewGuid().ToString(), set.Headers.ETag.ToString());
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        changed.Headers.ETag.Should().NotBe(set.Headers.ETag);
        var stalePublish = await Send(client, HttpMethod.Post, setPath + $"/{setId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "Stale definition" }, Guid.NewGuid().ToString(), set.Headers.ETag.ToString());
        stalePublish.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var restored = await Send(client, HttpMethod.Put, setPath + $"/{setId}", definition, Guid.NewGuid().ToString(), changed.Headers.ETag!.ToString());
        restored.StatusCode.Should().Be(HttpStatusCode.OK);
        set = restored;
        var publishKey = Guid.NewGuid().ToString();
        var publishInput = new { expectedPublishedSetId = (Guid?)null, reason = "PM selected segmentation" };
        var published = await Send(client, HttpMethod.Post, setPath + $"/{setId}/publish", publishInput, publishKey, set.Headers.ETag!.ToString()); published.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishReplay = await Send(client, HttpMethod.Post, setPath + $"/{setId}/publish", publishInput, publishKey, set.Headers.ETag.ToString());
        (await publishReplay.Content.ReadAsStringAsync()).Should().Be(await published.Content.ReadAsStringAsync());
        var package = await client.GetAsync(prefix + $"/geometry-package?routeVersionId={version}&segmentSetId={setId}"); package.StatusCode.Should().Be(HttpStatusCode.OK);
        (await package.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("schemaVersion").GetString().Should().Be("anh01.geometry.v1");
        var replacement = await Send(client, HttpMethod.Post, setPath, new { targetLengthMeters = 100d, remainderMode = "MERGE_PREVIOUS" }, Guid.NewGuid().ToString());
        replacement.StatusCode.Should().Be(HttpStatusCode.Created);
        var replacementId = (await replacement.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var conflictPublish = await Send(client, HttpMethod.Post, setPath + $"/{replacementId}/publish", publishInput, Guid.NewGuid().ToString(), replacement.Headers.ETag!.ToString());
        conflictPublish.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var replaced = await Send(client, HttpMethod.Post, setPath + $"/{replacementId}/publish", new { expectedPublishedSetId = setId, reason = "PM chose merged remainder" }, Guid.NewGuid().ToString(), replacement.Headers.ETag.ToString());
        replaced.StatusCode.Should().Be(HttpStatusCode.OK);
        var historical = await client.GetAsync(setPath + $"/{setId}"); historical.StatusCode.Should().Be(HttpStatusCode.OK);
        (await historical.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("SUPERSEDED");
        var historicalPackage = await client.GetAsync(prefix + $"/geometry-package?routeVersionId={version}&segmentSetId={setId}"); historicalPackage.StatusCode.Should().Be(HttpStatusCode.OK);
        var competingA = await Send(client, HttpMethod.Post, setPath, new { targetLengthMeters = 125d, remainderMode = "KEEP" }, Guid.NewGuid().ToString());
        var competingB = await Send(client, HttpMethod.Post, setPath, new { targetLengthMeters = 50d, remainderMode = "KEEP" }, Guid.NewGuid().ToString());
        competingA.StatusCode.Should().Be(HttpStatusCode.Created); competingB.StatusCode.Should().Be(HttpStatusCode.Created);
        var competingAId = (await competingA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var competingBId = (await competingB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var raceInput = new { expectedPublishedSetId = replacementId, reason = "Concurrent PM publish" };
        var race = await Task.WhenAll(
            Send(client, HttpMethod.Post, setPath + $"/{competingAId}/publish", raceInput, Guid.NewGuid().ToString(), competingA.Headers.ETag!.ToString()),
            Send(client, HttpMethod.Post, setPath + $"/{competingBId}/publish", raceInput, Guid.NewGuid().ToString(), competingB.Headers.ETag!.ToString()));
        race.Count(x => x.StatusCode == HttpStatusCode.OK).Should().Be(1);
        race.Count(x => x.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        var losing = race.Single(x => x.StatusCode == HttpStatusCode.Conflict);
        (await losing.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("segment_publication_conflict");
        var nextDraft = await Send(client, HttpMethod.Post, prefix + $"/road-sections/{section}/geometry-drafts", input, Guid.NewGuid().ToString());
        nextDraft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var nextDraftPath = nextDraft.Headers.Location!.OriginalString;
        var failedConfirm = await Send(client, HttpMethod.Post, nextDraftPath + "/confirm", new { expectedCurrentVersionId = Guid.NewGuid(), effectiveFrom = "2026-10-03T00:00:00Z", reason = "Stale expected route" }, Guid.NewGuid().ToString(), nextDraft.Headers.ETag!.ToString());
        failedConfirm.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var nextConfirm = await Send(client, HttpMethod.Post, nextDraftPath + "/confirm", new { expectedCurrentVersionId = version, effectiveFrom = "2026-10-03T00:00:00Z", reason = "Replacement route" }, Guid.NewGuid().ToString(), nextDraft.Headers.ETag.ToString());
        nextConfirm.StatusCode.Should().Be(HttpStatusCode.Created);
        (await nextConfirm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roadSectionId").GetGuid().Should().Be(section);
        var changedPackage = await client.GetAsync(prefix + $"/geometry-package?routeVersionId={version}&segmentSetId={setId}");
        changedPackage.StatusCode.Should().Be(HttpStatusCode.OK);
        changedPackage.Headers.ETag!.ToString().Should().NotBe(historicalPackage.Headers.ETag!.ToString());
        (await changedPackage.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("route").GetProperty("isCurrent").GetBoolean().Should().BeFalse();
        await using var db = sql.CreateDbContext();
        (await db.Set<RoadGeometryDraft>().CountAsync(x => x.ProjectId == project)).Should().Be(2);
        (await db.RoadSectionVersions.CountAsync(x => x.RoadSectionId == section)).Should().Be(2);
        (await db.RoadSectionVersions.CountAsync(x => x.RoadSectionId == section && x.IsCurrent)).Should().Be(1);
        (await db.Set<RoadGeometryMetadata>().CountAsync(x => x.RoadSectionVersionId == version)).Should().Be(1);
        (await db.RoadSegmentSets.CountAsync(x => x.RoadSectionVersionId == version && x.Status == "PUBLISHED")).Should().Be(1);
        (await db.RoadSegments.CountAsync(x => x.SegmentSetId == setId)).Should().Be(3);
        (await db.IdempotencyRecords.CountAsync(x => x.ProjectId == project && x.Operation.StartsWith("Geometry:"))).Should().Be(14);
    }
    private static async Task Login(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(name), password = "Current1!" }); response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object input, string key, string? etag = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(input) }; request.Headers.Add("Idempotency-Key", key); if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag); return await client.SendAsync(request);
    }
}
