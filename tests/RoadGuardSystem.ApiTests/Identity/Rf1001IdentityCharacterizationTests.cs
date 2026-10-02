using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Trait("TaskId", "RF-10-01-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Rf1001IdentityCharacterizationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public Rf1001IdentityCharacterizationTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task ProfileFamilies_SameAccountCrossReadAndWrite_KeepDistinctWireAndSharedRow()
    {
        var username = $"rf1001_profile_{Guid.NewGuid():N}";
        var otherName = $"rf1001_other_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        var other = await _sql.CreateUserAsync(otherName, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/v1/profile")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AuthenticateAsync(client, username, "Current1!");

        var oldRead = await client.GetAsync("/api/v1/profile");
        var meRead = await client.GetAsync("/api/v1/me");
        oldRead.StatusCode.Should().Be(HttpStatusCode.OK);
        meRead.StatusCode.Should().Be(HttpStatusCode.OK);
        oldRead.Headers.ETag.Should().BeNull();
        meRead.Headers.ETag.Should().BeNull();
        using var oldJson = JsonDocument.Parse(await oldRead.Content.ReadAsStringAsync());
        using var meJson = JsonDocument.Parse(await meRead.Content.ReadAsStringAsync());
        oldJson.RootElement.GetProperty("userId").GetGuid().Should().Be(user.Id);
        oldJson.RootElement.GetProperty("email").GetString().Should().Be(AuthenticationSqlServerFixture.EmailFor(username));
        meJson.RootElement.GetProperty("id").GetGuid().Should().Be(user.Id);
        meJson.RootElement.GetProperty("version").GetString().Should().Be(oldJson.RootElement.GetProperty("rowVersion").GetString());
        AssertReducedActor(meJson.RootElement);

        var oldVersion = oldJson.RootElement.GetProperty("rowVersion").GetString();
        var oldCommand = new
        {
            displayName = "Legacy profile edit",
            email = AuthenticationSqlServerFixture.EmailFor(username),
            expectedRowVersion = oldVersion,
            operationId = Guid.NewGuid()
        };
        var oldWrite = await client.PutAsJsonAsync("/api/v1/profile", oldCommand);
        oldWrite.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync("/api/v1/profile", oldCommand)).StatusCode.Should().Be(HttpStatusCode.OK);
        using var afterOld = JsonDocument.Parse(await (await client.GetAsync("/api/v1/me")).Content.ReadAsStringAsync());
        afterOld.RootElement.GetProperty("displayName").GetString().Should().Be("Legacy profile edit");
        var newVersion = afterOld.RootElement.GetProperty("version").GetString();
        newVersion.Should().NotBe(oldVersion);

        using var stale = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/me")
        {
            Content = JsonContent.Create(new { displayName = "Must not persist" })
        };
        stale.Headers.TryAddWithoutValidation("If-Match", $"\"{oldVersion}\"");
        stale.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        (await client.SendAsync(stale)).StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

        using var update = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/me")
        {
            Content = JsonContent.Create(new { displayName = "Me profile edit" })
        };
        update.Headers.TryAddWithoutValidation("If-Match", $"\"{newVersion}\"");
        update.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var meWrite = await client.SendAsync(update);
        meWrite.StatusCode.Should().Be(HttpStatusCode.OK);
        meWrite.Headers.ETag.Should().NotBeNull();
        using var finalOld = JsonDocument.Parse(await (await client.GetAsync("/api/v1/profile")).Content.ReadAsStringAsync());
        finalOld.RootElement.GetProperty("displayName").GetString().Should().Be("Me profile edit");
        finalOld.RootElement.GetProperty("rowVersion").GetString().Should().Be(meWrite.Headers.ETag!.Tag.Trim('"'));

        await using (var check = _sql.CreateDbContext())
        {
            var persisted = await check.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
            persisted.DisplayName.Should().Be("Me profile edit");
            persisted.Email.Should().Be(AuthenticationSqlServerFixture.EmailFor(username));
            (await check.AuditLogs.CountAsync(item => item.EntityId == user.Id && item.EventType == "user_profile_updated")).Should().Be(2);
            (await check.IdempotencyRecords.CountAsync(item => item.ActorUserId == user.Id && item.Operation == "UserProfileUpdated")).Should().Be(2);
            (await check.Users.AsNoTracking().SingleAsync(item => item.Id == other.Id)).DisplayName.Should().Be(otherName);
        }

        using var otherClient = factory.CreateClient();
        await AuthenticateAsync(otherClient, otherName, "Current1!");
        using var otherRead = JsonDocument.Parse(await (await otherClient.GetAsync("/api/v1/me")).Content.ReadAsStringAsync());
        otherRead.RootElement.GetProperty("id").GetGuid().Should().Be(other.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdminResetFamilies_UseSeparateTargetsAndRevokeOldCredentials(bool v2)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var supervisorName = $"rf1001_supervisor_{suffix}";
        var targetName = $"rf1001_target_{suffix}";
        var operatorName = $"rf1001_operator_{suffix}";
        var supervisor = await _sql.CreateUserAsync(supervisorName, "Supervisor1!", UserRoleCode.Supervisor);
        var target = await _sql.CreateUserAsync(targetName, "Current1!");
        await _sql.CreateUserAsync(operatorName, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var targetClient = factory.CreateClient();
        var oldTokens = await AuthenticateAsync(targetClient, targetName, "Current1!");
        using var operatorClient = factory.CreateClient();
        await AuthenticateAsync(operatorClient, operatorName, "Current1!");
        using var supervisorClient = factory.CreateClient();
        await AuthenticateAsync(supervisorClient, supervisorName, "Supervisor1!");
        var route = v2
            ? $"/api/v1/users/{target.Id}/password-reset"
            : $"/api/v1/admin/users/{target.Id}/password-reset";
        var key = Guid.NewGuid().ToString("N");
        var operationId = Guid.NewGuid();
        string version;
        string initialHash;
        await using (var before = _sql.CreateDbContext())
        {
            var row = await before.Users.AsNoTracking().SingleAsync(item => item.Id == target.Id);
            version = Convert.ToBase64String(row.RowVersion);
            initialHash = row.PasswordHash!;
        }

        async Task<HttpResponseMessage> SendReset(HttpClient caller)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, route)
            {
                Content = v2
                    ? JsonContent.Create(new { temporaryPassword = "Replacement2!", reason = "ADMINISTRATOR_INITIATED" })
                    : JsonContent.Create(new { expectedTargetRowVersion = version, operationId })
            };
            if (v2) request.Headers.Add("Idempotency-Key", key);
            return await caller.SendAsync(request);
        }

        (await SendReset(operatorClient)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var denied = _sql.CreateDbContext())
        {
            (await denied.Users.AsNoTracking().SingleAsync(item => item.Id == target.Id)).PasswordHash
                .Should().Be(initialHash);
            (await denied.PasswordResetLogs.CountAsync(item => item.TargetUserId == target.Id)).Should().Be(0);
        }

        var first = await SendReset(supervisorClient);
        first.StatusCode.Should().Be(v2 ? HttpStatusCode.NoContent : HttpStatusCode.OK);
        string replacement;
        if (v2)
        {
            (await first.Content.ReadAsStringAsync()).Should().BeEmpty();
            replacement = "Replacement2!";
        }
        else
        {
            using var body = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
            replacement = body.RootElement.GetProperty("temporaryPassword").GetString()!;
            replacement.Should().NotBeNullOrWhiteSpace();
            body.RootElement.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();
        }

        var replay = await SendReset(supervisorClient);
        replay.StatusCode.Should().Be(v2 ? HttpStatusCode.NoContent : HttpStatusCode.OK);
        if (!v2)
        {
            using var body = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
            body.RootElement.GetProperty("temporaryPassword").ValueKind.Should().Be(JsonValueKind.Null);
        }

        var oldAccess = await targetClient.GetAsync("/api/v1/me");
        oldAccess.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        targetClient.DefaultRequestHeaders.Authorization = null;
        (await targetClient.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = oldTokens.Refresh }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await targetClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(targetName), password = "Current1!"
        })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var newLogin = await targetClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(targetName), password = replacement
        });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        using var loginBody = JsonDocument.Parse(await newLogin.Content.ReadAsStringAsync());
        loginBody.RootElement.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();

        await using var after = _sql.CreateDbContext();
        var persisted = await after.Users.AsNoTracking().SingleAsync(item => item.Id == target.Id);
        persisted.MustChangePassword.Should().BeTrue();
        string.Equals(persisted.PasswordHash, initialHash, StringComparison.Ordinal).Should().BeFalse();
        (await after.Sessions.AsNoTracking().CountAsync(item => item.UserId == target.Id && item.RevokedAt != null)).Should().Be(1);
        (await after.RefreshTokens.AsNoTracking().CountAsync(item => item.Session.UserId == target.Id && item.RevokedAt != null)).Should().Be(1);
        (await after.PasswordResetLogs.CountAsync(item => item.TargetUserId == target.Id && item.PerformedByUserId == supervisor.Id)).Should().Be(1);
        var resetLog = await after.PasswordResetLogs.AsNoTracking().SingleAsync(item => item.TargetUserId == target.Id);
        resetLog.PerformedByUserId.Should().Be(supervisor.Id);
        resetLog.OccurredAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-5));
        (await after.AuditLogs.CountAsync(item => item.EntityId == target.Id && item.EventType == "user_password_reset")).Should().Be(1);
        (await after.IdempotencyRecords.CountAsync(item => item.ActorUserId == supervisor.Id && item.Operation == "AdminPasswordReset")).Should().Be(1);
    }

    [Fact]
    public async Task Refresh_SequentialReuseRevokesRotatedFamily()
    {
        var username = $"rf1001_refresh_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        var original = await AuthenticateAsync(client, username, "Current1!");

        var rotated = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original.Refresh });
        rotated.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await rotated.Content.ReadAsStringAsync());
        var replacement = body.RootElement.GetProperty("refreshToken").GetString()!;
        string.Equals(replacement, original.Refresh, StringComparison.Ordinal).Should().BeFalse();
        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original.Refresh });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        Guid sessionId;
        await using (var revoked = _sql.CreateDbContext())
        {
            var revokedSession = await revoked.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id);
            sessionId = revokedSession.Id;
            revokedSession.RevokedAt.Should().NotBeNull();
            (await revoked.AuditLogs.CountAsync(item => item.EntityId == sessionId && item.EventType == "auth_token_replay_detected"))
                .Should().Be(1);
            (await revoked.IdempotencyRecords.CountAsync(item =>
                item.Operation == "RefreshTokenReplay" && item.OutcomeJson.Contains(sessionId.ToString("N"))))
                .Should().Be(1);
        }
        (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = replacement }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var after = _sql.CreateDbContext();
        var session = await after.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id);
        session.Id.Should().Be(sessionId);
        session.RevokedAt.Should().NotBeNull();
        var tokens = await after.RefreshTokens.AsNoTracking().Where(item => item.SessionId == session.Id).ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Should().OnlyContain(item => item.RevokedAt != null);
        tokens.Select(item => item.TokenHash).Should().OnlyContain(hash => hash.Length == 64);
        (await after.AuditLogs.CountAsync(item => item.EntityId == session.Id && item.EventType == "auth_token_replay_detected"))
            .Should().Be(2);
        (await after.IdempotencyRecords.CountAsync(item =>
            item.Operation == "RefreshTokenReplay" && item.OutcomeJson.Contains(sessionId.ToString("N"))))
            .Should().Be(2);
    }

    [Fact]
    public async Task ReporterOtp_ClockExpiryReuseAndRateGuards_PreserveIntentState()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var sender = new CapturingSender();
        await using var factory = new AuthenticationWebApplicationFactory(
            _sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
                services.RemoveAll<IIdentityMessageSender>();
                services.AddSingleton<IIdentityMessageSender>(sender);
            });
        using var client = factory.CreateClient();

        async Task<Guid> Register()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/reporter-registrations")
            {
                Content = JsonContent.Create(new
                {
                    email = $"rf1001.reporter.{Guid.NewGuid():N}@example.test",
                    password = "Reporter1!",
                    displayName = "Reporter Fixture",
                    reporterType = "CITIZEN"
                })
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.GetProperty("intentId").GetGuid();
        }

        async Task<HttpResponseMessage> Verify(Guid intentId, string otp)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/reporter-registrations/verify")
            {
                Content = JsonContent.Create(new { intentId, otp })
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            return await client.SendAsync(request);
        }

        async Task<HttpResponseMessage> Resend(Guid intentId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/reporter-registrations/resend")
            {
                Content = JsonContent.Create(new { intentId })
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            return await client.SendAsync(request);
        }

        var expiredId = await Register();
        var expiredOtp = sender.LastOtp;
        await using (var issued = _sql.CreateDbContext())
        {
            var intent = await issued.ReporterRegistrationIntents.AsNoTracking().SingleAsync(item => item.Id == expiredId);
            intent.OtpGeneration.Should().Be(1);
            intent.ExpiresAt.Should().Be(clock.GetUtcNow().AddMinutes(10));
            intent.ResendAvailableAt.Should().Be(clock.GetUtcNow().AddSeconds(60));
        }
        clock.Advance(TimeSpan.FromMinutes(10));
        (await Verify(expiredId, expiredOtp)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await using (var expired = _sql.CreateDbContext())
        {
            var intent = await expired.ReporterRegistrationIntents.AsNoTracking().SingleAsync(item => item.Id == expiredId);
            intent.ConsumedAt.Should().BeNull();
            intent.FailedAttempts.Should().Be(0);
            (await expired.Sessions.CountAsync(item => item.UserId == intent.UserId)).Should().Be(0);
        }

        var rateId = await Register();
        var validOtp = sender.LastOtp;
        (await Resend(rateId)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            (await Verify(rateId, "000000")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        (await Verify(rateId, validOtp)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        await using (var rate = _sql.CreateDbContext())
        {
            var intent = await rate.ReporterRegistrationIntents.AsNoTracking().SingleAsync(item => item.Id == rateId);
            intent.FailedAttempts.Should().Be(5);
            intent.ConsumedAt.Should().BeNull();
        }

        var resendId = await Register();
        clock.Advance(TimeSpan.FromSeconds(60));
        for (var resend = 1; resend <= 3; resend++)
        {
            (await Resend(resendId)).StatusCode.Should().Be(HttpStatusCode.Accepted);
            clock.Advance(TimeSpan.FromSeconds(60));
        }
        (await Resend(resendId)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var latestOtp = sender.LastOtp;
        (await Verify(resendId, latestOtp)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Verify(resendId, latestOtp)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await using var final = _sql.CreateDbContext();
        var consumed = await final.ReporterRegistrationIntents.AsNoTracking().SingleAsync(item => item.Id == resendId);
        consumed.OtpGeneration.Should().Be(4);
        consumed.ConsumedAt.Should().NotBeNull();
        (await final.Sessions.CountAsync(item => item.UserId == consumed.UserId)).Should().Be(1);
        (await final.RefreshTokens.CountAsync(item => item.Session.UserId == consumed.UserId)).Should().Be(1);
    }

    private static void AssertReducedActor(JsonElement actor)
    {
        actor.EnumerateObject().Select(item => item.Name).Should().BeEquivalentTo("id", "displayName", "role", "version");
        actor.TryGetProperty("email", out _).Should().BeFalse();
        actor.TryGetProperty("passwordHash", out _).Should().BeFalse();
    }

    private static async Task<(string Access, string Refresh)> AuthenticateAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username), password
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var access = body.RootElement.GetProperty("accessToken").GetString()!;
        var refresh = body.RootElement.GetProperty("refreshToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return (access, refresh);
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }

    private sealed class CapturingSender : IIdentityMessageSender
    {
        public string LastOtp { get; private set; } = string.Empty;
        public Task<bool> SendReporterOtpAsync(string email, string otp, CancellationToken cancellationToken)
        {
            LastOtp = otp;
            return Task.FromResult(true);
        }
        public Task<bool> SendInvitationAsync(
            string email, string displayName, string roleName, string invitationToken,
            DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
