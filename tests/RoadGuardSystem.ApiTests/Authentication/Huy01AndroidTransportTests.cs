using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Trait("Package", "HUY-01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Huy01AndroidTransportTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task AndroidLoginAndRefresh_PersistentSessionAndFiniteFifteenMinuteAccess()
    {
        var username = $"android-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var login = await client.PostAsJsonAsync("/api/v1/auth/android/login",
            new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.CacheControl?.NoStore);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(900, body.GetProperty("expiresIn").GetInt32());
        var token = body.GetProperty("accessToken").GetString()!;
        var refreshToken = body.GetProperty("refreshToken").GetString()!;
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var sid = Guid.Parse(parsed.Claims.Single(claim => claim.Type == "sid").Value);

        await using (var db = fixture.CreateDbContext())
        {
            var session = await db.Sessions.AsNoTracking().SingleAsync(item => item.Id == sid);
            Assert.Equal(user.Id, session.UserId);
            Assert.Equal(SessionTransport.Android, session.Transport);
            Assert.Null(session.ExpiresAt);
            Assert.Equal(SessionLifecycle.PersistentRenewable, session.Lifecycle);
            Assert.InRange(parsed.ValidTo - session.IssuedAt.UtcDateTime, TimeSpan.FromMinutes(15).Add(-TimeSpan.FromSeconds(2)),
                TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(2)));
        }

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/android/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotated = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(900, rotated.GetProperty("expiresIn").GetInt32());
        await using var verification = fixture.CreateDbContext();
        var persisted = await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == sid);
        Assert.Null(persisted.ExpiresAt);
        Assert.Equal(2, await verification.RefreshTokens.CountAsync(item => item.SessionId == sid));
        Assert.All(await verification.RefreshTokens.Where(item => item.SessionId == sid).ToListAsync(),
            item => Assert.Null(item.ExpiresAt));
    }

    [Fact]
    public async Task Rotation_RechecksCurrentUserInsideSqlTransactionAfterPreflight()
    {
        var username = $"android-revoke-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/android/login",
            new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Guid oldTokenId;
        byte[] version;
        await using (var db = fixture.CreateDbContext())
        {
            var old = await db.RefreshTokens.AsNoTracking().SingleAsync(item => item.Session.UserId == user.Id);
            oldTokenId = old.Id;
            version = old.RowVersion;
        }

        await using (var db = fixture.CreateDbContext())
        {
            var current = await db.Users.SingleAsync(item => item.Id == user.Id);
            current.MustChangePassword = true;
            await db.SaveChangesAsync();
        }

        var replacementId = Guid.NewGuid();
        await using (var db = fixture.CreateDbContext())
        {
            var repository = new IdentityRepository(db);
            var result = await repository.RotateRefreshTokenAsync(oldTokenId, version,
                new RefreshToken
                {
                    Id = replacementId,
                    TokenHash = new string('a', 64),
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
                });
            Assert.Equal(RotateRefreshTokenStatus.SessionRevoked, result.Status);
        }
        await using (var db = fixture.CreateDbContext())
        {
            Assert.False(await db.RefreshTokens.AnyAsync(item => item.Id == replacementId));
            Assert.Null((await db.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == oldTokenId)).RevokedAt);
        }
    }

    [Fact]
    public async Task Refresh_RevokedPersistentSession_DoesNotIssueReplacement()
    {
        var username = $"android-expired-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/android/login",
            new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        var refreshToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();
        Guid sessionId;
        await using (var db = fixture.CreateDbContext())
        {
            var session = await db.Sessions.SingleAsync(item => item.UserId == user.Id);
            sessionId = session.Id;
            session.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/auth/android/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(1, await verification.RefreshTokens.CountAsync(item => item.SessionId == sessionId));
    }

    [Fact]
    public async Task ConcurrentSameRefresh_HasAtMostOneRotationWinner()
    {
        var username = $"android-concurrent-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/android/login",
            new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        var refreshToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();

        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/auth/android/refresh", new { refreshToken }),
            client.PostAsJsonAsync("/api/v1/auth/android/refresh", new { refreshToken }));
        Assert.InRange(attempts.Count(response => response.StatusCode == HttpStatusCode.OK), 0, 1);
        Assert.All(attempts, response => Assert.Contains(response.StatusCode,
            new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.Conflict }));
        await using var verification = fixture.CreateDbContext();
        var session = await verification.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id);
        Assert.InRange(await verification.RefreshTokens.CountAsync(item => item.SessionId == session.Id), 1, 2);
    }
}
