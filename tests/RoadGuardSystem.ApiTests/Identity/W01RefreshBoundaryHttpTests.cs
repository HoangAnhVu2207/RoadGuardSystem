using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class W01RefreshBoundaryHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task MalformedOperationKeyCannotRotateOrRevokeCredential()
    {
        var user = await sql.CreateUserAsync($"refresh29-{Guid.NewGuid():N}", "Synthetic29!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        var original = await Login(client, user.UserName!);
        await using var db = sql.CreateDbContext();
        var before = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Session.UserId == user.Id);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Idempotency-Key", new string('x', 151));
        var denied = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.DoesNotContain("accessToken", await denied.Content.ReadAsStringAsync());
        var after = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Session.UserId == user.Id);
        Assert.Equal(before.RowVersion, after.RowVersion);
        Assert.Null(after.RevokedAt);
        Assert.Null((await db.Sessions.AsNoTracking().SingleAsync(x => x.UserId == user.Id)).RevokedAt);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original })).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameOperationReplaysOneSuccessorButCurrentRevocationAndDifferentKeyDeny(bool revokeBeforeReplay)
    {
        var user = await sql.CreateUserAsync($"replay29-{Guid.NewGuid():N}", "Synthetic29!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        var original = await Login(client, user.UserName!);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var first = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var replacement = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();
        Assert.NotEqual(original, replacement);
        await using var db = sql.CreateDbContext();
        if (revokeBeforeReplay)
        {
            var session = await db.Sessions.SingleAsync(x => x.UserId == user.Id);
            session.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(revokeBeforeReplay ? HttpStatusCode.Unauthorized : HttpStatusCode.OK, replay.StatusCode);
        if (!revokeBeforeReplay)
        {
            Assert.Equal(replacement, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString());
            client.DefaultRequestHeaders.Remove("Idempotency-Key");
            client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original })).StatusCode);
        }
        else Assert.DoesNotContain("refreshToken", await replay.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = replacement })).StatusCode);
        Assert.Equal(2, await db.RefreshTokens.CountAsync(x => x.Session.UserId == user.Id));
        Assert.NotNull((await db.Sessions.AsNoTracking().SingleAsync(x => x.UserId == user.Id)).RevokedAt);
        if (!revokeBeforeReplay)
            Assert.Equal(0, await db.RefreshTokens.CountAsync(x => x.Session.UserId == user.Id && x.RevokedAt == null));
    }

    private static async Task<string> Login(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Synthetic29!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
    }
}
