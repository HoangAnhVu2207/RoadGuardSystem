namespace RoadGuardSystem.Services.Identity;

public enum PhoneNumberUpdateStatus { Success, Replay, Forbidden, NotFound, InvalidInput, PreconditionRequired, PreconditionFailed, KeyConflict }
public sealed record PhoneNumberUpdateResult(PhoneNumberUpdateStatus Status);

public interface IStaffPhoneNumberService
{
    Task<PhoneNumberUpdateResult> UpdateAsync(Guid actorUserId, Guid targetUserId, string? phoneNumber,
        string? ifMatch, string? idempotencyKey, CancellationToken cancellationToken = default);
}
