using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Processing;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ValidationRunCredentialAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-204", "POST", "/api/v1/projects/{id}/validation-runs", 403)]
    [InlineData("API-241", "GET", "/api/v1/validation-runs/{id}", 404)]
    public async Task ExactValidationRouteEnforcesBearerExpiryRevocationAndNoCookieFallback(string operation, string method, string route, int admittedStatus)
    {
        Assert.Contains(operation, new[] { "API-204", "API-241" });
        var actor = await sql.CreateUserAsync("validation-credentials-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services => services.RemoveAll<IHostedService>());
        using var bearer = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var web = await WebCookieTestClient.Login(factory, actor.Email!);
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var login = await bearer.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        var sid = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(x => x.Type == "sid").Value);
        var expired = new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value)
            .Create(actor.Id, sid, actor.RoleCode, DateTimeOffset.UtcNow.AddMinutes(-20), 15);
        var path = route.Replace("{id}", Guid.NewGuid().ToString());
        await using var db = sql.CreateDbContext();
        var auditIds = await db.AuditLogs.Where(x => x.ActorUserId == actor.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using (var admitted = await Send(bearer)) Assert.Equal(admittedStatus, (int)admitted.StatusCode);
        await Denied(anonymous, "auth_unauthorized");
        // These two exact operations intentionally have no WebCookieEligible metadata.
        await Denied(web, "auth_unauthorized");
        foreach (var badToken in new[] { "malformed", expired })
        {
            bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", badToken);
            await Denied(bearer, "auth_unauthorized");
        }
        web.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed");
        await Denied(web, "auth_unauthorized");
        web.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using (var admitted = await Send(web)) Assert.Equal(admittedStatus, (int)admitted.StatusCode);
        await db.Sessions.Where(x => x.Id == sid).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await Denied(bearer, "auth_session_revoked");
        await Denied(web, "auth_session_revoked");
        Assert.Equal(auditIds, await db.AuditLogs.Where(x => x.ActorUserId == actor.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync());
        Assert.Empty(await db.IdempotencyRecords.Where(x => x.ActorUserId == actor.Id).ToArrayAsync());

        Task<HttpResponseMessage> Send(HttpClient client)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (method == "POST")
            {
                request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
                request.Content = JsonContent.Create(new { pairs = new[] { new { groundTruthId = Guid.NewGuid(), derivedMeasurementId = Guid.NewGuid() } }, modelVersionId = Guid.NewGuid(), datasetSplitId = Guid.NewGuid(), measurementType = "DEPRESSION_DEPTH", unit = "mm" });
            }
            return client.SendAsync(request);
        }
        async Task Denied(HttpClient client, string code)
        {
            using var response = await Send(client);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(401, body.GetProperty("status").GetInt32());
            Assert.Equal(code, body.GetProperty("code").GetString());
            Assert.True(Guid.TryParse(body.GetProperty("correlationId").GetString(), out _));
            Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
            Assert.Null(response.Headers.ETag);
            Assert.Null(response.Headers.Location);
        }
    }
}
