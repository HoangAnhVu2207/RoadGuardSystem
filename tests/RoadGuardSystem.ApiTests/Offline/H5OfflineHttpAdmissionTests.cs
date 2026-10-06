using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.DTOs.Offline;
using Xunit;

namespace RoadGuardSystem.ApiTests.Offline;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H5OfflineHttpAdmissionTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task BearerDeviceRegistrationAndRevocationPreserveOneEffectPerKey()
    {
        var actor = await fixture.CreateUserAsync("h5-device-flow-" + Guid.NewGuid().ToString("N"),
            "Current1!", UserRoleCode.RepairCrew);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Offline device HTTP fixture",
            null, null, null, null, DateTimeOffset.UtcNow);
        await using (var setup = fixture.CreateDbContext())
        {
            setup.AddRange(project, new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = actor.Id,
                RoleCode = UserRoleCode.RepairCrew,
                Status = ProjectMemberStatus.Active,
                ValidFrom = new DateOnly(2000, 1, 1)
            });
            await setup.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var deviceId = Guid.NewGuid();
        using var keys = OfflineDeviceKeys.Generate(actor.Id, deviceId.ToString("D"));
        var body = new OfflineDeviceRegisterInput(deviceId, keys.PublicKeys.EncryptionPublicKey,
            keys.PublicKeys.SigningPublicKey);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "device-" + deviceId.ToString("N"));
        var route = $"/api/v1/projects/{project.Id}/offline/devices";
        var created = await client.PostAsJsonAsync(route, body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var registration = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(route, body)).StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "revoke-" + deviceId.ToString("N"));
        var revokeRoute = $"{route}/{registration}/revoke";
        var revoke = new OfflineDeviceRevokeInput("device retired");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(revokeRoute, revoke)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(revokeRoute, revoke)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{route}/{registration}")).StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.Set<OfflineDeviceRegistration>().CountAsync(row => row.Id == registration &&
            row.ProjectId == project.Id && row.ActorId == actor.Id));
        Assert.Equal(1, await db.Set<OfflineDeviceRevocation>().CountAsync(row =>
            row.DeviceRegistrationId == registration && row.ProjectId == project.Id));
        Assert.Equal(1, await db.AuditLogs.CountAsync(row => row.EventType == "offline_device_revoked" &&
            row.ActorUserId == actor.Id));
        (await db.ProjectMembers.SingleAsync(row => row.ProjectId == project.Id && row.UserId == actor.Id))
            .Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{route}/{registration}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(revokeRoute, revoke)).StatusCode);
        Assert.Equal(1, await db.Set<OfflineDeviceRevocation>().CountAsync(row =>
            row.DeviceRegistrationId == registration));
    }

    [Fact]
    public async Task BearerDeviceRegistrationRequiresKeyBeforeAnyProjectOrDeviceEffect()
    {
        var actor = await fixture.CreateUserAsync("h5-device-http-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var response = await client.PostAsJsonAsync(DevicePath(), new OfflineDeviceRegisterInput(Guid.NewGuid(), "key", "key"));
        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Equal("idempotency_key_required", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }

    [Fact]
    public async Task BearerSignedSyncRequiresKeyBeforeAnyBatchEffect()
    {
        var actor = await fixture.CreateUserAsync("h5-sync-http-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/offline/sync",
            new OfflineSignedBatchInput(Guid.NewGuid(), Guid.NewGuid(), [], "invalid"));
        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Equal("idempotency_key_required", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
        Assert.False(await db.Set<RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch>()
            .AnyAsync(row => row.CurrentImporterId == actor.Id));
    }

    [Fact]
    public async Task CookieDeviceRegistrationRequiresCsrfBeforeOfflineAdmission()
    {
        var actor = await fixture.CreateUserAsync("h5-device-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login",
            new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "offline-device-cookie");
        var response = await client.PostAsJsonAsync(DevicePath(), new OfflineDeviceRegisterInput(Guid.NewGuid(), "key", "key"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }

    private static string DevicePath() => $"/api/v1/projects/{Guid.NewGuid()}/offline/devices";
}
