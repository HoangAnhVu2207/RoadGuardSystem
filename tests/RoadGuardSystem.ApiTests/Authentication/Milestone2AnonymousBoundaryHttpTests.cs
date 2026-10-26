using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

// This checks only the advertised protected anonymous boundary. It does not prove
// successful authorization, DTO compatibility or full operation acceptance.
[Collection(AuthenticationApiFixture.Name)]
public sealed class Milestone2AnonymousBoundaryHttpTests(AuthenticationSqlServerFixture sql)
{
    public static IEnumerable<object[]> ProtectedOperations()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "deploy", "fe-handoff", "openapi.json")))
            directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Cannot find the pinned handoff OpenAPI.");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "deploy", "fe-handoff", "openapi.json")));
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
            foreach (var operation in path.Value.EnumerateObject())
            {
                if (operation.Name is not ("get" or "post" or "put" or "delete" or "patch")) continue;
                if (!operation.Value.TryGetProperty("security", out var security) || security.GetArrayLength() == 0) continue;
                // CSRF and renewal credentials are not an authenticated actor. The
                // two AllowAnonymous Web actions are tested explicitly below.
                if (!security.EnumerateArray().Any(requirement => requirement.TryGetProperty("Bearer", out _)
                    || requirement.TryGetProperty("WebSession", out _))) continue;
                var route = path.Name;
                if (operation.Value.TryGetProperty("parameters", out var parameters))
                    foreach (var parameter in parameters.EnumerateArray().Where(x => x.GetProperty("in").GetString() == "path"))
                    {
                        var schema = parameter.GetProperty("schema");
                        var value = schema.TryGetProperty("format", out var format) && format.GetString() == "uuid"
                            ? "00000000-0000-0000-0000-000000000001" : "boundary";
                        route = route.Replace("{" + parameter.GetProperty("name").GetString() + "}", value, StringComparison.Ordinal);
                    }
                var method = operation.Name.ToUpperInvariant();
                var operationKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(method + " " + path.Name)))[..16];
                yield return [operationKey, method, path.Name, route];
            }
    }

    [Theory]
    [MemberData(nameof(ProtectedOperations))]
    public async Task AdvertisedProtectedOperationRejectsAnonymousAndInvalidBearer(string operationKey, string method, string contractRoute, string requestRoute)
    {
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(method + " " + contractRoute)))[..16], operationKey);
        Assert.False(string.IsNullOrWhiteSpace(contractRoute));
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var anonymous = new HttpRequestMessage(new HttpMethod(method), requestRoute);
        using var denied = await client.SendAsync(anonymous);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var malformed = new HttpRequestMessage(new HttpMethod(method), requestRoute);
        malformed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid");
        using var invalid = await client.SendAsync(malformed);
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/auth/web/login")]
    [InlineData("/api/v1/auth/web/renew")]
    public async Task AnonymousWebCredentialActionsRequireCsrf(string route)
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        if (route.EndsWith("/login", StringComparison.Ordinal))
            request.Content = JsonContent.Create(new { email = "boundary@example.invalid", password = "Boundary1!" });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("csrf_failed", problem.GetProperty("code").GetString());
        Assert.True(response.Headers.CacheControl!.NoStore);
    }
}
