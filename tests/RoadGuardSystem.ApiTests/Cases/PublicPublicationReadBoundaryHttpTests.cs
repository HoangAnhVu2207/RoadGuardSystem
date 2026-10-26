using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

// Owned SQL fixture; no private source mutation, no external storage or provider.
public sealed class PublicPublicationReadBoundaryHttpTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task MissingPublicSnapshotsAreAnonymousNoStoreAndHaveNoCommandEffects()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await using var db = sql.CreateDbContext();
        var auditsBefore = await db.AuditLogs.CountAsync();
        var receiptsBefore = await db.IdempotencyRecords.CountAsync();
        foreach (var id in new[] { Guid.Empty, Guid.NewGuid() })
        {
            var path = $"/api/v1/public/publications/{id}";
            var first = await client.GetAsync(path);
            var retry = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, retry.StatusCode);
            Assert.True(first.Headers.CacheControl!.NoStore);
            Assert.True(retry.Headers.CacheControl!.NoStore);
            Assert.False(first.Headers.Contains("Set-Cookie"));
            Assert.False(first.Headers.Contains("WWW-Authenticate"));
        }
        Assert.Equal(auditsBefore, await db.AuditLogs.CountAsync());
        Assert.Equal(receiptsBefore, await db.IdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task MalformedPublicIdHasNoMatchedPublicationOrCommandEffects()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await using var db = sql.CreateDbContext();
        var auditsBefore = await db.AuditLogs.CountAsync();
        var receiptsBefore = await db.IdempotencyRecords.CountAsync();
        var response = await client.GetAsync("/api/v1/public/publications/not-a-guid");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(auditsBefore, await db.AuditLogs.CountAsync());
        Assert.Equal(receiptsBefore, await db.IdempotencyRecords.CountAsync());
    }
}
