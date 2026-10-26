using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Messaging;
using Xunit;

namespace RoadGuardSystem.ApiTests.Notifications;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H6NotificationOperationsHttpTests(AuthenticationSqlServerFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task CurrentOwnerReadsProtectedScopeAndClockPageWhileOutsiderAndRevokedMemberAreDenied()
    {
        var owner = await fixture.CreateUserAsync("h6-ops-owner-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var outsider = await fixture.CreateUserAsync("h6-ops-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Notification scope", null, null, null, null, now);
        var membership = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = owner.Id,
            RoleCode = UserRoleCode.ProjectManager,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1),
            IsPrimary = true
        };
        var notification = Notification.Create(Guid.NewGuid(), owner.Id, "Project", project.Id,
            "PROJECT_SCOPE", "Current project", "Current project notification", now);
        var first = DeadlineClock.Create(Guid.NewGuid(), project.Id, DeadlineClockKind.ProjectManagerReview,
            Guid.NewGuid(), Guid.NewGuid(), now.AddHours(-2));
        var second = DeadlineClock.Create(Guid.NewGuid(), project.Id, DeadlineClockKind.ProjectManagerReview,
            Guid.NewGuid(), Guid.NewGuid(), now.AddHours(-1));
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(project, membership, notification, first, second);
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var ownerClient = factory.CreateClient(); using var outsiderClient = factory.CreateClient();
        Assert.NotNull(owner.Email); Assert.NotNull(outsider.Email);
        await Login(ownerClient, owner.Email); await Login(outsiderClient, outsider.Email);
        var scopePath = $"/api/v1/notifications/{notification.Id}/scope";
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(scopePath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/v1/notifications/{Guid.NewGuid()}/scope")).StatusCode);
        var scopeResponse = await ownerClient.GetAsync(scopePath);
        Assert.Equal(HttpStatusCode.OK, scopeResponse.StatusCode);
        var scope = await scopeResponse.Content.ReadFromJsonAsync<H6NotificationScopeDto>();
        Assert.NotNull(scope); Assert.Equal(project.Id, scope.ProjectId); Assert.Equal("PROJECT", scope.Classification);
        Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.GetAsync(scopePath)).StatusCode);
        var clocksPath = $"/api/v1/projects/{project.Id}/clocks";
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(clocksPath)).StatusCode);
        foreach (var suffix in new[] { "?limit=0", "?limit=101", "?cursor=not-json", "?cursor=" + new string('x', 1025) })
        {
            var invalid = await ownerClient.GetAsync(clocksPath + suffix);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal("validation_error", (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        var page = await (await ownerClient.GetAsync(clocksPath + "?limit=1")).Content.ReadFromJsonAsync<H6ClockPage>();
        Assert.NotNull(page); Assert.Equal("READY", page.Status); Assert.Single(page.Items); Assert.NotNull(page.Continuation);
        var cursor = Uri.EscapeDataString(JsonSerializer.Serialize(page.Continuation, Json));
        var secondPage = await (await ownerClient.GetAsync(clocksPath + "?limit=1&cursor=" + cursor)).Content.ReadFromJsonAsync<H6ClockPage>();
        Assert.NotNull(secondPage); Assert.Single(secondPage.Items);
        Assert.NotEqual(page.Items[0].Id, secondPage.Items[0].Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsiderClient.GetAsync(clocksPath)).StatusCode);
        await using (var db = fixture.CreateDbContext())
            await db.ProjectMembers.Where(row => row.Id == membership.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        var revokedScope = await ownerClient.GetAsync(scopePath);
        Assert.Equal(HttpStatusCode.NotFound, revokedScope.StatusCode);
        var deniedBody = await revokedScope.Content.ReadAsStringAsync();
        Assert.DoesNotContain(project.Id.ToString(), deniedBody, StringComparison.OrdinalIgnoreCase);
        // ProblemDetails.instance may echo the identifier supplied by this caller.
        // Sensitive resolved content must not be returned with the denial.
        Assert.DoesNotContain(notification.Title, deniedBody, StringComparison.Ordinal);
        Assert.DoesNotContain(notification.Body, deniedBody, StringComparison.Ordinal);
        await using (var check = fixture.CreateDbContext())
        {
            var persisted = await check.Notifications.AsNoTracking().SingleAsync(n => n.Id == notification.Id);
            Assert.Null(persisted.ReadAt);
            Assert.Equal(notification.Title, persisted.Title); Assert.Equal(notification.Body, persisted.Body);
            Assert.Equal(2, await check.Set<DeadlineClock>().CountAsync(c => c.ProjectId == project.Id));
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.GetAsync(clocksPath)).StatusCode);
    }

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
}
