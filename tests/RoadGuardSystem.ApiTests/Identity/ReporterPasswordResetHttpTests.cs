using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Services.Authentication;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ReporterPasswordResetHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ResetIsPurposeBoundSingleUseAndRevokesSessions()
    {
        const string root = "/api/v1/auth/reporter/password-reset/";
        const string oldPassword = "Current1!";
        const string newPassword = "Replacement2!";
        var reporter = await sql.CreateUserAsync($"reset-{Guid.NewGuid():N}", oldPassword, UserRoleCode.Reporter);
        var staff = await sql.CreateUserAsync($"reset-staff-{Guid.NewGuid():N}", oldPassword, UserRoleCode.ProjectManager);
        var unconfirmed = await sql.CreateUserAsync($"reset-unconfirmed-{Guid.NewGuid():N}", oldPassword, UserRoleCode.Reporter);
        await using var db = sql.CreateDbContext();
        await db.Users.Where(user => user.Id == reporter.Id || user.Id == staff.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.EmailConfirmed, true));
        var clock = new ResetClock();
        var sink = new ResetMailSink();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            services.RemoveAll<IIdentityMessageSender>();
            services.AddSingleton<IIdentityMessageSender>(sink);
        });
        using var client = factory.CreateClient();
        async Task<HttpResponseMessage> Request(string email) => await client.PostAsJsonAsync(root + "request", new { email });
        Task<HttpResponseMessage> Confirm(string otp, string password = newPassword) =>
            client.PostAsJsonAsync(root + "confirm", new { email = reporter.Email, otp, newPassword = password });
        Task<HttpResponseMessage> Login(string password) =>
            client.PostAsJsonAsync("/api/v1/auth/login", new { email = reporter.Email, password });

        using var oldLogin = await Login(oldPassword);
        Assert.Equal(HttpStatusCode.OK, oldLogin.StatusCode);
        var credentials = await oldLogin.Content.ReadFromJsonAsync<JsonElement>();
        using var oldClient = factory.CreateClient();
        oldClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await oldClient.GetAsync("/api/v1/me")).StatusCode);

        // Before a reset is issued, an otherwise well-formed registration-purpose OTP cannot reset the account.
        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/reporter-registrations")
        {
            Content = JsonContent.Create(new { email = $"register-{Guid.NewGuid():N}@example.test", password = oldPassword, displayName = "New Reporter", reporterType = "CITIZEN" })
        };
        registration.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var registered = await client.SendAsync(registration);
        Assert.NotNull(sink.RegistrationOtp);
        await AssertCode(await Confirm(sink.RegistrationOtp!), HttpStatusCode.BadRequest, "invalid_reset_otp");

        foreach (var address in new[] { staff.Email!, unconfirmed.Email!, "missing@example.test" })
        {
            using var response = await Request(address);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }
        Assert.Empty(sink.Deliveries);
        using var issue = await Request(reporter.Email!.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.Accepted, issue.StatusCode);
        Assert.Empty(await issue.Content.ReadAsStringAsync());
        Assert.Equal(reporter.Email, Assert.Single(sink.Deliveries).Email);
        using var cooldown = await Request(reporter.Email!);
        Assert.Equal(HttpStatusCode.Accepted, cooldown.StatusCode);
        var firstOtp = Assert.Single(sink.Deliveries).Otp;
        var otherCode = firstOtp == "000000" ? "000001" : "000000";
        for (var attempt = 0; attempt < 5; attempt++)
            await AssertCode(await Confirm(otherCode), HttpStatusCode.BadRequest, "invalid_reset_otp");
        await AssertCode(await Confirm(firstOtp), HttpStatusCode.BadRequest, "invalid_reset_otp");

        clock.Now += TimeSpan.FromMinutes(2);
        Assert.Equal(HttpStatusCode.Accepted, (await Request(reporter.Email!)).StatusCode);
        var expiringOtp = sink.Deliveries[1].Otp;
        clock.Now += TimeSpan.FromMinutes(10);
        await AssertCode(await Confirm(expiringOtp), HttpStatusCode.BadRequest, "invalid_reset_otp");
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await Request(reporter.Email!)).StatusCode);
            clock.Now += TimeSpan.FromMinutes(1);
        }
        Assert.Equal(4, sink.Deliveries.Count);

        var otp = sink.Deliveries[^1].Otp;
        await AssertCode(await Confirm(otp, "a"), HttpStatusCode.UnprocessableEntity, "password_policy_failed");
        using var staffReset = await client.PostAsJsonAsync(root + "confirm", new { email = staff.Email, otp, newPassword });
        Assert.Equal(HttpStatusCode.BadRequest, staffReset.StatusCode);
        var race = await Task.WhenAll(Confirm(otp), Confirm(otp));
        Assert.Single(race, response => response.StatusCode == HttpStatusCode.NoContent);
        await AssertCode(Assert.Single(race, response => response.StatusCode != HttpStatusCode.NoContent),
            HttpStatusCode.BadRequest, "invalid_reset_otp");
        await AssertCode(await Confirm(otp), HttpStatusCode.BadRequest, "invalid_reset_otp");
        Assert.Equal(HttpStatusCode.Unauthorized, (await oldClient.GetAsync("/api/v1/me")).StatusCode);
        using var oldRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = credentials.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(oldPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(newPassword)).StatusCode);
        Assert.True(await db.Sessions.AnyAsync(session => session.UserId == reporter.Id && session.RevokedAt != null));
    }

    private static async Task AssertCode(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        using (response)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }

    private sealed class ResetClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class ResetMailSink : IIdentityMessageSender
    {
        public string? RegistrationOtp { get; private set; }
        public List<(string Email, string Otp)> Deliveries { get; } = [];

        public Task<bool> SendReporterOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            RegistrationOtp = otp;
            return Task.FromResult(true);
        }

        public Task<bool> SendReporterResetOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            Deliveries.Add((email, otp));
            return Task.FromResult(true);
        }

        public Task<bool> SendInvitationAsync(string email, string displayName, string roleName,
            string invitationToken, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
