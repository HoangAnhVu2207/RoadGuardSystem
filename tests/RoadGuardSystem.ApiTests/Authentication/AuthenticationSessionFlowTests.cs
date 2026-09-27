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

public sealed partial class AuthenticationFlowTests
{
    [Fact(DisplayName = "P1-10 VG-04: configured session and refresh lifetimes persist and return exact expiries")]
    public async Task Login_ConfiguredSessionAndRefreshLifetimes_DrivePersistedAndReturnedExpiries()
    {
        var username = $"configured_expiry_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            accessTokenLifetimeMinutes: 6,
            sessionLifetimeHours: 3,
            refreshTokenLifetimeDays: 5);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });
        var tokens = await ReadTokensAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var verification = _sql.CreateDbContext();
        var session = await verification.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id);
        var refresh = await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.SessionId == session.Id);
        (session.ExpiresAt - session.IssuedAt).Should().Be(TimeSpan.FromHours(3));
        refresh.ExpiresAt.Should().Be(session.IssuedAt.AddDays(5));
        tokens.ExpiresIn.Should().Be(360);
    }

    [Theory(DisplayName = "P1-10 Positive: every stable global role can login with its canonical JWT role code")]
    [InlineData(UserRoleCode.Supervisor)]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    public async Task Login_AllStableRoles_ReturnCanonicalRoleClaim(UserRoleCode role)
    {
        var username = $"role_matrix_{role}_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(username, "Current1!", role);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await ReadTokensAsync(response);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        jwt.Claims.Single(claim => claim.Type == "role").Value.Should().Be(role.ToDbCode());
    }

    [Theory(DisplayName = "P1-10 Negative: inactive accounts fail login without creating a session")]
    [InlineData(UserStatus.Pending)]
    [InlineData(UserStatus.Suspended)]
    public async Task Login_InactiveAccount_ReturnsGenericUnauthorizedWithoutSession(UserStatus status)
    {
        var username = $"inactive_{status}_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", status: status);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.InvalidCredentials);
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.CountAsync(session => session.UserId == user.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "P1-10 Negative: invalid device metadata fails before credential persistence")]
    public async Task Login_InvalidDeviceMetadata_ReturnsValidationErrorWithoutSession()
    {
        var username = $"metadata_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = Email(username),
            password = "Current1!",
            deviceMetadata = new { schema_version = 1, access_token = "forbidden" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.ValidationError);
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.CountAsync(session => session.UserId == user.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "P1-10 F-04: configured device metadata limits return validation ProblemDetails")]
    public async Task Login_ConfiguredMetadataLimitExceeded_ReturnsValidationErrorNotInternalError()
    {
        var username = $"configured_metadata_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            maxPlatformLength: 3);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = Email(username),
            password = "Current1!",
            deviceMetadata = new { schema_version = 1, platform = "Mobile" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.ValidationError);
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.CountAsync(session => session.UserId == user.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "P1-10 Negative: an expired authoritative session rejects a still-valid JWT")]
    public async Task Bearer_ExpiredServerSession_ReturnsSessionRevoked()
    {
        var username = $"expired_session_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });
        var tokens = await ReadTokensAsync(login);

        await using (var context = _sql.CreateDbContext())
        {
            var expiredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.Database.ExecuteSqlAsync(
                $"UPDATE [Sessions] SET [IssuedAt] = {expiredAt.AddMinutes(-1)}, [ExpiresAt] = {expiredAt} WHERE [UserId] = {user.Id}");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await client.GetAsync("/api/v1/probe/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.SessionRevoked);
    }

    [Fact(DisplayName = "P1-10 VG-04: inactive authoritative user is rejected by the bearer pipeline before the action")]
    public async Task Bearer_InactiveAuthoritativeUser_ReturnsUnauthorizedBeforeProtectedAction()
    {
        var username = $"bearer_inactive_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });
        var tokens = await ReadTokensAsync(login);

        await using (var context = _sql.CreateDbContext())
        {
            var persistedUser = await context.Users.SingleAsync(item => item.Id == user.Id);
            persistedUser.Status = UserStatus.Suspended;
            await context.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await client.GetAsync("/api/v1/probe/protected");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(body).Should().Be(ApiErrorCodes.Unauthorized);
        body.Should().NotContain("protected_ok");
    }

    [Theory(DisplayName = "P1-10 VG-04: missing or invalid sid is rejected by the real bearer pipeline")]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task Bearer_MissingOrInvalidSessionClaim_ReturnsUnauthorizedBeforeProtectedAction(string? sessionClaim)
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateBearerToken(Guid.NewGuid(), sessionClaim));

        var response = await client.GetAsync("/api/v1/probe/protected");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(body).Should().Be(ApiErrorCodes.Unauthorized);
        body.Should().NotContain("protected_ok");
    }

    [Fact(DisplayName = "V2-P1-002: expired refresh token has a distinct stable error")]
    public async Task Refresh_ExpiredToken_ReturnsExpiredWithoutReplacement()
    {
        var username = $"refresh_expired_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });
        var tokens = await ReadTokensAsync(login);

        await using (var context = _sql.CreateDbContext())
        {
            await context.Database.ExecuteSqlAsync(
                $"UPDATE [RefreshTokens] SET [ExpiresAt] = {DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE [SessionId] IN (SELECT [Id] FROM [Sessions] WHERE [UserId] = {user.Id})");
        }

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.RefreshTokenExpired);
        await using var verification = _sql.CreateDbContext();
        (await verification.RefreshTokens.CountAsync(token => token.Session.UserId == user.Id)).Should().Be(1);
    }

    [Fact(DisplayName = "V2-P1-002: unknown refresh token returns the invalid-token code")]
    public async Task Refresh_UnknownToken_ReturnsGenericUnauthorized()
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = "unknown-refresh-token-material" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.RefreshTokenInvalid);
    }

    [Fact(DisplayName = "P1-10 VG-04: malformed refresh token returns validation ProblemDetails")]
    public async Task Refresh_MalformedToken_ReturnsValidationProblemDetails()
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.ValidationError);
    }

    [Fact(DisplayName = "P1-10 Negative: concurrent refresh has one credential winner and revokes the replayed family")]
    public async Task Refresh_ConcurrentDuplicate_OneWinnerOneRejectedAndFamilyRevoked()
    {
        var username = $"refresh_race_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var loginClient = CreateClient(factory);
        var login = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new { email = Email(username), password = "Current1!" });
        var tokens = await ReadTokensAsync(login);
        using var firstClient = CreateClient(factory);
        using var secondClient = CreateClient(factory);

        var requests = await Task.WhenAll(
            firstClient.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken }),
            secondClient.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken }));

        requests.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        requests.Count(response => response.StatusCode == HttpStatusCode.Unauthorized).Should().Be(1);
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.AsNoTracking().SingleAsync(session => session.UserId == user.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.AuditLogs.CountAsync(log =>
            log.EventType == "auth_token_replay_detected" && log.EntityId ==
            verification.Sessions.Where(session => session.UserId == user.Id).Select(session => session.Id).Single()))
            .Should().Be(1);
    }

    [Theory(DisplayName = "P1-10 Negative: forced change rejects wrong current reused or weak replacement passwords")]
    [InlineData("Wrong1!", "Replacement1!", HttpStatusCode.Unauthorized)]
    [InlineData("Current1!", "Current1!", HttpStatusCode.BadRequest)]
    [InlineData("Current1!", "weak", HttpStatusCode.BadRequest)]
    public async Task ForcedPasswordChange_InvalidCredentialOrPolicy_DoesNotChangePassword(
        string currentPassword,
        string newPassword,
        HttpStatusCode expectedStatus)
    {
        var username = $"forced_negative_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", mustChangePassword: true);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/forced-password-change", new
        {
            username,
            currentPassword,
            newPassword,
            confirmPassword = newPassword,
            operationId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(expectedStatus);
        await using var verification = _sql.CreateDbContext();
        (await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id))
            .MustChangePassword.Should().BeTrue();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(0);
    }

    [Fact(DisplayName = "P1-10 VG-04: mismatched password confirmation is rejected before forced-change persistence")]
    public async Task ForcedPasswordChange_MismatchedConfirmation_ReturnsValidationErrorWithoutEffects()
    {
        var username = $"forced_confirmation_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", mustChangePassword: true);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/forced-password-change", new
        {
            username,
            currentPassword = "Current1!",
            newPassword = "Replacement1!",
            confirmPassword = "Different2!",
            operationId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.ValidationError);
        await using var verification = _sql.CreateDbContext();
        (await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id))
            .MustChangePassword.Should().BeTrue();
        (await verification.AuditLogs.CountAsync(item => item.EntityId == user.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "P1-10 VG-04: concurrency conflict returns runtime 409 ProblemDetails")]
    public async Task Login_ApplicationConflict_ReturnsConcurrencyProblemDetails()
    {
        var authService = new FixedAuthService(new AuthResult(AuthStatus.Conflict));
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IAuthService>();
                services.AddSingleton<IAuthService>(authService);
            });
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "conflict.user@example.test",
            password = "Conflict1!"
        });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        ProblemCode(body).Should().Be(ApiErrorCodes.ConcurrencyConflict);
        using var problem = JsonDocument.Parse(body);
        Guid.TryParse(problem.RootElement.GetProperty("correlationId").GetString(), out _).Should().BeTrue();
        body.Should().NotContain("Conflict1!");
    }

    [Fact(DisplayName = "P1-10 VG-04: auth errors and logs omit credential hashes signing keys and device metadata")]
    public async Task AuthenticationFailures_ResponseAndLogs_DoNotCaptureSecrets()
    {
        const string passwordMarker = "LogSecretPassword1!";
        const string refreshMarker = "unknown-refresh-material-for-log-capture";
        const string deviceMarker = "private-device-marker";
        var username = $"log_capture_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(username, "Correct1!");
        using var logs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            loggerProvider: logs);
        using var client = CreateClient(factory);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = Email(username),
            password = passwordMarker,
            deviceMetadata = new { schema_version = 1, device_id = deviceMarker }
        });
        var refresh = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = refreshMarker });
        var captured = string.Join(
            Environment.NewLine,
            await login.Content.ReadAsStringAsync(),
            await refresh.Content.ReadAsStringAsync(),
            logs.CombinedEntries);

        login.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        captured.Should().NotContain(passwordMarker)
            .And.NotContain(refreshMarker)
            .And.NotContain(RefreshTokenGenerator.Hash(refreshMarker))
            .And.NotContain(Convert.ToBase64String(AuthenticationWebApplicationFactory.CurrentSigningKey))
            .And.NotContain(deviceMarker);
    }

}
