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
public sealed class W01OtpBoundaryHttpTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidOrExpiredOtpCannotActivateOrCreateCredentials(bool expired)
    {
        var sender = new OtpDouble();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<IIdentityMessageSender>(); services.AddSingleton<IIdentityMessageSender>(sender); });
        using var client = factory.CreateClient();
        var intentId = await Register(client);
        await using var db = sql.CreateDbContext();
        var intent = await db.ReporterRegistrationIntents.SingleAsync(x => x.Id == intentId);
        var actor = intent.UserId!.Value;
        if (expired) { intent.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1); await db.SaveChangesAsync(); }
        var before = await db.Users.AsNoTracking().SingleAsync(x => x.Id == actor);
        var code = expired ? sender.Code : (sender.Code == "000000" ? "000001" : "000000");
        SetKey(client);
        var rejected = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations/verify", new { intentId, otp = code });
        Assert.Equal(expired ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        var response = await rejected.Content.ReadAsStringAsync();
        Assert.DoesNotContain(sender.Code!, response);
        Assert.DoesNotContain("accessToken", response);
        db.ChangeTracker.Clear();
        var after = await db.Users.AsNoTracking().SingleAsync(x => x.Id == actor);
        Assert.False(after.EmailConfirmed);
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(before.RowVersion, after.RowVersion);
        Assert.False(await db.Sessions.AnyAsync(x => x.UserId == actor));
        Assert.False(await db.RefreshTokens.AnyAsync(x => db.Sessions.Any(y => y.Id == x.SessionId && y.UserId == actor)));
        var final = await db.ReporterRegistrationIntents.AsNoTracking().SingleAsync(x => x.Id == intentId);
        Assert.Null(final.ConsumedAt);
        Assert.Equal(expired ? 0 : 1, final.FailedAttempts);
    }

    [Fact]
    public async Task ConsumedOtpRejectsNewKeyButSameKeyReplaysExactlyOneSession()
    {
        var sender = new OtpDouble();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<IIdentityMessageSender>(); services.AddSingleton<IIdentityMessageSender>(sender); });
        using var client = factory.CreateClient();
        var intentId = await Register(client);
        SetKey(client);
        var request = new { intentId, otp = sender.Code };
        var first = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations/verify", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await first.Content.ReadAsStringAsync();
        var replay = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations/verify", request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var original = JsonSerializer.Deserialize<JsonElement>(firstBody);
        var repeated = await replay.Content.ReadFromJsonAsync<JsonElement>();
        // AccessTokenFactory deliberately generates a fresh jti; replay must reuse persisted credentials.
        Assert.True(original.GetProperty("refreshToken").GetString() == repeated.GetProperty("refreshToken").GetString(), "Replay must reuse refresh credential");
        Assert.Equal(original.GetProperty("user").GetProperty("id").GetGuid(), repeated.GetProperty("user").GetProperty("id").GetGuid());
        SetKey(client);
        var rejected = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations/verify", request);
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        Assert.DoesNotContain("accessToken", await rejected.Content.ReadAsStringAsync());
        await using var db = sql.CreateDbContext();
        var intent = await db.ReporterRegistrationIntents.AsNoTracking().SingleAsync(x => x.Id == intentId);
        Assert.NotNull(intent.ConsumedAt);
        Assert.NotEqual(sender.Code, intent.OtpHash);
        Assert.Equal(1, await db.Sessions.CountAsync(x => x.UserId == intent.UserId));
        Assert.Equal(1, await db.RefreshTokens.CountAsync(x => db.Sessions.Any(y => y.Id == x.SessionId && y.UserId == intent.UserId)));
        Assert.True((await db.Users.AsNoTracking().SingleAsync(x => x.Id == intent.UserId)).EmailConfirmed);
    }

    private static async Task<Guid> Register(HttpClient client)
    {
        SetKey(client);
        var response = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations", new { email = $"w0128-{Guid.NewGuid():N}@example.test", password = "Synthetic28!", displayName = "OTP boundary", reporterType = "CITIZEN" });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("intentId").GetGuid();
    }
    private static void SetKey(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
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
