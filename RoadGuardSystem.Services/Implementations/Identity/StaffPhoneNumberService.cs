using System.Text.RegularExpressions;
using RoadGuardSystem.Repositories.Identity;

namespace RoadGuardSystem.Services.Identity;

public sealed class StaffPhoneNumberService(IIdentityRepository repository) : IStaffPhoneNumberService
{
    public async Task<PhoneNumberUpdateResult> UpdateAsync(Guid actorUserId, Guid targetUserId, string? phoneNumber,
        string? ifMatch, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ifMatch)) return new(PhoneNumberUpdateStatus.PreconditionRequired);
        if (ifMatch.StartsWith("W/", StringComparison.OrdinalIgnoreCase) || ifMatch.Length < 3 ||
            ifMatch[0] != '"' || ifMatch[^1] != '"' || !TryDecodeVersion(ifMatch[1..^1], out var version))
            return new(PhoneNumberUpdateStatus.InvalidInput);
        var normalized = phoneNumber?.Trim();
        if (actorUserId == Guid.Empty || targetUserId == Guid.Empty || normalized is null ||
            string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 200 ||
            !Regex.IsMatch(normalized, @"^\+?[0-9]{7,15}$", RegexOptions.CultureInvariant))
            return new(PhoneNumberUpdateStatus.InvalidInput);
        var status = await repository.UpdateStaffPhoneNumberAtomicAsync(actorUserId, targetUserId,
            normalized, version, idempotencyKey.Trim(), cancellationToken);
        return new(status switch
        {
            UserPhoneNumberWriteStatus.Success => PhoneNumberUpdateStatus.Success,
            UserPhoneNumberWriteStatus.Replay => PhoneNumberUpdateStatus.Replay,
            UserPhoneNumberWriteStatus.Forbidden => PhoneNumberUpdateStatus.Forbidden,
            UserPhoneNumberWriteStatus.NotFound => PhoneNumberUpdateStatus.NotFound,
            UserPhoneNumberWriteStatus.StaleVersion => PhoneNumberUpdateStatus.PreconditionFailed,
            _ => PhoneNumberUpdateStatus.KeyConflict
        });
    }

    private static bool TryDecodeVersion(string value, out byte[] version)
    {
        version = [];
        try
        {
            version = Convert.FromBase64String(value);
            return version.Length > 0;
        }
        catch (FormatException) { return false; }
    }
}
