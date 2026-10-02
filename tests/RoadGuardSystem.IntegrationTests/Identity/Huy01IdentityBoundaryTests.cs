using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Identity;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("Package", "HUY-01")]
public sealed class Huy01IdentityBoundaryTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task Otp_59And60Seconds_ThreeResendsAndFourthDenial_UseControlledTime()
    {
        var setup = await SeedAsync(); var start = setup.Now;
        await using var db = sql.CreateDbContext(); var repository = new IdentityOnboardingRepository(db);
        // All keys/fingerprints are isolated to this intent. The fingerprint must be 64 hex chars.
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(setup.Intent.Id.ToByteArray())).ToLowerInvariant();
        async Task<ReporterRegistrationPersistenceResult> Send(DateTimeOffset now, char hash)
            => await repository.ResendReporterOtpAsync(setup.Intent.Id, new string(hash, 64), now.AddMinutes(10), now.AddSeconds(60),
                3, TimeSpan.FromMinutes(15), Guid.NewGuid().ToString(), fingerprint, Guid.NewGuid(), now);
        Assert.Equal(IdentityOnboardingPersistenceStatus.TooManyRequests, (await Send(start.AddSeconds(59), 'b')).Status);
        Assert.Equal(IdentityOnboardingPersistenceStatus.Success, (await Send(start.AddSeconds(60), 'b')).Status);
        Assert.Equal(IdentityOnboardingPersistenceStatus.Success, (await Send(start.AddSeconds(120), 'c')).Status);
        Assert.Equal(IdentityOnboardingPersistenceStatus.Success, (await Send(start.AddSeconds(180), 'd')).Status);
        Assert.Equal(IdentityOnboardingPersistenceStatus.TooManyRequests, (await Send(start.AddSeconds(240), 'e')).Status);
        await using var check = sql.CreateDbContext();
        Assert.Equal(3, await check.IdempotencyRecords.CountAsync(r => r.Operation == "ResendReporterOtp" && r.RequestFingerprint == fingerprint));
        var intent = await check.ReporterRegistrationIntents.SingleAsync(i => i.Id == setup.Intent.Id);
        Assert.Equal(4, intent.OtpGeneration); Assert.Equal(new string('d', 64), intent.OtpHash);
    }

    [Fact]
    public async Task Otp_FifthFailedAttemptBlocksSixth_AndExactExpiryRejectsWithoutSession()
    {
        var setup = await SeedAsync();
        await using var db = sql.CreateDbContext(); var repository = new IdentityOnboardingRepository(db);
        var (session, token) = Credentials(setup.User, setup.Now);
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(IdentityOnboardingPersistenceStatus.InvalidInput, (await repository.VerifyReporterOtpAsync(setup.Intent.Id,
                new string('0', 64), 5, session, token, Guid.NewGuid().ToString(), new string('a', 64), Guid.NewGuid(), setup.Now.AddMinutes(1))).Status);
        Assert.Equal(IdentityOnboardingPersistenceStatus.TooManyRequests, (await repository.VerifyReporterOtpAsync(setup.Intent.Id,
            setup.Intent.OtpHash, 5, session, token, Guid.NewGuid().ToString(), new string('a', 64), Guid.NewGuid(), setup.Now.AddMinutes(1))).Status);
        var expired = await SeedAsync(); var (expiredSession, expiredToken) = Credentials(expired.User, expired.Now);
        Assert.Equal(IdentityOnboardingPersistenceStatus.NotFound, (await repository.VerifyReporterOtpAsync(expired.Intent.Id,
            expired.Intent.OtpHash, 5, expiredSession, expiredToken, Guid.NewGuid().ToString(), new string('a', 64), Guid.NewGuid(), expired.Intent.ExpiresAt)).Status);
        await using var check = sql.CreateDbContext();
        Assert.Equal(5, (await check.ReporterRegistrationIntents.SingleAsync(i => i.Id == setup.Intent.Id)).FailedAttempts);
        Assert.False(await check.Sessions.AnyAsync(s => s.UserId == setup.User || s.UserId == expired.User));
    }

    [Fact]
    public async Task Otp_ConcurrentCorrectVerification_CommitsOneSessionAndOneConsumption()
    {
        var setup = await SeedAsync();
        async Task<ReporterRegistrationPersistenceResult> Verify()
        {
            await using var db = sql.CreateDbContext(); var (session, token) = Credentials(setup.User, setup.Now);
            return await new IdentityOnboardingRepository(db).VerifyReporterOtpAsync(setup.Intent.Id, setup.Intent.OtpHash, 5,
                session, token, Guid.NewGuid().ToString(), new string('a', 64), Guid.NewGuid(), setup.Now.AddSeconds(1));
        }
        var results = await Task.WhenAll(Verify(), Verify());
        Assert.Single(results.Where(r => r.Status == IdentityOnboardingPersistenceStatus.Success));
        await using var check = sql.CreateDbContext();
        Assert.Equal(1, await check.Sessions.CountAsync(s => s.UserId == setup.User));
        Assert.NotNull((await check.ReporterRegistrationIntents.SingleAsync(i => i.Id == setup.Intent.Id)).ConsumedAt);
    }

    private async Task<(Guid User, ReporterRegistrationIntent Intent, DateTimeOffset Now)> SeedAsync()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db);
        var now = DateTimeOffset.UtcNow; var id = Guid.NewGuid(); var email = $"boundary-{id:N}@example.test";
        db.Users.Add(new() { Id = id, UserName = email, NormalizedUserName = email.ToUpperInvariant(), Email = email,
            NormalizedEmail = email.ToUpperInvariant(), DisplayName = "OTP fixture", RoleCode = UserRoleCode.Reporter, Status = UserStatus.Pending,
            SecurityStamp = Guid.NewGuid().ToString(), PasswordHash = "fixture-only", CreatedAt = now });
        var intent = new ReporterRegistrationIntent { Id = Guid.NewGuid(), UserId = id, NormalizedEmail = email.ToUpperInvariant(), ReporterType = ReporterType.Citizen,
            OtpHash = new string('a', 64), OtpGeneration = 1, CreatedAt = now, ExpiresAt = now.AddMinutes(10), ResendAvailableAt = now.AddSeconds(60) };
        db.ReporterRegistrationIntents.Add(intent); await db.SaveChangesAsync(); return (id, intent, now);
    }
    private static (UserSession, RefreshToken) Credentials(Guid user, DateTimeOffset now)
    {
        var session = new UserSession { Id = Guid.NewGuid(), UserId = user, IssuedAt = now, ExpiresAt = now.AddHours(12) };
        return (session, new() { Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Guid.NewGuid().ToByteArray())), ExpiresAt = session.ExpiresAt });
    }
}
