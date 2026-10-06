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
        var confirmInput = new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = DateTimeOffset.UtcNow, reason = "Supervisor confirms candidate engineering only" };
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Post, draftPath + "/confirm", confirmInput, edited.Headers.ETag!.Tag)).StatusCode);
        await Login(client, supervisor.UserName!);
        var confirmed = await Send(client, HttpMethod.Post, draftPath + "/confirm", confirmInput, edited.Headers.ETag!.Tag); Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
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
        using(var producerScope=factory.Services.CreateScope())
        {
            var producer=producerScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.IAnhHuyProducerService>();
            var context=await producer.ResolveGeometryAsync(pm.Id,UserRoleCode.ProjectManager,project,version,setId);
            Assert.Equal(RoadGuardSystem.Services.Integration.AnhHuyProducerStatus.Ready,context.Status);
            Assert.Equal("anh-huy.geometry.v2",context.Facts!.SchemaVersion);Assert.Equal(profileId,context.Facts.CrsProfileRevisionId);Assert.True(context.Facts.SampleOnly);Assert.Equal(length,context.Facts.CanonicalLengthMeters);
        }
        // Branch stationing is calibrated independently of geometric length; its parent is explicitly pinned.
        var branchInput=new {sourceKind="NATIVE_ALIGNMENT",sourceCrs=0,stationOriginMeters=0,changeReason="branch fixture",widthProfile=new[]{new{fromOffsetMeters=0,toOffsetMeters=50,widthMeters=12}},surveyWidthMeters=14,roadCode="BRANCH",routeSystemId=systemId,routeKind="BRANCH",parentRouteVersionId=version,junctionOffsetMeters=0,tessellationToleranceMeters=.1,chainageCalibration=new{sourceReference="sample-only controls",sourceChecksum=new string('b',64),selectedReason="PM fixture",controls=new[]{new{geometricOffsetMeters=0,stationMeters=500},new{geometricOffsetMeters=50,stationMeters=600}}},nativeAlignment=new{crsProfileRevisionId=profileId,spatialSrid=0,primitives=new[]{new{kind="LINE",start=new{x=100,y=0},end=new{x=150,y=0}}}}};
        var branch=await Send(client,HttpMethod.Post,root+"/road-geometry-drafts",branchInput);Assert.Equal(HttpStatusCode.Created,branch.StatusCode);
        var branchDraft=(await branch.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Login(client,supervisor.UserName!);
        var branchConfirmed=await Send(client,HttpMethod.Post,root+$"/road-geometry-drafts/{branchDraft}/confirm",confirmInput,branch.Headers.ETag!.Tag);Assert.Equal(HttpStatusCode.Created,branchConfirmed.StatusCode);
        var branchRoute=await branchConfirmed.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(version,branchRoute.GetProperty("parentRouteVersionId").GetGuid());Assert.Equal(50,branchRoute.GetProperty("lengthMeters").GetDouble());
        await Login(client,pm.UserName!);
        var branchSet=await Send(client,HttpMethod.Post,root+$"/road-sections/{branchRoute.GetProperty("roadSectionId").GetGuid()}/versions/{branchRoute.GetProperty("routeVersionId").GetGuid()}/segment-sets",new{targetLengthMeters=100,remainderMode="KEEP"});Assert.Equal(HttpStatusCode.Created,branchSet.StatusCode);
        var calibrated=(await branchSet.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("segments")[0];Assert.Equal(500,calibrated.GetProperty("startStationMeters").GetDouble());Assert.Equal(600,calibrated.GetProperty("endStationMeters").GetDouble());
        var cycleDraft=await Send(client,HttpMethod.Post,root+$"/road-sections/{section}/geometry-drafts",branchInput);Assert.Equal(HttpStatusCode.Created,cycleDraft.StatusCode);
        var cycleDraftId=(await cycleDraft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Login(client,supervisor.UserName!);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,(await Send(client,HttpMethod.Post,root+$"/road-geometry-drafts/{cycleDraftId}/confirm",new{expectedCurrentVersionId=version,effectiveFrom=DateTimeOffset.UtcNow,reason="must reject road identity cycle"},cycleDraft.Headers.ETag!.Tag)).StatusCode);
        await Login(client,pm.UserName!);
        var correction=await Send(client,HttpMethod.Post,root+$"/road-sections/{section}/geometry-drafts",complete);Assert.Equal(HttpStatusCode.Created,correction.StatusCode);
        var correctionId=(await correction.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Login(client,supervisor.UserName!);
        var corrected=await Send(client,HttpMethod.Post,root+$"/road-geometry-drafts/{correctionId}/confirm",new{expectedCurrentVersionId=version,effectiveFrom=DateTimeOffset.UtcNow,reason="preserve earlier pins and record impact"},correction.Headers.ETag!.Tag);Assert.Equal(HttpStatusCode.Created,corrected.StatusCode);
        await using(var inspect=sql.CreateDbContext())
        {
            var impact=await inspect.Set<GeometryLocationImpact>().AsNoTracking().SingleAsync(x=>x.PreviousRouteVersionId==version);
            var references=JsonSerializer.Deserialize<RoadGuardSystem.DTOs.Projects.GeometryAffectedReference[]>(impact.AffectedReferencesJson)!;
            Assert.Contains(references,x=>x.Kind=="BRANCH" && x.Id==branchRoute.GetProperty("routeVersionId").GetGuid() && x.RouteVersionId==version);
            Assert.Equal(version,(await inspect.Set<NativeRouteVersionFacts>().AsNoTracking().SingleAsync(x=>x.RoadSectionVersionId==branchRoute.GetProperty("routeVersionId").GetGuid())).ParentRouteVersionId);
        }
        await Login(client,pm.UserName!);
        await using (var revoke = sql.CreateDbContext()) { await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={project} AND UserId={pm.Id}"); }
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, HttpMethod.Post, setPath + $"/{setId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "candidate publish" }, set.Headers.ETag!.Tag, key)).StatusCode);
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
