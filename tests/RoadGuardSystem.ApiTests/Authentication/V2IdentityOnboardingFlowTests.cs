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

namespace RoadGuardSystem.ApiTests.Authentication;

[Trait("TaskId", "V2-P1-006..010")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class V2IdentityOnboardingFlowTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public V2IdentityOnboardingFlowTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task ReporterRegistration_VerifiesOneTimeOtpAndIssuesToken()
    {
        var sender = new CapturingIdentityMessageSender();
        await using var factory = Factory(sender);
        using var client = factory.CreateClient();
        var email = $"reporter.{Guid.NewGuid():N}@gmail.com";
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var registration = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations", new
        {
            email,
            password = "Reporter1!",
            displayName = "Road Reporter",
            reporterType = "CITIZEN"
        });
        var intent = await registration.Content.ReadFromJsonAsync<JsonElement>();

        registration.StatusCode.Should().Be(HttpStatusCode.Accepted);
        registration.Headers.Location.Should().NotBeNull();
        sender.Otp.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var verified = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations/verify", new
        {
            intentId = intent.GetProperty("intentId").GetGuid(),
            otp = sender.Otp
        });

        verified.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await verified.Content.ReadFromJsonAsync<JsonElement>();
        tokens.GetProperty("user").GetProperty("role").GetString().Should().Be("REPORTER");
        await using var verification = _sql.CreateDbContext();
        var normalizedEmail = email.ToUpperInvariant();
        var user = await verification.Users.AsNoTracking().SingleAsync(item => item.NormalizedEmail == normalizedEmail);
        user.Status.Should().Be(UserStatus.Active);
        user.EmailConfirmed.Should().BeTrue();
        (await verification.ReporterRegistrationIntents.AsNoTracking()
            .SingleAsync(item => item.Id == intent.GetProperty("intentId").GetGuid())).OtpHash
            .Should().NotBe(sender.Otp);
    }

    [Fact]
    public async Task Invitation_SupervisorCreatesAndRecipientAcceptsOnce()
    {
        var supervisorName = $"invite_supervisor_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(supervisorName, "Supervisor1!", UserRoleCode.Supervisor);
        var sender = new CapturingIdentityMessageSender();
        await using var factory = Factory(sender);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, supervisorName, "Supervisor1!");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var email = $"invitee.{Guid.NewGuid():N}@example.test";

        var created = await client.PostAsJsonAsync("/api/v1/invitations", new
        {
            email,
            displayName = "Invited Operator",
            role = "OPERATOR",
            projectIds = Array.Empty<Guid>()
        });

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        created.Headers.ETag.Should().NotBeNull();
        sender.InvitationToken.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var accepted = await client.PostAsJsonAsync("/api/v1/invitations/accept", new
        {
            invitationToken = sender.InvitationToken,
            displayName = "Invited Operator",
            password = "Operator1!"
        });

        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("user").GetProperty("role").GetString().Should().Be("OPERATOR");
        (await client.PostAsJsonAsync("/api/v1/invitations/accept", new
        {
            invitationToken = sender.InvitationToken,
            displayName = "Invited Operator",
            password = "Operator1!"
        })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private AuthenticationWebApplicationFactory Factory(CapturingIdentityMessageSender sender) =>
        new(_sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IIdentityMessageSender>();
            services.AddSingleton<IIdentityMessageSender>(sender);
        });

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
    }

    private sealed class CapturingIdentityMessageSender : IIdentityMessageSender
    {
        public string? Otp { get; private set; }

        public string? InvitationToken { get; private set; }

        public Task<bool> SendReporterOtpAsync(
            string email,
            string otp,
            CancellationToken cancellationToken = default)
        {
            Otp = otp;
            return Task.FromResult(true);
        }

        public Task<bool> SendInvitationAsync(
            string email,
            string displayName,
            string roleName,
            string invitationToken,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            InvitationToken = invitationToken;
            return Task.FromResult(true);
        }
    }
}
