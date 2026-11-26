using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Cases;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.Services.Implementations.Cases;

public sealed class RoutingCursorCodec(JwtOptions options) : IRoutingCursorCodec
{
    private static readonly byte[] Purpose = Encoding.UTF8.GetBytes("RoadGuard:routing-pagination:v1");
    private byte[] Key(string id) => HMACSHA256.HashData(JwtOptionsValidator.DecodeKey(options.SigningKeys[id]), Purpose);
    public string Protect(string value)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var clear = Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[clear.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key(options.ActiveKeyId), 16);
        aes.Encrypt(nonce, clear, ciphertext, tag, Purpose);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(options.ActiveKeyId)) + "." +
            Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray());
    }
    public string Unprotect(string value)
    {
        if (value.Length > 4096) throw new CryptographicException("Invalid routing cursor.");
        var split = value.IndexOf('.');
        if (split <= 0) throw new CryptographicException("Invalid routing cursor.");
        var keyId = Encoding.UTF8.GetString(Convert.FromBase64String(value[..split]));
        if (!options.SigningKeys.ContainsKey(keyId)) throw new CryptographicException("Expired routing cursor key.");
        var bytes = Convert.FromBase64String(value[(split + 1)..]);
        if (bytes.Length < 28) throw new CryptographicException("Invalid routing cursor.");
        var clear = new byte[bytes.Length - 28];
        using var aes = new AesGcm(Key(keyId), 16);
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), clear, Purpose);
        return Encoding.UTF8.GetString(clear);
    }
}

public sealed class RoutingMapService(IRoutingMapRepository repository, IProjectScopeGuard scope,
    IdempotencyOperationService idempotency) : IRoutingMapService
{
    private Func<Guid, CancellationToken, Task<bool>> Access(Guid actor, UserRoleCode role)
        => async (project, token) => await scope.AuthorizeAsync(actor, role, project, token) is not null;
    public Task<RoutingResult> MapAsync(Guid actor, UserRoleCode role, string? bbox, int limit, string? cursor,
        bool history, CancellationToken token)
        => Run(async () => new(200, Value: await repository.MapAsync(actor, role, bbox, limit, cursor, history, token)));
    public Task<RoutingResult> CandidatesAsync(Guid actor, UserRoleCode role, Guid caseId, Guid reportId, Guid? evidenceId,
        int limit, string? cursor, CancellationToken token)
        => Run(async () => new(200, Value: await repository.CandidatesAsync(actor, role, caseId, reportId, evidenceId,
            limit, cursor, Access(actor, role), token)));
    public Task<RoutingResult> HistoryAsync(Guid actor, UserRoleCode role, Guid caseId, int limit, string? cursor, CancellationToken token)
        => Run(async () => new(200, Value: await repository.HistoryAsync(actor, role, caseId, limit, cursor, Access(actor, role), token)));
    public Task<RoutingResult> DecideAsync(Guid actor, UserRoleCode role, Guid caseId, string expectedVersion, string key,
        CaseRoutingDecisionInput input, Guid? correlation, CancellationToken token)
        => Run(async () =>
        {
            if (role != UserRoleCode.Supervisor) return new(403, "access_forbidden");
            if (key.Trim(' ').Length is < 1 or > 200 || key.Any(c => c is < ' ' or > '~')) return new(400, "validation_error");
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { caseId, expectedVersion, input }))).ToLowerInvariant();
            var result = await idempotency.ExecuteSerializableAsync(actor, null, "case.routing.decision.v1", key, fingerprint,
                async ct =>
                {
                    var write = await repository.DecideAsync(actor, role, caseId, expectedVersion, input, correlation, Access(actor, role), ct);
                    return (write.Decision.DecisionId, JsonSerializer.Serialize(write));
                }, token, ct => repository.GuardDecisionAsync(actor, role, caseId, input, Access(actor, role), ct));
            return result.Status == IdempotencyOperationStatus.Conflict ? new(409, "idempotency_key_reused")
                : new(200, Value: JsonSerializer.Deserialize<RoutingWriteView>(result.OutcomeJson));
        });
    private static async Task<RoutingResult> Run(Func<Task<RoutingResult>> action)
    {
        try { return await action(); }
        catch (CaseWorkflowException exception) { return new(exception.Status, exception.Code); }
    }
}
