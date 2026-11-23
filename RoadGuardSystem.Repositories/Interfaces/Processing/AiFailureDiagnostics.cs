using RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing;

namespace RoadGuardSystem.Repositories.Processing;

// Server-owned vocabulary only. Never expose exception messages, paths or supplied payloads.
public static class AiFailureDiagnostics
{
    public static AiFailureDiagnosticFact Describe(AiRequestException error, Guid? correlation = null)
    {
        var reason = error.ReasonCode ?? (error.Code switch { "mock_fixture_source_mismatch" => "fixture_source_mismatch", "source_not_ready" => "source_lineage_invalid", "candidate_stale" => "source_changed", "not_found" => "resource_not_found", "access_forbidden" => "access_not_current", "result_not_ready" => "result_pending", _ => "request_invalid" });
        var recovery = reason switch
        {
            "model_not_released" => "Select a released model version approved for this workflow.",
            "resource_not_found" or "access_not_current" => "Check the current session, project and resource permission without changing resource IDs to bypass scope.",
            "result_pending" => "Read the run status again; a result is available only after successful processing.",
            "fixture_version_unsupported" => "Select a supported, versioned MOCK fixture.",
            "fixture_missing" or "fixture_hash_mismatch" => "Restore the approved fixture files and verify their published hashes on the backend.",
            "fixture_source_mismatch" or "source_hash_mismatch" => "Use the approved synthetic video bytes for MOCK; verify the uploaded source and checksum.",
            "source_lineage_invalid" or "source_changed" => "Reload the dataset, scope and current geometry; select sources from the same project and dataset.",
            "manifest_hash_mismatch" or "manifest_schema_invalid" => "Inspect the run provenance using its IDs and correlation ID; do not edit the stored manifest.",
            "result_schema_invalid" or "result_hash_mismatch" or "artifact_integrity_mismatch" => "Inspect the versioned mock result/artifact producer; retain run IDs for diagnosis.",
            _ => "Validate request fields against the API contract and reload the authoritative source."
        };
        return new(reason, error.Step ?? "admission", error.Field, recovery, correlation);
    }
}
