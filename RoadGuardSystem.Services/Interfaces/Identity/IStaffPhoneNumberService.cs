namespace RoadGuardSystem.Services.Identity;

public enum PhoneNumberUpdateStatus { Success, Replay, Forbidden, NotFound, InvalidInput, PreconditionRequired, PreconditionFailed, KeyConflict }
public sealed record PhoneNumberUpdateResult(PhoneNumberUpdateStatus Status);
public sealed record SelfPhoneNumberResult(PhoneNumberUpdateStatus Status, string? PhoneNumber = null,
    bool PhoneNumberConfirmed = false, string? Version = null);

public interface IStaffPhoneNumberService
{
    Task<PhoneNumberUpdateResult> UpdateAsync(Guid actorUserId, Guid targetUserId, string? phoneNumber,
        string? ifMatch, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<SelfPhoneNumberResult> GetSelfAsync(Guid actorUserId, CancellationToken cancellationToken = default);
    Task<PhoneNumberUpdateResult> UpdateSelfAsync(Guid actorUserId, string? phoneNumber,
        string? ifMatch, string? idempotencyKey, CancellationToken cancellationToken = default);
}
