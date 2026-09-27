using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Generators;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Trait("TaskId", "P1-10")]
[Collection(AuthenticationApiFixture.Name)]
public sealed partial class AuthenticationFlowTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public AuthenticationFlowTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact(DisplayName = "V2-P1-001: forced-change login returns flagged credentials and fresh login needs replacement password")]
    public async Task ForcedPasswordChange_RequiredThenCompleted_RequiresFreshLogin()
    {
        var username = $"forced_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", mustChangePassword: true);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = Email(username),
            password = "Current1!"
        });
        var blockedBody = await blocked.Content.ReadFromJsonAsync<JsonElement>();

        blocked.StatusCode.Should().Be(HttpStatusCode.OK);
        blockedBody.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();

        var operationId = Guid.NewGuid();
        var changeRequest = new
        {
            username,
            currentPassword = "Current1!",
            newPassword = "Replacement1!",
            confirmPassword = "Replacement1!",
            operationId
        };
        var changed = await client.PostAsJsonAsync("/api/v1/auth/forced-password-change", changeRequest);
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var replay = await client.PostAsJsonAsync("/api/v1/auth/forced-password-change", changeRequest);
        replay.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await using (var verification = _sql.CreateDbContext())
        {
            (await verification.AuditLogs.CountAsync(item =>
                item.EntityId == user.Id && item.EventType == "auth_password_changed"))
                .Should().Be(1);
        }

        (await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Replacement1!" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "P1-10 F-03: forced-change retry survives active JWT signing-key rotation")]
    public async Task ForcedPasswordChange_RetryAfterJwtKeyRotation_RemainsIdempotent()
    {
        var username = $"forced_key_rotation_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", mustChangePassword: true);
        var request = new
        {
            username,
            currentPassword = "Current1!",
            newPassword = "Replacement1!",
            confirmPassword = "Replacement1!",
            operationId = Guid.NewGuid()
        };

        await using (var firstHost = new AuthenticationWebApplicationFactory(
                         _sql.ConnectionString,
                         activeKeyId: "test-current"))
        using (var client = CreateClient(firstHost))
        {
            (await client.PostAsJsonAsync("/api/v1/auth/forced-password-change", request))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        await using (var rotatedHost = new AuthenticationWebApplicationFactory(
                         _sql.ConnectionString,
                         activeKeyId: "test-next"))
        using (var client = CreateClient(rotatedHost))
        {
            (await client.PostAsJsonAsync("/api/v1/auth/forced-password-change", request))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        await using var verification = _sql.CreateDbContext();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 Positive: login refresh logout revokes access and persists refresh hashes only")]
    public async Task LoginRefreshLogout_ValidFlow_RevokesAllCredentials()
    {
        var username = $"flow_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = Email(username),
            password = "Current1!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await ReadTokensAsync(login);

        await using (var verification = _sql.CreateDbContext())
        {
            var persisted = await verification.RefreshTokens.AsNoTracking()
                .SingleAsync(token => token.Session.UserId == user.Id);
            persisted.TokenHash.Should().NotBe(first.RefreshToken);
            persisted.TokenHash.Should().HaveLength(64);
        }

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = first.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await ReadTokensAsync(refresh);
        second.RefreshToken.Should().NotBe(first.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.AccessToken);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        (await client.GetAsync("/api/v1/probe/protected")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/api/v1/auth/logout", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rejected = await client.GetAsync("/api/v1/probe/protected");
        rejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(await rejected.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.SessionRevoked);

        var duplicateLogout = await client.PostAsync("/api/v1/auth/logout", content: null);
        duplicateLogout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = second.RefreshToken }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "P1-10 Negative: stale bearer role is rejected and revokes the session before the action")]
    public async Task Bearer_StaleRole_RejectsAndRevokesSession()
    {
        var username = $"role_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });
        var tokens = await ReadTokensAsync(login);

        await using (var context = _sql.CreateDbContext())
        {
            await context.Database.ExecuteSqlAsync(
                $"UPDATE [Users] SET [RoleCode] = {"PM"} WHERE [Id] = {user.Id}");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await client.GetAsync("/api/v1/probe/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.SessionRevoked);
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.AsNoTracking().SingleAsync(session => session.UserId == user.Id))
            .RevokedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "P1-10 Negative: malformed login and unknown credentials use stable sanitized ProblemDetails")]
    public async Task Login_InvalidInputs_ReturnsStableProblemDetailsWithoutCredentialLeak()
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        const string suppliedPassword = "NeverEchoThis1!";

        var malformed = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });
        malformed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemCode(await malformed.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.ValidationError);

        var unknown = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = $"missing_{Guid.NewGuid():N}@example.test",
            password = suppliedPassword
        });
        var body = await unknown.Content.ReadAsStringAsync();
        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(body).Should().Be(ApiErrorCodes.InvalidCredentials);
        body.Should().NotContain(suppliedPassword).And.NotContain("PasswordHash");
    }

    [Fact(DisplayName = "P1-10 VG-04: existing username with a wrong password returns generic unauthorized without a session")]
    public async Task Login_ExistingUsernameWrongPassword_ReturnsGenericUnauthorizedWithoutSession()
    {
        var username = $"wrong_password_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Correct1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = Email(username),
            password = "Wrong1!"
        });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(body).Should().Be(ApiErrorCodes.InvalidCredentials);
        body.Should().NotContain(username).And.NotContain("Wrong1!");
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.CountAsync(session => session.UserId == user.Id)).Should().Be(0);
    }

    private static HttpClient CreateClient(AuthenticationWebApplicationFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static string Email(string username) => AuthenticationSqlServerFixture.EmailFor(username);

    private static string ProblemCode(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("code").GetString()!;
    }

    private static async Task<TokenPair> ReadTokensAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new TokenPair(
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!,
            body.GetProperty("expiresIn").GetInt32());
    }

    private static string CreateBearerToken(Guid userId, string? sessionClaim)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", UserRoleCode.DroneOperator.ToDbCode())
        };
        if (sessionClaim is not null)
        {
            claims.Add(new Claim("sid", sessionClaim));
        }

        var now = DateTimeOffset.UtcNow;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(AuthenticationWebApplicationFactory.CurrentSigningKey)
            {
                KeyId = AuthenticationWebApplicationFactory.CurrentKeyId
            },
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            AuthenticationWebApplicationFactory.TestIssuer,
            AuthenticationWebApplicationFactory.TestAudience,
            claims,
            now.UtcDateTime,
            now.AddMinutes(5).UtcDateTime,
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record TokenPair(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn);

    private sealed class FixedAuthService : IAuthService
    {
        private readonly AuthResult _result;

        public FixedAuthService(AuthResult result)
        {
            _result = result;
        }

        public Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);

        public Task<AuthResult> RefreshAsync(RefreshCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);

        public Task<AuthResult> LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);

        public Task<AuthResult> LogoutAsync(Guid userId, Guid sessionId, string idempotencyKey, Guid? correlationId = null, CancellationToken cancellationToken = default) => Task.FromResult(_result);

        public Task<PasswordRecoveryResult> RequestPasswordRecoveryAsync(PasswordRecoveryCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new PasswordRecoveryResult(Guid.NewGuid()));

        public Task<AuthResult> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default) => Task.FromResult(_result);

        public Task<AuthResult> CompleteForcedPasswordChangeAsync(
            ForcedPasswordChangeCommand command,
            CancellationToken cancellationToken = default) => Task.FromResult(_result);
    }
}
