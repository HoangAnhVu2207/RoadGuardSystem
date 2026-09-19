using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;

namespace RoadGuardSystem.Services.Authentication;

public sealed class IdentityCredentialVerifier : ICredentialVerifier
{
    private static readonly string DummyPasswordHash = CreateDummyPasswordHash();
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityCredentialVerifier(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<CredentialVerificationResult> VerifyAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new CredentialVerificationResult(CredentialVerificationStatus.InvalidCredentials);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByNameAsync(username.Trim());
        if (user is null)
        {
            await _userManager.CheckPasswordAsync(
                new ApplicationUser { PasswordHash = DummyPasswordHash },
                password);
            return new CredentialVerificationResult(CredentialVerificationStatus.InvalidCredentials);
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            return new CredentialVerificationResult(CredentialVerificationStatus.InvalidCredentials);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new CredentialVerificationResult(CredentialVerificationStatus.Success, Snapshot(user));
    }

    public async Task<PasswordChangePreparationResult> PrepareForcedPasswordChangeAsync(
        string username,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword))
        {
            return new PasswordChangePreparationResult(PasswordChangePreparationStatus.InvalidCredentials);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByNameAsync(username.Trim());
        if (user is null)
        {
            return new PasswordChangePreparationResult(PasswordChangePreparationStatus.InvalidCredentials);
        }

        if (!await _userManager.CheckPasswordAsync(user, currentPassword))
        {
            var replacementIsCurrent = _userManager.PasswordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash!,
                newPassword) != PasswordVerificationResult.Failed;
            return replacementIsCurrent
                ? new PasswordChangePreparationResult(
                    PasswordChangePreparationStatus.Success,
                    Snapshot(user),
                    user.PasswordHash,
                    user.SecurityStamp ?? Guid.NewGuid().ToString("N"))
                : new PasswordChangePreparationResult(PasswordChangePreparationStatus.InvalidCredentials);
        }

        if (_userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, newPassword) !=
            PasswordVerificationResult.Failed)
        {
            return new PasswordChangePreparationResult(
                PasswordChangePreparationStatus.ReusedPassword,
                ErrorMessage: "The replacement password must differ from the current password.");
        }

        foreach (var validator in _userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(_userManager, user, newPassword);
            if (!validation.Succeeded)
            {
                return new PasswordChangePreparationResult(
                    PasswordChangePreparationStatus.PolicyRejected,
                    ErrorMessage: "The replacement password does not satisfy the password policy.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new PasswordChangePreparationResult(
            PasswordChangePreparationStatus.Success,
            Snapshot(user),
            _userManager.PasswordHasher.HashPassword(user, newPassword),
            Guid.NewGuid().ToString("N"));
    }

    private static UserSecurityState Snapshot(ApplicationUser user) => new(
        user.Id,
        user.UserName!,
        user.DisplayName,
        user.RoleCode,
        user.Status,
        user.MustChangePassword,
        user.RowVersion.ToArray());

    private static string CreateDummyPasswordHash()
    {
        var dummyUser = new ApplicationUser();
        var dummyPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return new PasswordHasher<ApplicationUser>().HashPassword(dummyUser, dummyPassword);
    }
}
