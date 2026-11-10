using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H2NativeWorkflowHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task CoordinatesDraftPreviewReadinessAndSupervisorConfirmationUseProjectCrs()
    {
        var pm = await sql.CreateUserAsync($"coords-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync($"coords-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Coordinates fixture", null, 32648, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2000, 1, 1)));
            await db.SaveChangesAsync();
        }

        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, pm.UserName!);
        var drafts = $"/api/v1/projects/{project}/road-geometry-drafts";
        var input = new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            coordinates = new[] { new { x = 500000, y = 1100000 }, new { x = 500020, y = 1100000 } },
            stationOriginMeters = 100,
            changeReason = "Coordinates from fixture survey",
            widthProfile = new[] { new { fromOffsetMeters = 0, toOffsetMeters = 20, widthMeters = 8 } },
            surveyWidthMeters = 10,
            roadCode = "COORD"
        };
        using var invalid = await Send(client, HttpMethod.Post, drafts, input with { surveyWidthMeters = 7 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal("geometry_validation_failed", (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        using var wrongCrs = await Send(client, HttpMethod.Post, drafts, input with { sourceCrs = 32649 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongCrs.StatusCode);
        Assert.Equal("unsupported_crs", (await wrongCrs.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        using var saved = await Send(client, HttpMethod.Post, drafts, input);
        Assert.Equal(HttpStatusCode.Created, saved.StatusCode);
        var draftId = (await saved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var path = $"{drafts}/{draftId}";
        using var preview = await Send(client, HttpMethod.Post, path + "/preview", new { }, saved.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal(20, (await preview.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("lengthMeters").GetDouble());
        using var readiness = await client.GetAsync(path + "/readiness");
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
        var body = await readiness.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("ready").GetBoolean(), body.GetRawText());
        Assert.Equal("PROJECT_UTM", body.GetProperty("crsStatus").GetString());
        Assert.Empty(body.GetProperty("missingFields").EnumerateArray());
        Assert.Empty(body.GetProperty("errors").EnumerateArray());
        Assert.Equal(saved.Headers.ETag, readiness.Headers.ETag);
        var draft = await client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal(JsonValueKind.Null, draft.GetProperty("nativeAlignment").ValueKind);
        Assert.False(draft.TryGetProperty("routeSystemId", out _));

        await using (var db = sql.CreateDbContext())
        {
            // A previously saved draft can outlive a change to the project's CRS.
            var row = await db.Projects.SingleAsync(x => x.Id == project);
            row.EngineeringUtmSrid = 32649;
            await db.SaveChangesAsync();
            using var mismatched = await client.GetAsync(path + "/readiness");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, mismatched.StatusCode);
            Assert.Equal("unsupported_crs", (await mismatched.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            row.EngineeringUtmSrid = 32648;
            await db.SaveChangesAsync();
        }

        using var submitted = await GeometrySubmissionHttpTests.Submit(client, path, readiness.Headers.ETag!);
        await Login(client, supervisor.UserName!);
        using var confirmation = await Send(client, HttpMethod.Post, path + "/confirm",
            new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = DateTimeOffset.UtcNow, reason = "Confirm coordinate draft" },
            submitted.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.Created, confirmation.StatusCode);
        var routeId = (await confirmation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("routeVersionId").GetGuid();
        await using var persisted = sql.CreateDbContext();
        var route = await persisted.RoadSectionVersions.AsNoTracking().SingleAsync(x => x.Id == routeId);
        Assert.Equal(32648, route.Geometry.SRID);
        Assert.Equal(20, route.Geometry.Length);
    }

    [Fact]
    public async Task Candidate_profile_partial_native_arc_confirm_segment_publish_and_revoked_replay_are_real()
    {
        var pm = await sql.CreateUserAsync($"h2-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync($"h2-sup-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var project = Guid.NewGuid();
        await using (var seed = sql.CreateDbContext())
        {
            seed.Projects.Add(Project.Create(project, project.ToString(), "Native candidate fixture", null, null, null, null, DateTimeOffset.UtcNow));
            seed.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2000, 1, 1)));
            await seed.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, pm.UserName!);
        var root = $"/api/v1/projects/{project}";
        var profile = await Send(client, HttpMethod.Post, root + "/crs-profiles", new { code = "SAMPLE", sourceSrid = 0, datum = "CANDIDATE_SAMPLE_DATUM", projection = "CANDIDATE_SAMPLE_PROJECTION", axisOrder = "EN", metresPerUnit = 1, sourceReference = "sample-only fixture", sourceChecksum = new string('a', 64), sampleOnly = true });
        Assert.Equal(HttpStatusCode.Created, profile.StatusCode);
        var profileId = (await profile.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var system = await Send(client, HttpMethod.Post, root + "/route-systems", new { code = "SAMPLE_SYSTEM", name = "sample route system" });
        Assert.Equal(HttpStatusCode.Created, system.StatusCode);
        var systemId = (await system.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var partial = await Send(client, HttpMethod.Post, root + "/road-geometry-drafts", new { sourceKind = "NATIVE_ALIGNMENT", sourceCrs = 0, stationOriginMeters = 0, changeReason = "stepwise", widthProfile = Array.Empty<object>(), surveyWidthMeters = 0, roadCode = "NATIVE" });
        Assert.Equal(HttpStatusCode.Created, partial.StatusCode);
        var draftId = (await partial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var draftPath = root + $"/road-geometry-drafts/{draftId}";
        var readiness = await client.GetAsync(draftPath + "/readiness"); Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
        Assert.False((await readiness.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ready").GetBoolean());
        var length = 100 * Math.PI / 2;
        var complete = new { sourceKind = "NATIVE_ALIGNMENT", sourceCrs = 0, stationOriginMeters = 1000, changeReason = "native candidate", coordinates = (object?)null, widthProfile = new[] { new { fromOffsetMeters = 0, toOffsetMeters = length, widthMeters = 12 } }, surveyWidthMeters = 14, roadCode = "NATIVE", routeSystemId = systemId, routeKind = "MAIN", declaredLengthMeters = 170, tessellationToleranceMeters = .1, nativeAlignment = new { crsProfileRevisionId = profileId, spatialSrid = 0, primitives = new[] { new { kind = "ARC", start = new { x = 100, y = 0 }, end = new { x = 0, y = 100 }, center = new { x = 0, y = 0 }, radius = 100, startAngleRadians = 0, sweepRadians = Math.PI / 2 } } } };
        var edited = await Send(client, HttpMethod.Put, draftPath, complete, partial.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        var savedMain = await ReadNativeDraft(client, draftPath, edited.Headers.ETag!, profileId, systemId, "MAIN");
        Assert.Equal(170, savedMain.GetProperty("declaredLengthMeters").GetDouble());
        Assert.False(savedMain.TryGetProperty("parentRouteVersionId", out _));
        using var mainRevised = await Send(client, HttpMethod.Put, draftPath, complete with { changeReason = "Recheck MAIN field notes" }, edited.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, mainRevised.StatusCode);
        Assert.NotEqual(edited.Headers.ETag, mainRevised.Headers.ETag);
        var revisedMain = await ReadNativeDraft(client, draftPath, mainRevised.Headers.ETag!, profileId, systemId, "MAIN");
        Assert.Equal(170, revisedMain.GetProperty("declaredLengthMeters").GetDouble());
        using var mainStale = await Send(client, HttpMethod.Put, draftPath, complete with { changeReason = "Stale MAIN draft" }, edited.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, mainStale.StatusCode);
        var confirmInput = new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = DateTimeOffset.UtcNow, reason = "Supervisor confirms candidate engineering only" };
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Post, draftPath + "/confirm", confirmInput, mainRevised.Headers.ETag!.Tag)).StatusCode);
        using var mainSubmitted = await GeometrySubmissionHttpTests.Submit(client, draftPath, mainRevised.Headers.ETag!);
        await Login(client, supervisor.UserName!);
        var confirmed = await Send(client, HttpMethod.Post, draftPath + "/confirm", confirmInput, mainSubmitted.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        var route = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(length, route.GetProperty("lengthMeters").GetDouble(), 8); Assert.Equal(170, route.GetProperty("declaredLengthMeters").GetDouble()); Assert.True(route.GetProperty("sampleOnly").GetBoolean());
        var section = route.GetProperty("roadSectionId").GetGuid(); var version = route.GetProperty("routeVersionId").GetGuid();
        await Login(client, pm.UserName!);
        var setPath = root + $"/road-sections/{section}/versions/{version}/segment-sets";
        var set = await Send(client, HttpMethod.Post, setPath, new { targetLengthMeters = 100, remainderMode = "KEEP" }); Assert.Equal(HttpStatusCode.Created, set.StatusCode);
        var setJson = await set.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(2, setJson.GetProperty("segments").GetArrayLength());
        Assert.Equal(100, setJson.GetProperty("segments")[0].GetProperty("lengthMeters").GetDouble(), 8);
        var setId = setJson.GetProperty("id").GetGuid(); var key = Guid.NewGuid().ToString();
        var published = await Send(client, HttpMethod.Post, setPath + $"/{setId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "candidate publish" }, set.Headers.ETag!.Tag, key); Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        using (var producerScope = factory.Services.CreateScope())
        {
            var producer = producerScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.IAnhHuyProducerService>();
            var context = await producer.ResolveGeometryAsync(pm.Id, UserRoleCode.ProjectManager, project, version, setId);
            Assert.Equal(RoadGuardSystem.Services.Integration.AnhHuyProducerStatus.Ready, context.Status);
            Assert.Equal("anh-huy.geometry.v2", context.Facts!.SchemaVersion); Assert.Equal(profileId, context.Facts.CrsProfileRevisionId); Assert.True(context.Facts.SampleOnly); Assert.Equal(length, context.Facts.CanonicalLengthMeters);
        }
        await GeometrySubmissionHttpTests.MarkAsPreMigrationConfirmedFixture(sql, draftId);
        // Branch stationing is calibrated independently of geometric length; its parent is explicitly pinned.
        var branchInput = new { sourceKind = "NATIVE_ALIGNMENT", sourceCrs = 0, stationOriginMeters = 0, changeReason = "branch fixture", widthProfile = new[] { new { fromOffsetMeters = 0, toOffsetMeters = 50, widthMeters = 12 } }, surveyWidthMeters = 14, roadCode = "BRANCH", routeSystemId = systemId, routeKind = "BRANCH", parentRouteVersionId = version, junctionOffsetMeters = 0, tessellationToleranceMeters = .1, chainageCalibration = new { sourceReference = "sample-only controls", sourceChecksum = new string('b', 64), selectedReason = "PM fixture", controls = new[] { new { geometricOffsetMeters = 0, stationMeters = 500 }, new { geometricOffsetMeters = 50, stationMeters = 600 } } }, nativeAlignment = new { crsProfileRevisionId = profileId, spatialSrid = 0, primitives = new[] { new { kind = "LINE", start = new { x = 100, y = 0 }, end = new { x = 150, y = 0 } } } } };
        var branch = await Send(client, HttpMethod.Post, root + "/road-geometry-drafts", branchInput); Assert.Equal(HttpStatusCode.Created, branch.StatusCode);
        var branchDraft = (await branch.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var branchPath = root + $"/road-geometry-drafts/{branchDraft}";
        var savedBranch = await ReadNativeDraft(client, branchPath, branch.Headers.ETag!, profileId, systemId, "BRANCH");
        AssertBranchMetadata(savedBranch, version);
        using var branchRevised = await Send(client, HttpMethod.Put, branchPath, branchInput with { changeReason = "Recheck branch controls" }, branch.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, branchRevised.StatusCode);
        Assert.NotEqual(branch.Headers.ETag, branchRevised.Headers.ETag);
        var revisedBranch = await ReadNativeDraft(client, branchPath, branchRevised.Headers.ETag!, profileId, systemId, "BRANCH");
        AssertBranchMetadata(revisedBranch, version);
        using var branchStale = await Send(client, HttpMethod.Put, branchPath, branchInput with { changeReason = "Stale branch draft" }, branch.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, branchStale.StatusCode);
        using var branchSubmitted = await GeometrySubmissionHttpTests.Submit(client, branchPath, branchRevised.Headers.ETag!);
        await Login(client, supervisor.UserName!);
        var branchConfirmed = await Send(client, HttpMethod.Post, branchPath + "/confirm", confirmInput, branchSubmitted.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.Created, branchConfirmed.StatusCode);
        var branchRoute = await branchConfirmed.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(version, branchRoute.GetProperty("parentRouteVersionId").GetGuid()); Assert.Equal(50, branchRoute.GetProperty("lengthMeters").GetDouble());
        await Login(client, pm.UserName!);
        var branchSet = await Send(client, HttpMethod.Post, root + $"/road-sections/{branchRoute.GetProperty("roadSectionId").GetGuid()}/versions/{branchRoute.GetProperty("routeVersionId").GetGuid()}/segment-sets", new { targetLengthMeters = 100, remainderMode = "KEEP" }); Assert.Equal(HttpStatusCode.Created, branchSet.StatusCode);
        var calibrated = (await branchSet.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("segments")[0]; Assert.Equal(500, calibrated.GetProperty("startStationMeters").GetDouble()); Assert.Equal(600, calibrated.GetProperty("endStationMeters").GetDouble());
        await GeometrySubmissionHttpTests.MarkAsPreMigrationConfirmedFixture(sql, branchDraft);
        var cycleDraft = await Send(client, HttpMethod.Post, root + $"/road-sections/{section}/geometry-drafts", branchInput); Assert.Equal(HttpStatusCode.Created, cycleDraft.StatusCode);
        var cycleDraftId = (await cycleDraft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var cycleSubmit = await GeometrySubmissionHttpTests.Send(client, HttpMethod.Post,
            root + $"/road-geometry-drafts/{cycleDraftId}/submit", etag: cycleDraft.Headers.ETag);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, cycleSubmit.StatusCode);
        var correction = await Send(client, HttpMethod.Post, root + $"/road-sections/{section}/geometry-drafts", complete); Assert.Equal(HttpStatusCode.Created, correction.StatusCode);
        var correctionId = (await correction.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var correctionSubmitted = await GeometrySubmissionHttpTests.Submit(client,
            root + $"/road-geometry-drafts/{correctionId}", correction.Headers.ETag!);
        await Login(client, supervisor.UserName!);
        var corrected = await Send(client, HttpMethod.Post, root + $"/road-geometry-drafts/{correctionId}/confirm", new { expectedCurrentVersionId = version, effectiveFrom = DateTimeOffset.UtcNow, reason = "preserve earlier pins and record impact" }, correctionSubmitted.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.Created, corrected.StatusCode);
        await using (var inspect = sql.CreateDbContext())
        {
            var impact = await inspect.Set<GeometryLocationImpact>().AsNoTracking().SingleAsync(x => x.PreviousRouteVersionId == version);
            var references = JsonSerializer.Deserialize<RoadGuardSystem.DTOs.Projects.GeometryAffectedReference[]>(impact.AffectedReferencesJson)!;
            Assert.Contains(references, x => x.Kind == "BRANCH" && x.Id == branchRoute.GetProperty("routeVersionId").GetGuid() && x.RouteVersionId == version);
            Assert.Equal(version, (await inspect.Set<NativeRouteVersionFacts>().AsNoTracking().SingleAsync(x => x.RoadSectionVersionId == branchRoute.GetProperty("routeVersionId").GetGuid())).ParentRouteVersionId);
        }
        await Login(client, pm.UserName!);
        await using (var revoke = sql.CreateDbContext()) { await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={project} AND UserId={pm.Id}"); }
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Post, setPath + $"/{setId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "candidate publish" }, set.Headers.ETag!.Tag, key)).StatusCode);
    }

    private static async Task<JsonElement> ReadNativeDraft(HttpClient client, string path, EntityTagHeaderValue version,
        Guid profileId, Guid systemId, string kind)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(version, response.Headers.ETag);
        var draft = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NATIVE_ALIGNMENT", draft.GetProperty("sourceKind").GetString());
        Assert.Equal(kind, draft.GetProperty("routeKind").GetString());
        Assert.Equal(systemId, draft.GetProperty("routeSystemId").GetGuid());
        Assert.Equal(.1, draft.GetProperty("tessellationToleranceMeters").GetDouble());
        Assert.Equal(profileId, draft.GetProperty("nativeAlignment").GetProperty("crsProfileRevisionId").GetGuid());
        Assert.False(draft.TryGetProperty("resolvedCrsProfile", out _));
        return draft;
    }

    private static void AssertBranchMetadata(JsonElement draft, Guid parentRouteVersionId)
    {
        Assert.Equal(parentRouteVersionId, draft.GetProperty("parentRouteVersionId").GetGuid());
        Assert.Equal(0, draft.GetProperty("junctionOffsetMeters").GetDouble());
        var calibration = draft.GetProperty("chainageCalibration");
        Assert.Equal("sample-only controls", calibration.GetProperty("sourceReference").GetString());
        Assert.Equal(new string('b', 64), calibration.GetProperty("sourceChecksum").GetString());
        Assert.Equal("PM fixture", calibration.GetProperty("selectedReason").GetString());
        var controls = calibration.GetProperty("controls");
        Assert.Equal(2, controls.GetArrayLength());
        Assert.Equal(0, controls[0].GetProperty("geometricOffsetMeters").GetDouble());
        Assert.Equal(500, controls[0].GetProperty("stationMeters").GetDouble());
        Assert.Equal(50, controls[1].GetProperty("geometricOffsetMeters").GetDouble());
        Assert.Equal(600, controls[1].GetProperty("stationMeters").GetDouble());
    }

    private static async Task Login(HttpClient client, string user)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(user), password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, object body, string? etag = null, string? key = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) }; request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString()); if (etag is not null) request.Headers.Add("If-Match", etag); return client.SendAsync(request);
    }
}
