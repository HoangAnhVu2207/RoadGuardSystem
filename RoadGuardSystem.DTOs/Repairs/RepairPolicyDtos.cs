using System.Text.Json.Serialization;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.DTOs.Repairs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairPolicyDefinitionInput(string DefectTypeCode, string ChecklistVersion,
    RepairMeasurementRule[] Measurements, string[] StopConditions, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairPolicyReasonInput(string Reason);
public sealed record RepairPolicyView(Guid Id, Guid ProjectId, Guid? CurrentChangeId, Guid? PublishedRevisionId,
    int? Revision, string DefectTypeCode, string ChecklistVersion, RepairMeasurementRule[] Measurements,
    string[] StopConditions, string State, Guid ActorId, DateTimeOffset At, string Reason, string Version);
public sealed record RepairPolicyServiceResult(int Status, string? Code = null, RepairPolicyView? Value = null, bool Replayed = false);
