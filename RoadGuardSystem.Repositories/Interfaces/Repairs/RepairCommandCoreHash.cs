using System.Security.Cryptography;
using System.Text.Json;

namespace RoadGuardSystem.Repositories.Repairs;

/// <summary>Version-one typed H4 canonical payload. Project remains the authoritative registry partition.
/// This does not verify actor, assignment, signature, chronology or execution permission.</summary>
public static class RepairCommandCoreHash
{
    public static string Assessment(Guid taskId, Guid itemId, Guid originalActorId, RepairMeasurementAssessmentData input)
        => Compute("REPAIR_ASSESSMENT", taskId, itemId, originalActorId, input);
    public static string ExecutionStart(Guid taskId, Guid itemId, Guid originalActorId, RepairExecutionStartData input)
        => Compute("REPAIR_EXECUTION_START", taskId, itemId, originalActorId, input);
    public static string ExecutionFinish(Guid taskId, Guid itemId, Guid originalActorId, RepairExecutionFinishData input)
        => Compute("REPAIR_EXECUTION_FINISH", taskId, itemId, originalActorId, input);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private static string Compute<T>(string kind, Guid taskId, Guid itemId, Guid originalActorId, T input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, kind, taskId, itemId, originalActorId, input }, Options);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
