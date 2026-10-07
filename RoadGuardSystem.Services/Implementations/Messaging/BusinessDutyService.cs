using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Messaging;

namespace RoadGuardSystem.Services.Messaging;

public sealed class BusinessDutyService(IBusinessDutyRepository repository)
{
    public Task<BusinessDutyResult> ListAsync(Guid actor, UserRoleCode role, Guid project, Guid? scope, Guid? after, int limit, CancellationToken token)
        => repository.ListAsync(actor, role, project, scope, after, limit, token);
    public Task<BusinessDutyResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid request, CancellationToken token)
        => repository.ReadAsync(actor, role, project, request, token);
    public Task<BusinessDutyResult> ExecuteAsync(Guid actor, UserRoleCode role, Guid project, Guid request, string action,
        string key, string etag, Guid? assignee, string? reason, DateTimeOffset? claimedAt, CancellationToken token)
    {
        if (actor == Guid.Empty || project == Guid.Empty || request == Guid.Empty || string.IsNullOrWhiteSpace(key) ||
            key.Length > 200 || etag.Length < 3 || etag[0] != '"' || etag[^1] != '"' || etag.Contains(','))
            return Task.FromResult(new BusinessDutyResult(400, "validation_error"));
        var version = etag[1..^1];
        try { if (Convert.FromBase64String(version).Length != 8) return Task.FromResult(new BusinessDutyResult(400, "validation_error")); }
        catch (FormatException) { return Task.FromResult(new BusinessDutyResult(400, "validation_error")); }
        return repository.ExecuteAsync(new(actor, role, project, request, action, key, version, assignee, reason, claimedAt), token);
    }
}
