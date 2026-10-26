using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class SessionOperationHeaderAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-007", "/api/v1/auth/logout", false, 204)]
    [InlineData("API-015", "/api/v1/auth/web/logout", true, 204)]
    [InlineData("API-016", "/api/v1/auth/web/renew", true, 200)]
    [InlineData("API-017", "/api/v1/auth/web/session", true, 200)]
    public async Task ExactSessionOperationEchoesCorrelationOnSuccessAndCredentialDenial(string operation, string path, bool web, int success)
    {
        Assert.StartsWith("API-", operation);
        var actor = await sql.CreateUserAsync("HEADER-" + Guid.NewGuid().ToString("N"), "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var authenticated = web ? await WebCookieTestClient.Login(factory, actor.Email!) : factory.CreateClient();
        if (!web)
        {
            using var login = await authenticated.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        if (operation == "API-016")
        {
            using var bootstrap = await anonymous.GetAsync("/api/v1/auth/web/csrf");
            Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);
            anonymous.DefaultRequestHeaders.Add("Cookie", Assert.Single(bootstrap.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-RoadGuardCsrf=", StringComparison.Ordinal)).Split(';')[0]);
            anonymous.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await bootstrap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        }
        foreach (var (client, expected) in new[] { (authenticated, success), (anonymous, 401) })
        {
            using var request = new HttpRequestMessage(path.EndsWith("/session", StringComparison.Ordinal) ? HttpMethod.Get : HttpMethod.Post, path);
            var correlation = Guid.NewGuid().ToString(); request.Headers.Add("X-Correlation-ID", correlation);
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            using var response = await client.SendAsync(request);
            Assert.Equal((HttpStatusCode)expected, response.StatusCode);
            Assert.Equal(correlation, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
            Assert.Null(response.Headers.ETag);
            if (expected == 200) Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            if (expected == 204) Assert.Empty(await response.Content.ReadAsByteArrayAsync());
            if (web && expected == success) Assert.True(response.Headers.CacheControl?.NoStore);
            if (expected == 401)
            {
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(401, body.GetProperty("status").GetInt32());
                Assert.DoesNotContain(actor.Email!, body.GetRawText());
            }
        }
    }
}
