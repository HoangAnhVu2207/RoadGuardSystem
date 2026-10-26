using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class W01AcceptanceHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ControlledOtpToVersionedProfileReplayAndLogoutHasOneAttributedMutation()
    {
        var sender = new OtpDouble();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IIdentityMessageSender>();
                services.AddSingleton<IIdentityMessageSender>(sender);
            });
        using var client = factory.CreateClient();
        var email = $"w01-{Guid.NewGuid():N}@example.test";
        SetKey(client);
        var registered = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations",
            new { email, password = "Synthetic27!", displayName = "W01 reporter", reporterType = "CITIZEN" });
        Assert.Equal(HttpStatusCode.Accepted, registered.StatusCode);
        var intent = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("intentId").GetGuid();
        Assert.False(string.IsNullOrEmpty(sender.Code));
        SetKey(client);
        var verified = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations/verify", new { intentId = intent, otp = sender.Code });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var credential = await verified.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential.GetProperty("accessToken").GetString());
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        var me = await Read(client, "/api/v1/me");
        var actor = me.GetProperty("id").GetGuid();
        Assert.Equal("REPORTER", me.GetProperty("role").GetString());
        Assert.Equal(actor, (await Read(client, "/api/v1/profile")).GetProperty("userId").GetGuid());
        var version = me.GetProperty("version").GetString();
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"" + version + "\"");
        SetKey(client);
        for (var repeat = 0; repeat < 2; repeat++)
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "W01 updated" })).StatusCode);
        var current = await Read(client, "/api/v1/me");
        Assert.NotEqual(version, current.GetProperty("version").GetString());
        SetKey(client);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "Stale overwrite" })).StatusCode);
        await using (var db = sql.CreateDbContext())
        {
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == actor);
            Assert.Equal("W01 updated", user.DisplayName);
            Assert.True(user.EmailConfirmed);
            Assert.NotEqual("Synthetic27!", user.PasswordHash);
            Assert.Equal(Convert.FromBase64String(current.GetProperty("version").GetString()!), user.RowVersion);
            var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.EntityId == actor && x.EventType == "user_profile_updated").ToArrayAsync());
            Assert.Equal(actor, audit.ActorUserId);
            Assert.DoesNotContain(sender.Code!, (audit.BeforeSnapshot ?? "") + (audit.AfterSnapshot ?? ""));
            var registration = await db.ReporterRegistrationIntents.AsNoTracking().SingleAsync(x => x.Id == intent);
            Assert.NotNull(registration.ConsumedAt);
            Assert.NotEqual(sender.Code, registration.OtpHash);
        }
        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        // The current logout contract requires a key; a rejected request must not revoke.
        var missingKey = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
        Assert.Equal("application/problem+json", missingKey.Content.Headers.ContentType?.MediaType);
        Assert.Equal(actor, (await Read(client, "/api/v1/me")).GetProperty("id").GetGuid());
        SetKey(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        foreach (var route in new[] { "/api/v1/me", "/api/v1/profile" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
        await using var final = sql.CreateDbContext();
        Assert.NotNull(Assert.Single(await final.Sessions.AsNoTracking().Where(x => x.UserId == actor).ToArrayAsync()).RevokedAt);
        Assert.Equal(1, await final.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor && x.Operation == "Logout"));
        Assert.Equal(1, await final.AuditLogs.CountAsync(x => x.EntityId == actor && x.EventType == "user_profile_updated"));
        Assert.Equal("W01 updated", (await final.Users.AsNoTracking().SingleAsync(x => x.Id == actor)).DisplayName);
    }

    [Theory]
    [InlineData("/api/v1/me", false)]
    [InlineData("/api/v1/profile", false)]
    [InlineData("/api/v1/me", true)]
    [InlineData("/api/v1/profile", true)]
    public async Task SelfReadRejectsSuspendedActorOrExpiredJwtWithoutProfileMutation(string route, bool expired)
    {
        var user = await sql.CreateUserAsync("W0127-" + Guid.NewGuid().ToString("N"), "Synthetic27!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Synthetic27!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        await using var db = sql.CreateDbContext();
        if (expired)
        {
            var session = await db.Sessions.AsNoTracking().SingleAsync(x => x.UserId == user.Id);
            token = new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value)
                .Create(user.Id, session.Id, user.RoleCode, DateTimeOffset.UtcNow.AddMinutes(-20), 15);
        }
        else
        {
            var changed = await db.Users.SingleAsync(x => x.Id == user.Id);
            changed.Status = UserStatus.Suspended;
            await db.SaveChangesAsync();
        }
        var before = await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id);
        var audits = await db.AuditLogs.CountAsync(x => x.EntityId == user.Id);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(before.RowVersion, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).RowVersion);
        Assert.Equal(audits, await db.AuditLogs.CountAsync(x => x.EntityId == user.Id));
    }

    private static void SetKey(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
    }

    private static async Task<JsonElement> Read(HttpClient client, string route)
    {
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed class OtpDouble : IIdentityMessageSender
    {
        public string? Code { get; private set; }
        public Task<bool> SendReporterOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            Code = otp;
            return Task.FromResult(true);
        }
        public Task<bool> SendInvitationAsync(string email, string displayName, string roleName, string invitationToken,
            DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
