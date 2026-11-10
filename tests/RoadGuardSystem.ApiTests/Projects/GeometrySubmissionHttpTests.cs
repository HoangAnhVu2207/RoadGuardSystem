using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class GeometrySubmissionHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task WebSubmissionPinsContentAndSupervisorFindsItByProject()
    {
        var pm = await sql.CreateUserAsync($"geometry-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var outsider = await sql.CreateUserAsync($"geometry-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync($"geometry-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var project = await Seed(pm.Id);
        var otherProject = await Seed(pm.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var author = await WebCookieTestClient.Login(factory, pm.Email!);
        using var reader = await WebCookieTestClient.Login(factory, supervisor.Email!);
        using var denied = await WebCookieTestClient.Login(factory, outsider.Email!);
        var root = $"/api/v1/projects/{project}";
        var lookup = root + "/road-geometry-submission";
        var (path, etag) = await CreateDraft(author, root, "PINNED");
        var directRoad = new
        {
            code = "OTHER",
            srid = 32648,
            coordinates = new[] { new { x = 500000, y = 1200000 }, new { x = 500020, y = 1200000 } },
            effectiveFrom = DateTimeOffset.UtcNow,
            changeReason = "legacy route",
            operationId = Guid.NewGuid()
        };
        using var initialRoad = await Send(reader, HttpMethod.Post, root + "/road-sections", directRoad);
        Assert.Equal(HttpStatusCode.Created, initialRoad.StatusCode);
        var otherSection = (await initialRoad.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roadSectionId").GetGuid();
        using var otherDraft = await Send(author, HttpMethod.Post, root + $"/road-sections/{otherSection}/geometry-drafts", Draft("OTHER"));
        Assert.Equal(HttpStatusCode.Created, otherDraft.StatusCode);
        var second = otherDraft.Headers.Location!.OriginalString;
        var secondEtag = otherDraft.Headers.ETag!;
        var otherDraftId = (await otherDraft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var draftId = Guid.Parse(path[(path.LastIndexOf('/') + 1)..]);
        var changed = Draft("PINNED", 25);
        using var edit = await Send(author, HttpMethod.Put, path, changed, etag);
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var updated = edit.Headers.ETag!;
        var confirmation = new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = DateTimeOffset.UtcNow, reason = "approved" };

        await Error(await reader.GetAsync(lookup), HttpStatusCode.NotFound, "road_geometry_submission_not_found");
        await Error(await denied.GetAsync(lookup), HttpStatusCode.Forbidden, "access_forbidden");
        await Error(await Send(reader, HttpMethod.Post, path + "/confirm", confirmation, updated),
            HttpStatusCode.Conflict, "road_geometry_submission_required");
        await Error(await Send(reader, HttpMethod.Post, path + "/submit", etag: updated), HttpStatusCode.Forbidden, "access_forbidden");
        await Error(await Send(denied, HttpMethod.Post, path + "/submit", etag: updated), HttpStatusCode.Forbidden, "access_forbidden");
        await Error(await Send(author, HttpMethod.Post, $"/api/v1/projects/{otherProject}/road-geometry-drafts/{draftId}/submit", etag: updated),
            HttpStatusCode.NotFound, "road_geometry_draft_not_found");
        await Error(await Send(author, HttpMethod.Post, path + "/submit", etag: etag), HttpStatusCode.PreconditionFailed, "concurrency_conflict");
        author.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        await Error(await Send(author, HttpMethod.Post, path + "/submit", etag: updated), HttpStatusCode.Forbidden, "csrf_failed");
        await WebCookieTestClient.RefreshCsrf(author);

        var key = Guid.NewGuid().ToString();
        using var submitted = await Send(author, HttpMethod.Post, path + "/submit", etag: updated, key: key);
        Assert.Equal(HttpStatusCode.Created, submitted.StatusCode);
        Assert.Equal(lookup, submitted.Headers.Location!.OriginalString);
        using var found = await reader.GetAsync(lookup);
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(submitted.Headers.ETag, found.Headers.ETag);
        var dossier = await found.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(draftId, dossier.GetProperty("draftId").GetGuid());
        Assert.Equal("SUBMITTED", dossier.GetProperty("status").GetString());
        Assert.Equal(updated.Tag.Trim('"'), dossier.GetProperty("submittedContentVersion").GetString());
        Assert.Equal(JsonValueKind.Null, dossier.GetProperty("roadSectionId").ValueKind);
        using var replay = await Send(author, HttpMethod.Post, path + "/submit", etag: updated, key: key);
        Assert.Equal(submitted.Headers.ETag, replay.Headers.ETag);
        Assert.Equal(await submitted.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        await Error(await Send(author, HttpMethod.Post, second + "/submit", etag: secondEtag, key: key), HttpStatusCode.Conflict, "duplicate_request");
        await Error(await Send(author, HttpMethod.Post, second + "/submit", etag: secondEtag), HttpStatusCode.Conflict, "road_geometry_submission_exists");
        await Error(await Send(author, HttpMethod.Put, path, changed, found.Headers.ETag), HttpStatusCode.Conflict, "road_geometry_submission_locked");
        await Error(await Send(author, HttpMethod.Put, root + $"/road-sections/{otherSection}/geometry-drafts/{otherDraftId}", Draft("OTHER", 25), secondEtag),
            HttpStatusCode.Conflict, "road_geometry_submission_locked");
        await Error(await Send(reader, HttpMethod.Post, root + "/road-sections", directRoad with { code = "PINNED", operationId = Guid.NewGuid() }),
            HttpStatusCode.Conflict, "road_geometry_submission_locked");
        await Error(await Send(reader, HttpMethod.Post, second + "/confirm", confirmation, secondEtag), HttpStatusCode.Conflict, "road_geometry_submission_required");
        await Error(await Send(reader, HttpMethod.Post, path + "/confirm", confirmation, updated), HttpStatusCode.PreconditionFailed, "concurrency_conflict");
        await Error(await Send(reader, HttpMethod.Post, path + "/confirm", confirmation with { expectedCurrentVersionId = Guid.NewGuid() }, found.Headers.ETag),
            HttpStatusCode.Conflict, "road_section_concurrency_conflict");
        using var confirmed = await Send(reader, HttpMethod.Post, path + "/confirm", confirmation, found.Headers.ETag);
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        var route = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        var section = route.GetProperty("roadSectionId").GetGuid();
        var version = route.GetProperty("routeVersionId").GetGuid();
        await Error(await Send(reader, HttpMethod.Post, root + $"/road-sections/{section}/versions", new
        {
            directRoad.srid,
            directRoad.coordinates,
            directRoad.effectiveFrom,
            directRoad.changeReason,
            operationId = Guid.NewGuid(),
            expectedCurrentVersionId = version
        }), HttpStatusCode.Conflict, "road_geometry_submission_locked");
        await Error(await Send(author, HttpMethod.Put, root + $"/road-sections/{section}/geometry-drafts/{draftId}", changed, found.Headers.ETag),
            HttpStatusCode.Conflict, "road_geometry_submission_locked");
        await Error(await Send(author, HttpMethod.Post, second + "/submit", etag: secondEtag), HttpStatusCode.Conflict, "road_geometry_submission_exists");
        foreach (var client in new[] { author, reader })
        {
            using var reopened = await client.GetAsync(lookup);
            var current = await reopened.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("CONFIRMED", current.GetProperty("status").GetString());
            Assert.Equal(version, current.GetProperty("confirmedRouteVersionId").GetGuid());
            using var detail = await client.GetAsync(path);
            Assert.Equal(reopened.Headers.ETag, detail.Headers.ETag);
            var content = await detail.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("CONFIRMED", content.GetProperty("status").GetString());
            Assert.Equal(500025, content.GetProperty("editedCoordinates")[1].GetProperty("x").GetDouble());
        }
        await using var db = sql.CreateDbContext();
        var membership = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project && x.UserId == pm.Id);
        membership.Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync();
        await Error(await Send(author, HttpMethod.Post, path + "/submit", etag: updated, key: key), HttpStatusCode.Forbidden, "access_forbidden");
    }

    [Theory]
    [InlineData("different-drafts")]
    [InlineData("same-receipt")]
    [InlineData("edit")]
    public async Task ConcurrentGeometryWritesLeaveExactlyOneConsistentSubmission(string scenario)
    {
        var pm = await sql.CreateUserAsync($"geometry-race-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = await Seed(pm.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var one = await WebCookieTestClient.Login(factory, pm.Email!);
        using var two = await WebCookieTestClient.Login(factory, pm.Email!);
        var root = $"/api/v1/projects/{project}";
        var (path, etag) = await CreateDraft(one, root, "RACE");
        var (other, otherEtag) = scenario == "different-drafts" ? await CreateDraft(two, root, "OTHER") : (path, etag);
        var key = Guid.NewGuid().ToString();
        var responses = await Task.WhenAll(
            Send(one, HttpMethod.Post, path + "/submit", etag: etag, key: key),
            scenario == "edit"
                ? Send(two, HttpMethod.Put, path, Draft("RACE", 25), etag)
                : Send(two, HttpMethod.Post, other + "/submit", etag: otherEtag, key: scenario == "same-receipt" ? key : null));
        using var first = responses[0];
        using var second = responses[1];
        if (scenario == "same-receipt")
        {
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(first.StatusCode, second.StatusCode);
            Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        }
        else if (scenario == "different-drafts")
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        }
        else if (second.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal(HttpStatusCode.PreconditionFailed, first.StatusCode);
            using var retried = await Submit(one, path, second.Headers.ETag!);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }
        await using var db = sql.CreateDbContext();
        var stored = await db.Set<RoadGeometryDraft>().SingleAsync(x => x.ProjectId == project && x.SubmittedAt != null);
        Assert.Equal("SUBMITTED", stored.Status);
        Assert.Equal(scenario == "edit" && second.StatusCode == HttpStatusCode.OK ? second.Headers.ETag!.Tag.Trim('"') : etag.Tag.Trim('"'), stored.SubmittedContentVersion);
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.ProjectId == project && x.Operation == "Geometry:draft-submit"));
    }

    private async Task<Guid> Seed(Guid pm)
    {
        await using var db = sql.CreateDbContext();
        var id = Guid.NewGuid();
        db.AddRange(Project.Create(id, id.ToString("N"), "Submission test", null, 32648, null, null, DateTimeOffset.UtcNow),
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), id, pm, new(2000, 1, 1)));
        await db.SaveChangesAsync();
        return id;
    }

    private static object Draft(string code, int length = 20) => new
    {
        sourceKind = "COORDINATES",
        sourceCrs = 32648,
        stationOriginMeters = 0,
        changeReason = "synthetic",
        coordinates = new[] { new { x = 500000, y = 1200000 }, new { x = 500000 + length, y = 1200000 } },
        widthProfile = new[] { new { fromOffsetMeters = 0, toOffsetMeters = length, widthMeters = 7 } },
        surveyWidthMeters = 9,
        roadCode = code
    };

    private static async Task<(string Path, EntityTagHeaderValue ETag)> CreateDraft(HttpClient client, string root, string code)
    {
        using var response = await Send(client, HttpMethod.Post, root + "/road-geometry-drafts", Draft(code));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (response.Headers.Location!.OriginalString, response.Headers.ETag!);
    }

    internal static async Task<HttpResponseMessage> Submit(HttpClient client, string path, EntityTagHeaderValue etag)
    {
        var response = await Send(client, HttpMethod.Post, path + "/submit", etag: etag);
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return response;
    }

    internal static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path,
        object? body = null, EntityTagHeaderValue? etag = null, string? key = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (etag is not null) request.Headers.IfMatch.Add(etag);
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return await client.SendAsync(request);
    }

    internal static async Task MarkAsPreMigrationConfirmedFixture(AuthenticationSqlServerFixture sql, Guid draft)
    {
        await using var db = sql.CreateDbContext();
        // The historical topology and replacement fixtures start with a route confirmed before submissions existed.
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE RoadGeometryDrafts SET SubmittedAt=NULL, SubmittedBy=NULL, SubmittedContentVersion=NULL WHERE Id={draft} AND Status='CONFIRMED'"));
    }

    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        using (response)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }
}
