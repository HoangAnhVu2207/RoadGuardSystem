using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Authentication;
using Xunit;

namespace RoadGuardSystem.ApiTests.Sandbox;

// Actual SMTP capture with a test transport; Compose GmailIdentityMessageSender
// wiring and native clients are separate acceptance dimensions.
[Collection(AuthenticationApiFixture.Name)]
public sealed class SandboxMailpitTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ReporterOtpTravelsThroughMailpitAndVerificationCommitsOnce()
    {
        sql.GetOwnedSeedDatabaseIdentity();
        await using var mail = new ContainerBuilder("axllent/mailpit@sha256:23fe39e81bde596a918000b3135c0f85dc8bb4c9ea6be274b051143f3e300387")
            .WithPortBinding(1025, true).WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1025))
            .Build();
        await mail.StartAsync();
        using var capture = new HttpClient { BaseAddress = new Uri($"http://{mail.Hostname}:{mail.GetMappedPublicPort(8025)}"), Timeout = TimeSpan.FromSeconds(5) };
        var sender = new LocalCaptureTransport(mail.Hostname, mail.GetMappedPublicPort(1025));
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<IIdentityMessageSender>(); services.AddSingleton<IIdentityMessageSender>(sender); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var email = $"sbx-{Guid.NewGuid():N}@example.test";
        var password = $"Synthetic{Guid.NewGuid():N}1!";
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var request = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations", new
        { email, password, displayName = "Synthetic Mailpit Reporter", reporterType = "CITIZEN" });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var intent = (await request.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("intentId").GetGuid();
        string? otp = null;
        for (var i = 0; i < 10 && otp is null; i++)
        {
            var messages = await capture.GetFromJsonAsync<JsonElement>("/api/v1/messages");
            foreach (var message in messages.GetProperty("messages").EnumerateArray())
            {
                var detail = await capture.GetFromJsonAsync<JsonElement>("/api/v1/message/" + message.GetProperty("ID").GetString());
                if (!detail.GetProperty("To").EnumerateArray().Any(x => string.Equals(x.GetProperty("Address").GetString(), email, StringComparison.OrdinalIgnoreCase))) continue;
                var match = Regex.Match(detail.GetProperty("Text").GetString() ?? "", @"\b[0-9]{6}\b");
                if (match.Success) otp = match.Value;
            }
            if (otp is null) await Task.Delay(100);
        }
        Assert.True(otp is not null, "Isolated Mailpit must contain the synthetic registration OTP.");
        async Task<HttpStatusCode> Verify(string code, string key)
        {
            client.DefaultRequestHeaders.Remove("Idempotency-Key");
            client.DefaultRequestHeaders.Add("Idempotency-Key", key);
            using var response = await client.PostAsJsonAsync("/api/v1/auth/reporter-registrations/verify", new { intentId = intent, otp = code });
            return response.StatusCode;
        }
        var invalid = otp == "000000" ? "000001" : "000000";
        Assert.Equal(HttpStatusCode.BadRequest, await Verify(invalid, Guid.NewGuid().ToString("N")));
        await using (var rejectedDb = sql.CreateDbContext())
        {
            var rejected = await rejectedDb.ReporterRegistrationIntents.AsNoTracking().SingleAsync(x => x.Id == intent);
            Assert.Equal(1, rejected.FailedAttempts);
            Assert.Null(rejected.ConsumedAt);
            Assert.Null(rejected.EmailConfirmedAt);
            Assert.False((await rejectedDb.Users.AsNoTracking().SingleAsync(x => x.Id == rejected.UserId)).EmailConfirmed);
            Assert.Equal(0, await rejectedDb.Sessions.CountAsync(x => x.UserId == rejected.UserId));
        }
        var key = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, await Verify(otp!, key));
        await using var db = sql.CreateDbContext();
        var persisted = await db.ReporterRegistrationIntents.AsNoTracking().SingleAsync(x => x.Id == intent);
        Assert.NotNull(persisted.ConsumedAt);
        Assert.NotNull(persisted.EmailConfirmedAt);
        Assert.NotEqual(otp, persisted.OtpHash);
        var sessions = await db.Sessions.CountAsync(x => x.UserId == persisted.UserId);
        Assert.Equal(1, sessions);
        Assert.Equal(HttpStatusCode.OK, await Verify(otp!, key));
        Assert.Equal(sessions, await db.Sessions.CountAsync(x => x.UserId == persisted.UserId));
        Assert.Equal(HttpStatusCode.NotFound, await Verify(otp!, Guid.NewGuid().ToString("N")));
        Assert.True((await db.Users.AsNoTracking().SingleAsync(x => x.Id == persisted.UserId)).EmailConfirmed);
    }

    private sealed class LocalCaptureTransport(string host, int port) : IIdentityMessageSender
    {
        public async Task<bool> SendReporterOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            // Endpoints originate solely from this owned Testcontainer, never ambient SMTP settings.
            using var message = new MailMessage("sandbox@example.test", email, "Synthetic OTP", otp);
            using var smtp = new SmtpClient(host, port) { EnableSsl = false };
            await smtp.SendMailAsync(message, cancellationToken);
            return true;
        }
        public Task<bool> SendInvitationAsync(string email, string displayName, string roleName,
            string invitationToken, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No invitation/external transport in this fixture.");
    }
}
