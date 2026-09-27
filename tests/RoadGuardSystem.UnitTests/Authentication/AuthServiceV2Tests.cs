using FluentAssertions;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Authentication;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

public sealed partial class AuthServiceTests
{
    [Fact]
    public async Task PasswordRecovery_NormalizesEmailBeforePersistence()
    {
        var repository = new StubIdentityRepository();
        var service = CreateService(repository, new StubCredentialVerifier());

        var result = await service.RequestPasswordRecoveryAsync(
            new PasswordRecoveryCommand(" Field.User@Example.Test "));

        result.RequestId.Should().NotBeEmpty();
        repository.RecoveryEmail.Should().Be("FIELD.USER@EXAMPLE.TEST");
    }

    [Fact]
    public async Task Logout_ChangedPayloadForSameKey_ReturnsIdempotencyConflict()
    {
        var repository = new StubIdentityRepository
        {
            LogoutAtomicResult = new LogoutPersistenceResult(IdempotentConflict: true)
        };
        var service = CreateService(repository, new StubCredentialVerifier());

        var result = await service.LogoutAsync(
            Guid.NewGuid(), Guid.NewGuid(), "same-key", Guid.NewGuid());

        result.Status.Should().Be(AuthStatus.IdempotentConflict);
    }

    [Fact]
    public async Task ChangePassword_DurableSuccess_ReturnsSuccess()
    {
        var user = ActiveUser();
        var repository = new StubIdentityRepository();
        var verifier = new StubCredentialVerifier
        {
            PasswordPreparation = new PasswordChangePreparationResult(
                PasswordChangePreparationStatus.Success,
                user,
                "replacement-hash",
                "replacement-stamp")
        };
        var service = CreateService(repository, verifier);

        var result = await service.ChangePasswordAsync(new ChangePasswordCommand(
            user.Id,
            "Current1!",
            "Replacement2!",
            "change-key"));

        result.Status.Should().Be(AuthStatus.Success);
    }

    [Fact]
    public async Task ChangePassword_ReusedKeyWithDifferentPayload_ReturnsConflictBeforeCredentialCheck()
    {
        var repository = new StubIdentityRepository { IdempotencyFingerprint = new string('a', 64) };
        var service = CreateService(repository, new StubCredentialVerifier());

        var result = await service.ChangePasswordAsync(new ChangePasswordCommand(
            Guid.NewGuid(),
            "wrong-current",
            "different-payload",
            "existing-key"));

        result.Status.Should().Be(AuthStatus.IdempotentConflict);
    }
}
