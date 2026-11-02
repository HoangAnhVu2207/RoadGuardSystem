using System.Text.RegularExpressions;
using RoadGuardSystem.Repositories.Identity;

namespace RoadGuardSystem.Services.Identity;

public sealed class StaffPhoneNumberService(IIdentityRepository repository) : IStaffPhoneNumberService
{
    public async Task<SelfPhoneNumberResult> GetSelfAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var row = await repository.GetSelfPhoneNumberAsync(actorUserId, cancellationToken);
        return row is null ? new(PhoneNumberUpdateStatus.Forbidden) :
            new(PhoneNumberUpdateStatus.Success, row.Value.PhoneNumber, row.Value.Confirmed,
                Convert.ToBase64String(row.Value.Version));
    }

    public async Task<PhoneNumberUpdateResult> UpdateAsync(Guid actorUserId, Guid targetUserId, string? phoneNumber,
        string? ifMatch, string? idempotencyKey, CancellationToken cancellationToken = default)
        => await UpdateCoreAsync(actorUserId, targetUserId, phoneNumber, ifMatch, idempotencyKey, false, cancellationToken);

    public async Task<PhoneNumberUpdateResult> UpdateSelfAsync(Guid actorUserId, string? phoneNumber,
        string? ifMatch, string? idempotencyKey, CancellationToken cancellationToken = default)
        => await UpdateCoreAsync(actorUserId, actorUserId, phoneNumber, ifMatch, idempotencyKey, true, cancellationToken);

    private async Task<PhoneNumberUpdateResult> UpdateCoreAsync(Guid actorUserId, Guid targetUserId, string? phoneNumber,
        string? ifMatch, string? idempotencyKey, bool self, CancellationToken cancellationToken)
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
        var status = self
            ? await repository.UpdateSelfPhoneNumberAtomicAsync(actorUserId, normalized, version, idempotencyKey.Trim(), cancellationToken)
            : await repository.UpdateStaffPhoneNumberAtomicAsync(actorUserId, targetUserId,
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
