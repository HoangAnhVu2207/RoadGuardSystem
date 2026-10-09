using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H6ProjectLifecycleHttpTests(AuthenticationSqlServerFixture fixture, ITestOutputHelper output)
{
    [Theory]
    [InlineData("renewed-handling-scope")]
    [InlineData("construction-declarations")]
    [InlineData("construction-confirmations")]
    [InlineData("defect-closures")]
    [InlineData("operational-closures")]
    [InlineData("recurrences")]
    [InlineData("obligation-transfers")]
    [InlineData("obligation-transfer-acceptances")]
    public async Task CookieLifecycleWritesRequireCsrfBeforeAnyHistoryOrReceipt(string action)
    {
        var actor = await fixture.CreateUserAsync("lifecycle-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"\"{new string('a', 64)}\"");
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/lifecycle/{action}",
            new RenewedHandlingScopeInput(Guid.NewGuid(), "renew", "scope", "basis"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext(); Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
        Assert.False(await db.Set<ProjectLifecycleHistoryRecord>().AnyAsync(row => row.ActorId == actor.Id));
    }
    [Fact]
    public async Task RenewedScopeRequiresPreconditionsBeforeAnyLifecycleEffect()
    {
        var actor = await fixture.CreateUserAsync("lifecycle-http-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString); using var client = factory.CreateClient();
        Assert.NotNull(actor.Email); await Login(client, actor.Email);
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/lifecycle/renewed-handling-scope",
            new RenewedHandlingScopeInput(Guid.NewGuid(), "renewed scope", "bounded handling", "basis"));
        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        await using var db = fixture.CreateDbContext(); Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
        Assert.False(await db.Set<ProjectLifecycleHistoryRecord>().AnyAsync(row => row.ActorId == actor.Id));
    }

    [Fact]
    public async Task CurrentPrivateLifecycleReadAndCandidateSourceDenialUseActualHttp()
    {
        var actor = await fixture.CreateUserAsync("lifecycle-source-http-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Candidate lifecycle HTTP source", null, null, null, null, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = actor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        var closure = ProjectLifecycleHistoryRecord.RecordCandidate(Guid.NewGuid(), project.Id, ProjectLifecycleFactKind.OperationalClosure,
            actor.Id, now, "candidate source", "TEST_ONLY no generic close authority", "{}");
        db.AddRange(project, member, closure); await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString); using var client = factory.CreateClient();
        Assert.NotNull(actor.Email); await Login(client, actor.Email); var path = $"/api/v1/projects/{project.Id}/lifecycle";
        var read = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var view = await read.Content.ReadFromJsonAsync<ProjectLifecycleViewDto>(); Assert.NotNull(view);
        Assert.Equal("UNKNOWN", view.OperationalClosure); Assert.Equal("UNKNOWN", view.ObligationInventory); Assert.True(view.AcceptsNewReports);
        Assert.NotNull(read.Headers.ETag); Assert.Equal($"\"{view.Version}\"", read.Headers.ETag.ToString());
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", read.Headers.ETag.ToString());
        var denial = await client.PostAsJsonAsync(path + "/renewed-handling-scope", new RenewedHandlingScopeInput(closure.Id, "renewed", "scope", "candidate basis"));
        Assert.Equal(HttpStatusCode.Conflict, denial.StatusCode);
        Assert.Equal("operational_closure_source_unavailable", (await denial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await db.ProjectMembers.Where(row => row.Id == member.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
        Assert.Single(await db.Set<ProjectLifecycleHistoryRecord>().Where(row => row.ProjectId == project.Id).ToArrayAsync());
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }

    [Fact]
    public async Task ConfirmedOperationalClosureProducesRenewedHandlingThroughActualHttp()
    {
        var actor = await fixture.CreateUserAsync("lifecycle-renew-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var receiverActor = await fixture.CreateUserAsync("lifecycle-receiver-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "TEST_ONLY completed inventory", null, null, null, null, now);
        var receiver = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "TEST_ONLY receiving inventory", null, null, null, null, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = actor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        var receiverMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = receiver.Id,
            UserId = receiverActor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "TEST_ONLY physical road");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "TEST_ONLY source version");
        var type = DefectType.Create("LC" + Guid.NewGuid().ToString("N"), "TEST_ONLY");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null,
            DefectSeverity.Low, DefectStatus.Open, geometry.CreatePoint(new Coordinate(5, 0)), now);
        db.AddRange(project, receiver, member, receiverMember, road, route, type, defect); await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), road.Id, "TEST_ONLY_SOURCE_V1", "TEST_ONLY", 0, 10, -1, 1));
        db.Add(RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation])); await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(); using var receiverClient = factory.CreateClient();
        await Login(client, actor.Email!); await Login(receiverClient, receiverActor.Email!);
        var path = $"/api/v1/projects/{project.Id}/lifecycle";
        var initial = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var scope = Assert.Single((await initial.Content.ReadFromJsonAsync<ProjectLifecycleViewDto>())!.TransferableObligations!);
        Assert.Equal(obligation.Id, scope.Id);
        var issue = new HttpRequestMessage(HttpMethod.Post, path + "/obligation-transfers")
        {
            Content = JsonContent.Create(new LD06LifecycleInputDto("TEST_ONLY exact scope transfer", [],
            ObligationId: obligation.Id, ReceivingProjectId: receiver.Id, ScopeHash: scope.ScopeHash))
        };
        issue.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        issue.Headers.TryAddWithoutValidation("If-Match", initial.Headers.ETag!.ToString());
        var issued = await client.SendAsync(issue); Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
        var issueId = await db.Set<LD06LifecycleAction>().AsNoTracking().Where(row => row.ProjectId == project.Id &&
            row.Kind == LD06ActionKind.IssueTransfer).Select(row => row.Id).SingleAsync();
        var receiverPath = $"/api/v1/projects/{receiver.Id}/lifecycle";
        var receiverRead = await receiverClient.GetAsync(receiverPath); Assert.Equal(HttpStatusCode.OK, receiverRead.StatusCode);
        var accept = new HttpRequestMessage(HttpMethod.Post, receiverPath + "/obligation-transfer-acceptances")
        {
            Content = JsonContent.Create(new LD06LifecycleInputDto("TEST_ONLY receive exact scope", [], issueId,
            ObligationId: obligation.Id, ScopeHash: scope.ScopeHash))
        };
        accept.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        accept.Headers.TryAddWithoutValidation("If-Match", receiverRead.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Created, (await receiverClient.SendAsync(accept)).StatusCode);
        var close = new HttpRequestMessage(HttpMethod.Post, path + "/operational-closures")
        { Content = JsonContent.Create(new { reason = "TEST_ONLY transferred mandatory obligation", evidenceFileIds = Array.Empty<Guid>() }) };
        close.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        close.Headers.TryAddWithoutValidation("If-Match", (await client.GetAsync(path)).Headers.ETag!.ToString());
        var closed = await client.SendAsync(close); Assert.True(closed.StatusCode == HttpStatusCode.Created, await closed.Content.ReadAsStringAsync());
        var closedView = await closed.Content.ReadFromJsonAsync<ProjectLifecycleViewDto>(); Assert.NotNull(closedView);
        Assert.Equal("CONFIRMED", closedView.OperationalClosure);
        var source = await db.Set<LD06LifecycleAction>().AsNoTracking()
            .SingleAsync(row => row.ProjectId == project.Id && row.Kind == LD06ActionKind.OperationalClose);
        var renew = new HttpRequestMessage(HttpMethod.Post, path + "/renewed-handling-scope")
        { Content = JsonContent.Create(new RenewedHandlingScopeInput(source.Id, "TEST_ONLY renewed intake", "bounded new handling", "confirmed operational closure")) };
        renew.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        renew.Headers.TryAddWithoutValidation("If-Match", closed.Headers.ETag!.ToString());
        var renewed = await client.SendAsync(renew); Assert.Equal(HttpStatusCode.Created, renewed.StatusCode);
        var view = await renewed.Content.ReadFromJsonAsync<ProjectLifecycleViewDto>(); Assert.NotNull(view);
        Assert.Contains(view.History, row => row.OperationalClosureId == source.Id && row.Kind == "RenewedHandlingScope");
        Assert.True(view.AcceptsNewReports);
        Assert.Single(await db.Set<ProjectLifecycleHistoryRecord>().Where(row => row.ProjectId == project.Id &&
            row.Kind == ProjectLifecycleFactKind.RenewedHandlingScope).ToArrayAsync());
        Assert.Equal(receiver.Id, (await db.Set<ObligationResponsibility>().AsNoTracking()
            .SingleAsync(row => row.ObligationId == obligation.Id)).CurrentProjectId);
        output.WriteLine(JsonSerializer.Serialize(new
        {
            proof = "SWG-130",
            environment = "TEST_HOST_E2E",
            status = 201,
            actorId = actor.Id,
            role = "Supervisor",
            projectId = project.Id,
            receivingProjectId = receiver.Id,
            defectId = defect.Id,
            obligationId = obligation.Id,
            transferIssueId = issueId,
            operationalClosureId = source.Id,
            producer = "LD06 issue/accept transfer and operational closure HTTP",
            request = new RenewedHandlingScopeInput(source.Id, "TEST_ONLY renewed intake", "bounded new handling", "confirmed operational closure"),
            persistedRenewedHistory = true,
            persistedReceivingProject = receiver.Id
        }));
    }

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
}
