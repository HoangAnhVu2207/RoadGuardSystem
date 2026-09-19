using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Services.Authentication;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "P1-10")]
public sealed class IdentityCredentialVerifierTests
{
    [Fact(DisplayName = "P1-10 F-07: unknown and known-wrong credentials execute the same password verification boundary")]
    public async Task Verify_UnknownAndKnownWrongPassword_UseSamePasswordHashBoundary()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "known.user",
            NormalizedUserName = "KNOWN.USER",
            DisplayName = "Known User"
        };
        var passwordHasher = new CountingPasswordHasher();
        user.PasswordHash = passwordHasher.HashPassword(user, "Correct1!");
        using var userManager = CreateUserManager(new InMemoryPasswordUserStore(user), passwordHasher);
        var verifier = new IdentityCredentialVerifier(userManager);

        var knownWrong = await verifier.VerifyAsync("known.user", "Wrong1!");
        var knownWrongVerifyCount = passwordHasher.VerifyCount;
        passwordHasher.Reset();

        var unknown = await verifier.VerifyAsync("missing.user", "Wrong1!");

        knownWrong.Status.Should().Be(CredentialVerificationStatus.InvalidCredentials);
        unknown.Status.Should().Be(CredentialVerificationStatus.InvalidCredentials);
        knownWrongVerifyCount.Should().Be(1);
        passwordHasher.VerifyCount.Should().Be(knownWrongVerifyCount);
    }

    private static UserManager<ApplicationUser> CreateUserManager(
        IUserPasswordStore<ApplicationUser> store,
        IPasswordHasher<ApplicationUser> passwordHasher) =>
        new(
            store,
            Options.Create(new IdentityOptions()),
            passwordHasher,
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

    private sealed class CountingPasswordHasher : IPasswordHasher<ApplicationUser>
    {
        private readonly PasswordHasher<ApplicationUser> _inner = new();

        public int VerifyCount { get; private set; }

        public string HashPassword(ApplicationUser user, string password) =>
            _inner.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(
            ApplicationUser user,
            string hashedPassword,
            string providedPassword)
        {
            VerifyCount++;
            return _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }

        public void Reset() => VerifyCount = 0;
    }

    private sealed class InMemoryPasswordUserStore : IUserPasswordStore<ApplicationUser>
    {
        private readonly ApplicationUser _user;

        public InMemoryPasswordUserStore(ApplicationUser user)
        {
            _user = user;
        }

        public Task<ApplicationUser?> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken) =>
            Task.FromResult(string.Equals(
                normalizedUserName,
                _user.NormalizedUserName,
                StringComparison.Ordinal) ? _user : null);

        public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash);

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));

        public Task SetPasswordHashAsync(
            ApplicationUser user,
            string? passwordHash,
            CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(
            ApplicationUser user,
            string? userName,
            CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(
            ApplicationUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(
            ApplicationUser user,
            string? normalizedName,
            CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
