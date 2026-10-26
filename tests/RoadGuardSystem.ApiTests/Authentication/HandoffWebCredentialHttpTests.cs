using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

public sealed class HandoffWebCredentialHttpTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    public static IEnumerable<object[]> Operations() => HandoffExpiredCredentialHttpTests.Operations();

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task ExactOperationAcceptsLiveCookieAdmissionAndDeniesExpiredTicket(string operationId, string method, string route)
    {
        Assert.StartsWith("ADD-", operationId);
        var user = await sql.CreateUserAsync($"handoff-web-expiry-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock); });
        using var client = await WebCookieTestClient.Login(factory, user.Email!);
        var path = Regex.Replace(route, "\\{[^}]+\\}", _ => Guid.NewGuid().ToString());
        // Random IDs deliberately stop at project/resource authority after
        // credential admission. This is not a positive business operation.
        using (var admitted = await Send(client, method, path))
        {
            Assert.NotEqual(HttpStatusCode.Unauthorized, admitted.StatusCode);
            Assert.True(admitted.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
            if (admitted.StatusCode == HttpStatusCode.Forbidden)
                Assert.NotEqual("csrf_failed", (await admitted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        if (method != "GET")
        {
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            using (var missing = await Send(client, method, path)) await AssertCsrfDenied(missing);
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token");
            using (var invalid = await Send(client, method, path)) await AssertCsrfDenied(invalid);
            await WebCookieTestClient.RefreshCsrf(client);
        }
        clock.Now += TimeSpan.FromHours(13);
        using var expired = await Send(client, method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    private static async Task AssertCsrfDenied(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }

    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
