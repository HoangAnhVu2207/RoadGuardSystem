using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Notifications;

[Trait("TaskId", "P2-001/P2-002/P2-062")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P2NotificationApiTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P2NotificationApiTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task NotificationEndpoints_EnforceOwnerAndReplayRead()
    {
        var owner = await _sql.CreateUserAsync($"p2_notification_owner_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var other = await _sql.CreateUserAsync($"p2_notification_other_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P2-INBOX-{Guid.NewGuid():N}",
            Name = "Inbox source",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var notification = Notification.Create(Guid.NewGuid(), owner.Id, "Project", project.Id, "ASSIGNED", "Survey assigned", "A survey task was assigned.", DateTimeOffset.UtcNow);
        await using (var context = _sql.CreateDbContext())
        {
            context.Projects.Add(project);
            context.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = owner.Id,
                RoleCode = UserRoleCode.DroneOperator,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            });
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var ownerClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var otherClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AuthenticateAsync(ownerClient, owner.UserName!);
        await AuthenticateAsync(otherClient, other.UserName!);

        var list = await ownerClient.GetAsync("/api/v1/notifications");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await list.Content.ReadFromJsonAsync<JsonElement>();
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Should().Contain(notification.Id);

        var get = await ownerClient.GetAsync($"/api/v1/notifications/{notification.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = (await get.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString();
        get.Headers.ETag!.Tag.Should().Be($"\"{version}\"");

        var hidden = await otherClient.GetAsync($"/api/v1/notifications/{notification.Id}");
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);

        const string key = "p2-notification-read-001";
        var read = await MarkReadAsync(ownerClient, notification.Id, key, version!);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var replay = await MarkReadAsync(ownerClient, notification.Id, key, version!);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        var readBody = await read.Content.ReadFromJsonAsync<JsonElement>();
        var nextVersion = readBody.GetProperty("version").GetString()!;
        var secondRead = await MarkReadAsync(ownerClient, notification.Id, "p2-notification-read-002", nextVersion);
        secondRead.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var verify = _sql.CreateDbContext();
        (await verify.Notifications.FindAsync(notification.Id))!.ReadAt.Should().NotBeNull();
    }

    private static async Task<HttpResponseMessage> MarkReadAsync(HttpClient client, Guid notificationId, string key, string version)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/notifications/{notificationId}/read");
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return await client.SendAsync(request);
    }

    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }
}
