using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Messaging;

namespace RoadGuardSystem.Services.Messaging;

public sealed class ClockDutyService(IClockDutyRepository repository)
{
    public Task<BusinessDutyResult> ExecuteAsync(Guid actor, UserRoleCode role, Guid project, Guid clock, string action,
        string key, string etag, string reason, DateTimeOffset? due, Guid? assignee, CancellationToken token)
    {
        if (actor == Guid.Empty || project == Guid.Empty || clock == Guid.Empty || string.IsNullOrWhiteSpace(key) ||
            key.Length > 200 || etag.Length < 3 || etag[0] != '"' || etag[^1] != '"' || etag.Contains(','))
            return Task.FromResult(new BusinessDutyResult(400, "validation_error"));
        var version = etag[1..^1];
        try { if (Convert.FromBase64String(version).Length != 8) return Task.FromResult(new BusinessDutyResult(400, "validation_error")); }
        catch (FormatException) { return Task.FromResult(new BusinessDutyResult(400, "validation_error")); }
        return repository.ExecuteAsync(new(actor, role, project, clock, action, key, version, reason, due, assignee), token);
    }
}
