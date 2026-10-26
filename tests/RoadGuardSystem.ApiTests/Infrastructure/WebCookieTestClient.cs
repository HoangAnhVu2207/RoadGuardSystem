using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RoadGuardSystem.ApiTests.Infrastructure;

internal static class WebCookieTestClient
{
    internal static async Task<HttpClient> Login(AuthenticationWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await RefreshCsrf(client);
        var login = await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), x => x.StartsWith("__Host-RoadGuardSession=", StringComparison.Ordinal));
        await RefreshCsrf(client);
        Assert.Null(client.DefaultRequestHeaders.Authorization);
        return client;
    }

    internal static async Task RefreshCsrf(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
    }
}
