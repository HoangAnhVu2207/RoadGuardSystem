using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;

namespace RoadGuardSystem.Repositories.Implementations.Defects;

public sealed class DefectTypeAdminRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider clock) : IDefectTypeAdminRepository
{
    private const string AuditSource = "defect-type.admin.v1";
    private const string EntityType = "DefectType";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] AuditFields = ["code", "name", "description", "isActive", "version"];

    public async Task<DefectTypeAdminResult> ListAsync(Guid actor, CancellationToken token)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                await GuardAsync(actor, token);
                var rows = await db.DefectTypes.AsNoTracking().OrderBy(row => row.Code).ToArrayAsync(token);
                var generations = await Events().GroupBy(row => row.EntityId)
                    .Select(group => new { Id = group.Key, Count = group.LongCount() }).ToDictionaryAsync(row => row.Id, row => row.Count, token);
                var views = rows.Select(row => View(row, generations.GetValueOrDefault(EntityId(row.Code)))).ToArray();
                await tx.CommitAsync(token);
                return new DefectTypeAdminResult(200, Items: views);
            });
        }
        catch (Denied error) { return error.Result; }
    }

    public async Task<DefectTypeAdminResult> WriteAsync(Guid actor, DefectTypeAdminWrite command, CancellationToken token)
    {
        try
        {
            var creating = command.ExpectedVersion is null;
            var operation = creating ? "defect-type.create.v1" : "defect-type.update.v1";
            var fingerprint = Hash(JsonSerializer.SerializeToUtf8Bytes(command, Json));
            var result = await receipts.ExecuteSerializableAsync(actor, null, operation, command.Key, fingerprint, async ct =>
            {
                var current = await db.DefectTypes.FromSqlInterpolated(
                    $"SELECT * FROM [DefectTypes] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={command.Code}").SingleOrDefaultAsync(ct);
                if (creating && current is not null) throw new Denied(409, "defect_type_code_exists");
                if (!creating && current is null) throw new Denied(404, "not_found");
                var id = EntityId(current?.Code ?? command.Code);
                var generation = await Events().LongCountAsync(row => row.EntityId == id, ct);
                var before = current is null ? null : View(current, generation);
                if (before is not null && before.Version != command.ExpectedVersion)
                    throw new Denied(409, "concurrency_conflict");

                if (current is null)
                {
                    current = DefectType.Create(command.Code, command.Name, command.Description, command.IsActive);
                    db.DefectTypes.Add(current);
                }
                else
                {
                    current.Update(command.Name, command.Description, command.IsActive);
                }

                // The append-only audit count changes on every accepted write, including changes
                // back to a previous value. Existing seed rows start at generation zero.
                var after = View(current, generation + 1);
                var auditId = Guid.NewGuid();
                db.AuditLogs.Add(AuditLog.Create(auditId, actor, clock.GetUtcNow(), operation,
                    EntityType, id, before is null ? null : JsonSerializer.Serialize(before, Json),
                    JsonSerializer.Serialize(after, Json), command.Reason, AuditSource, auditId, AuditFields));
                await db.SaveChangesAsync(ct);
                return (auditId, JsonSerializer.Serialize(after, Json));
            }, token, ct => GuardAsync(actor, ct));

            return result.Status == IdempotencyOperationStatus.Conflict
                ? new(409, "idempotency_key_reused")
                : new(result.Status == IdempotencyOperationStatus.Replayed || !creating ? 200 : 201,
                    Item: JsonSerializer.Deserialize<DefectTypeAdminFact>(result.OutcomeJson, Json));
        }
        catch (Denied error)
        {
            db.ChangeTracker.Clear();
            return error.Result;
        }
    }

    private IQueryable<AuditLog> Events() => db.AuditLogs.AsNoTracking().Where(row =>
        row.EntityType == EntityType && row.Source == AuditSource);

    private async Task GuardAsync(Guid actor, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(db, actor, null, token);
        if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, UserRoleCode.Supervisor, token))
            throw new Denied(403, "access_forbidden");
    }

    private static Guid EntityId(string code) => new(SHA256.HashData(Encoding.UTF8.GetBytes(code.ToUpperInvariant())).AsSpan(0, 16));

    private static DefectTypeAdminFact View(DefectType row, long generation)
        => new(row.Code, row.Name, row.Description, row.IsActive,
            Hash(JsonSerializer.SerializeToUtf8Bytes(new { row.Code, row.Name, row.Description, row.IsActive, generation }, Json)));

    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private sealed class Denied(int status, string code) : Exception
    {
        public DefectTypeAdminResult Result { get; } = new(status, code);
    }
}