using RoadGuardSystem.BusinessObjects.Repairs;
namespace RoadGuardSystem.DTOs.Repairs;
public sealed record RepairEligibilityReadView(bool Eligible, string Activation, Guid ItemId, Guid BindingId,
    Guid? PolicyRevisionId, string? PolicyContentHash, string PolicyState, RepairMeasurementRule[] Rules,
    Guid? AssessmentId, Guid[] MeasurementIds, DateTimeOffset? OriginalVerifiedStart, DateTimeOffset? ExecutionExpiresAt,
    string SourceMapping, RepairWarrantySource[] WarrantySources, RepairHandoverSource[] HandoverSources,
    string[] MissingReasons, string Version);
public sealed record RepairEligibilityServiceResult(int Status, string? Code = null, RepairEligibilityReadView? Value = null);
