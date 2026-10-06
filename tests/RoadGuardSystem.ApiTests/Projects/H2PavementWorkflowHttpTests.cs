using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H2PavementWorkflowHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task Production_plan_asbuilt_manifest_page_and_revoked_receipts_are_real()
    {
        var pm = await sql.CreateUserAsync($"pavement-http-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var scope = await Seed(pm.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") }); await Login(client, pm.UserName!);
        var root = $"/api/v1/projects/{scope.Project}";
        var path = root + $"/route-versions/{scope.Route}/segment-sets/{scope.Set}/pavement-plans";
        var input = new { layout = new { stripWidthMeters = 4, slabLengthMeters = 10, widthProfile = new[] { new { fromOffsetMeters = 0, toOffsetMeters = 20, widthMeters = 12 } } }, displayToleranceMeters = .1 };
        var key = Guid.NewGuid().ToString(); var plan = await Send(client, path, input, key);
        Assert.Equal(HttpStatusCode.Created, plan.StatusCode); var layout = await plan.Content.ReadFromJsonAsync<JsonElement>();
        var layoutId = layout.GetProperty("id").GetGuid(); var hash = layout.GetProperty("contentHash").GetString()!;
        Assert.Equal($"\"{hash}\"",plan.Headers.ETag!.Tag); Assert.EndsWith($"/pavement-layouts/{layoutId}",plan.Headers.Location!.ToString());
        var replay=await Send(client,path,input,key); Assert.Equal(HttpStatusCode.OK,replay.StatusCode);
        Assert.Equal(layout.GetRawText(),(await replay.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        Assert.Equal(6, layout.GetProperty("geometry").GetProperty("slabs").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(root + $"/pavement-layouts/{layoutId}")).StatusCode);
        var publication = await Send(client, root + "/geometry-map-publications", new { layoutRevisionId = layoutId, publicationMode = "SAMPLE", reason = "native sample fixture" }, hash: hash);
        Assert.Equal(HttpStatusCode.Created, publication.StatusCode); var manifest = await publication.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(manifest.GetProperty("sampleOnly").GetBoolean()); var id = manifest.GetProperty("publicationId").GetGuid(); var mapHash = manifest.GetProperty("contentHash").GetString();
        Assert.Equal($"\"{mapHash}\"",publication.Headers.ETag!.Tag); Assert.EndsWith($"/geometry-map-publications/{id}",publication.Headers.Location!.ToString());
        var pagePath = root + $"/geometry-map-publications/{id}/layers/slabs?limit=2&expectedContentHash={mapHash}";
        var page = await client.GetAsync(pagePath); Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var mixedHeader=new HttpRequestMessage(HttpMethod.Get,pagePath); mixedHeader.Headers.Add("If-Match",$"\"{new string('a',64)}\"");
        Assert.Equal(HttpStatusCode.Conflict,(await client.SendAsync(mixedHeader)).StatusCode);
        var pageJson = await page.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(2, pageJson.GetProperty("features").GetArrayLength()); Assert.Equal(JsonValueKind.String, pageJson.GetProperty("nextCursor").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(pagePath + $"&routeVersionId={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await client.GetAsync(root + $"/geometry-map-publications/{id}/layers/slabs")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, root + "/geometry-map-publications", new { layoutRevisionId = layoutId, publicationMode = "OFFICIAL", reason = "no accepted CRS" }, hash: hash)).StatusCode);
        var built = await Send(client, root + "/pavement-layouts/as-built", new { sourcePlanId = layoutId, reason = "custom unknown dimension", slabs = new[] { new { key = "custom", plannedSequence = 1, footprint = new[] { new { x = 0, y = 0 }, new { x = 5, y = 0 }, new { x = 5, y = 4 }, new { x = 0, y = 4 }, new { x = 0, y = 0 } }, lengthMeters = (double?)null, widthMeters = 4, dimensionUnknownReason = "not measured", source = "observed field polygon" } } }, hash: hash);
        Assert.Equal(HttpStatusCode.Created, built.StatusCode);
        await using (var revoke = sql.CreateDbContext()) { await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={scope.Project} AND UserId={pm.Id}"); }
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, path, input, key)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(root + $"/geometry-map-publications/{id}")).StatusCode);
    }
    [Fact]
    public async Task Crew_wrong_project_invalid_strips_and_duplicate_headers_are_denied()
    {
        var pm = await sql.CreateUserAsync($"pavement-neg-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var crew = await sql.CreateUserAsync($"pavement-crew-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var unscoped = await sql.CreateUserAsync($"pavement-unscoped-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var scope = await Seed(pm.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") }); await Login(client, pm.UserName!);
        var path = $"/api/v1/projects/{scope.Project}/route-versions/{scope.Route}/segment-sets/{scope.Set}/pavement-plans";
        var input = new { layout = new { stripWidthMeters = 5, slabLengthMeters = 10, widthProfile = new[] { new { fromOffsetMeters = 0, toOffsetMeters = 20, widthMeters = 12 } } }, displayToleranceMeters = .1 };
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send(client, path, input)).StatusCode);
        var duplicate = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        duplicate.Headers.Add("Idempotency-Key", new[] { "one", "two" }); Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(duplicate)).StatusCode);
        await Login(client,unscoped.UserName!); Assert.Equal(HttpStatusCode.Forbidden,(await Send(client,path,input)).StatusCode);
        await Login(client, crew.UserName!); Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, path, input)).StatusCode);
        await using var check = sql.CreateDbContext(); Assert.Equal(0, await check.Set<PavementLayoutRevision>().CountAsync(x => x.ProjectId == scope.Project));
    }
    [Fact]
    public async Task Confirmed_native_candidate_produces_scoped_road_survey_strips_and_exact_manifest_pins()
    {
        var pm=await sql.CreateUserAsync($"native-map-{Guid.NewGuid():N}","Current1!",UserRoleCode.ProjectManager);
        var supervisor=await sql.CreateUserAsync($"native-map-sup-{Guid.NewGuid():N}","Current1!",UserRoleCode.Supervisor);
        var scope=await Seed(pm.Id); await using var factory=new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client=factory.CreateClient(new(){BaseAddress=new("https://localhost")}); await Login(client,pm.UserName!);
        var root=$"/api/v1/projects/{scope.Project}";
        var profile=await Send(client,root+"/crs-profiles",new{code="SAMPLE",sourceSrid=0,datum="CANDIDATE_SAMPLE_DATUM",projection="CANDIDATE_SAMPLE_PROJECTION",axisOrder="EN",metresPerUnit=1,sourceReference="sample only",sourceChecksum=new string('a',64),sampleOnly=true}); Assert.Equal(HttpStatusCode.Created,profile.StatusCode);
        var profileId=(await profile.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var system=await Send(client,root+"/route-systems",new{code="SAMPLE",name="sample road"}); Assert.Equal(HttpStatusCode.Created,system.StatusCode);
        var systemId=(await system.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var draft=await Send(client,root+"/road-geometry-drafts",new{sourceKind="NATIVE_ALIGNMENT",sourceCrs=0,stationOriginMeters=1000,changeReason="native sample",widthProfile=new[]{new{fromOffsetMeters=0,toOffsetMeters=20,widthMeters=12}},surveyWidthMeters=14,roadCode="NATIVE_MAP",routeSystemId=systemId,routeKind="MAIN",declaredLengthMeters=25,tessellationToleranceMeters=.1,nativeAlignment=new{crsProfileRevisionId=profileId,spatialSrid=0,primitives=new[]{new{kind="LINE",start=new{x=0,y=0},end=new{x=20,y=0}}}}}); Assert.Equal(HttpStatusCode.Created,draft.StatusCode);
        var draftId=(await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid(); await Login(client,supervisor.UserName!);
        var confirmed=await Send(client,root+$"/road-geometry-drafts/{draftId}/confirm",new{expectedCurrentVersionId=(Guid?)null,effectiveFrom=DateTimeOffset.UtcNow,reason="candidate engineering"},hash:draft.Headers.ETag!.Tag.Trim('"')); Assert.Equal(HttpStatusCode.Created,confirmed.StatusCode);
        var confirmedJson=await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        var route=confirmedJson.GetProperty("routeVersionId").GetGuid(); var section=confirmedJson.GetProperty("roadSectionId").GetGuid(); await Login(client,pm.UserName!);
        var setPath=root+$"/road-sections/{section}/versions/{route}/segment-sets";
        var set=await Send(client,setPath,new{targetLengthMeters=10,remainderMode="KEEP"}); Assert.Equal(HttpStatusCode.Created,set.StatusCode);
        var setId=(await set.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK,(await Send(client,setPath+$"/{setId}/publish",new{expectedPublishedSetId=(Guid?)null,reason="sample"},hash:set.Headers.ETag!.Tag.Trim('"'))).StatusCode);
        var plan=await Send(client,root+$"/route-versions/{route}/segment-sets/{setId}/pavement-plans",new{layout=new{stripWidthMeters=4,slabLengthMeters=10,widthProfile=new[]{new{fromOffsetMeters=0,toOffsetMeters=20,widthMeters=12}}},displayToleranceMeters=.1}); Assert.Equal(HttpStatusCode.Created,plan.StatusCode);
        var planJson=await plan.Content.ReadFromJsonAsync<JsonElement>();
        var published=await Send(client,root+"/geometry-map-publications",new{layoutRevisionId=planJson.GetProperty("id").GetGuid(),publicationMode="SAMPLE",reason="native layers"},hash:planJson.GetProperty("contentHash").GetString()); Assert.Equal(HttpStatusCode.Created,published.StatusCode);
        var manifest=await published.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(route,manifest.GetProperty("routeVersionId").GetGuid()); Assert.Equal(setId,manifest.GetProperty("segmentSetId").GetGuid()); Assert.Equal(profileId,manifest.GetProperty("crsProfileRevisionId").GetGuid()); Assert.Equal(systemId,manifest.GetProperty("routeSystemId").GetGuid());
        Assert.Equal(20,manifest.GetProperty("canonicalLengthMeters").GetDouble()); Assert.Equal(25,manifest.GetProperty("declaredLengthMeters").GetDouble());
        foreach(var layer in new[]{"roadSurface","surveyArea","strips","plannedSlabs"})
        {
            var descriptor=manifest.GetProperty("layers").EnumerateArray().Single(x=>x.GetProperty("key").GetString()==layer); Assert.Equal("READY",descriptor.GetProperty("status").GetString()); Assert.Equal(0,descriptor.GetProperty("spatialSrid").GetInt32());
            var page=await client.GetAsync(root+$"/geometry-map-publications/{manifest.GetProperty("publicationId").GetGuid()}/layers/{layer}?expectedContentHash={manifest.GetProperty("contentHash").GetString()}"); Assert.Equal(HttpStatusCode.OK,page.StatusCode);
            Assert.Equal(descriptor.GetProperty("count").GetInt32(),(await page.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("features").GetArrayLength());
        }
    }
    private async Task<(Guid Project, Guid Route, Guid Set)> Seed(Guid pm)
    {
        await using var db = sql.CreateDbContext();
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "pavement HTTP sample", null, null, null, null, DateTimeOffset.UtcNow);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "sample"); var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new(0,0),new(20,0)]);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, DateTimeOffset.UtcNow, "legacy source"); var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id);
        var segment = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 1); segment.SetGeometry(0,20,0,geometry);
        db.AddRange(project, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm, new(2000,1,1)), road, route, set, segment); await db.SaveChangesAsync();
        return (project.Id, route.Id, set.Id);
    }
    private static async Task Login(HttpClient client, string user)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(user), password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, string path, object body, string? key = null, string? hash = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) }; request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        if (hash is not null) request.Headers.Add("If-Match", $"\"{hash}\""); return client.SendAsync(request);
    }
}
