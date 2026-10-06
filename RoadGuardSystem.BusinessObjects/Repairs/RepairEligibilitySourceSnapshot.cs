using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Warranties;

namespace RoadGuardSystem.BusinessObjects.Repairs;

public sealed record RepairWarrantySource(Guid Id, Guid ProjectId, Guid? RoadSectionId, Guid? HandoverDocumentId,
    DateOnly HandoverDate, DateOnly WarrantyStartDate, DateOnly WarrantyEndDate, WarrantyScope Scope,
    WarrantyStatus Status, Guid? SourceDocumentId);
public sealed record RepairHandoverSource(Guid Id, Guid ProjectId, string DocumentNo, DateOnly HandoverDate,
    Guid? AcceptedByUserId, Guid? FileId, string RowVersion);

/// <summary>Actual existing scoped raw sources. Project handover to a specific road and coverage interpretation
/// remain unadopted; these values alone never grant FT execution or recreate maintenance management.</summary>
public sealed class RepairEligibilitySourceSnapshot
{
    public Guid ProjectId { get; private set; }
    public Guid RoadSectionId { get; private set; }
    public IReadOnlyList<RepairWarrantySource> Warranties { get; private set; } = [];
    public IReadOnlyList<RepairHandoverSource> Handovers { get; private set; } = [];
    public RepairFactState RoadHandover { get; private set; } = RepairFactState.Unknown;
    public RepairFactState Coverage { get; private set; } = RepairFactState.Unknown;
    public string SourceMapping { get; private set; } = "UNKNOWN_OWNER_MAPPING";
    private RepairEligibilitySourceSnapshot() { }
    public static RepairEligibilitySourceSnapshot Capture(Guid project, Guid roadSection, IReadOnlyList<Warranty> warranties,
        IReadOnlyList<HandoverDocument> handovers)
    {
        RepairGuards.Id(project); RepairGuards.Id(roadSection);
        ArgumentNullException.ThrowIfNull(warranties); ArgumentNullException.ThrowIfNull(handovers);
        if (warranties.Any(row => row is null || row.ProjectId != project || row.RoadSectionId is not null && row.RoadSectionId != roadSection) ||
            handovers.Any(row => row is null || row.ProjectId != project) ||
            warranties.Select(row => row.Id).Distinct().Count() != warranties.Count ||
            handovers.Select(row => row.Id).Distinct().Count() != handovers.Count)
            throw new InvalidOperationException("Raw eligibility sources must be unique and belong to the actual project/road scope.");
        return new RepairEligibilitySourceSnapshot
        {
            ProjectId = project,
            RoadSectionId = roadSection,
            Warranties = Array.AsReadOnly(warranties.Select(row => new RepairWarrantySource(row.Id, row.ProjectId, row.RoadSectionId,
                row.HandoverDocumentId, row.HandoverDate, row.WarrantyStartDate, row.WarrantyEndDate, row.Scope, row.Status, row.SourceDocumentId)).ToArray()),
            Handovers = Array.AsReadOnly(handovers.Select(row => new RepairHandoverSource(row.Id, row.ProjectId, row.DocumentNo,
                row.HandoverDate, row.AcceptedByUserId, row.FileId, Convert.ToBase64String(row.RowVersion))).ToArray())
        };
    }
}
