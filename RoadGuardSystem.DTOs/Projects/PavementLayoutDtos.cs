namespace RoadGuardSystem.DTOs.Projects;

public sealed record PavementWidthInterval(decimal FromOffsetMeters, decimal ToOffsetMeters, decimal WidthMeters)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementWidthIntervalFact?(PavementWidthInterval? value)
        => value is null ? null! : new(value.FromOffsetMeters, value.ToOffsetMeters, value.WidthMeters);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator PavementWidthInterval?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementWidthIntervalFact? value)
        => value is null ? null! : new(value.FromOffsetMeters, value.ToOffsetMeters, value.WidthMeters);
}
public sealed record PavementLayoutInput(decimal StripWidthMeters, decimal SlabLengthMeters, PavementWidthInterval[] WidthProfile)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementLayoutInputFact?(PavementLayoutInput? value)
        => value is null ? null! : new(value.StripWidthMeters, value.SlabLengthMeters, value.WidthProfile?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementWidthIntervalFact)item).ToArray()!);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator PavementLayoutInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementLayoutInputFact? value)
        => value is null ? null! : new(value.StripWidthMeters, value.SlabLengthMeters, value.WidthProfile?.Select(item => (global::RoadGuardSystem.DTOs.Projects.PavementWidthInterval)item).ToArray()!);
}
public sealed record PlannedSlabCell(int Sequence, int LongitudinalRow, int Strip, decimal FromOffsetMeters,
    decimal ToOffsetMeters, decimal FromLateralMeters, decimal ToLateralMeters, bool IsTerminalResidual,
    bool IsWidthTransitionFragment)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PlannedSlabCellFact?(PlannedSlabCell? value)
        => value is null ? null! : new(value.Sequence, value.LongitudinalRow, value.Strip, value.FromOffsetMeters, value.ToOffsetMeters, value.FromLateralMeters, value.ToLateralMeters, value.IsTerminalResidual, value.IsWidthTransitionFragment);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator PlannedSlabCell?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PlannedSlabCellFact? value)
        => value is null ? null! : new(value.Sequence, value.LongitudinalRow, value.Strip, value.FromOffsetMeters, value.ToOffsetMeters, value.FromLateralMeters, value.ToLateralMeters, value.IsTerminalResidual, value.IsWidthTransitionFragment);
}
public sealed record PavementLayoutPreview(decimal AnalyticLengthMeters, decimal StripWidthMeters,
    decimal SlabLengthMeters, PlannedSlabCell[] Cells, string[] Warnings)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementLayoutPreviewFact?(PavementLayoutPreview? value)
        => value is null ? null! : new(value.AnalyticLengthMeters, value.StripWidthMeters, value.SlabLengthMeters, value.Cells?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PlannedSlabCellFact)item).ToArray()!, value.Warnings);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator PavementLayoutPreview?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementLayoutPreviewFact? value)
        => value is null ? null! : new(value.AnalyticLengthMeters, value.StripWidthMeters, value.SlabLengthMeters, value.Cells?.Select(item => (global::RoadGuardSystem.DTOs.Projects.PlannedSlabCell)item).ToArray()!, value.Warnings);
}
public sealed record PavementPlanCreateInput(PavementLayoutInput Layout, double DisplayToleranceMeters)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementPlanCreateInputFact?(PavementPlanCreateInput? value)
        => value is null ? null! : new((global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementLayoutInputFact)value.Layout, value.DisplayToleranceMeters);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator PavementPlanCreateInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementPlanCreateInputFact? value)
        => value is null ? null! : new((global::RoadGuardSystem.DTOs.Projects.PavementLayoutInput)value.Layout, value.DisplayToleranceMeters);
}
public sealed record SlabGeometrySnapshot(string Key, int? PlannedSequence, GeometryPoint[] Footprint,
    double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,
    Guid? SourceFileId = null, string ProvenanceStatus = "CLAIMED_CUSTOM")
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SlabGeometrySnapshotFact?(SlabGeometrySnapshot? value)
        => value is null ? null! : new(value.Key, value.PlannedSequence, value.Footprint?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)item).ToArray()!, value.LengthMeters, value.WidthMeters, value.DimensionUnknownReason, value.Source, value.SourceFileId, value.ProvenanceStatus);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator SlabGeometrySnapshot?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SlabGeometrySnapshotFact? value)
        => value is null ? null! : new(value.Key, value.PlannedSequence, value.Footprint?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)item).ToArray()!, value.LengthMeters, value.WidthMeters, value.DimensionUnknownReason, value.Source, value.SourceFileId, value.ProvenanceStatus);
}
public sealed record AsBuiltSlabInput(string Key, int? PlannedSequence, GeometryPoint[] Footprint,
    double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,
    Guid? SourceFileId = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.AsBuiltSlabInputFact?(AsBuiltSlabInput? value)
        => value is null ? null! : new(value.Key, value.PlannedSequence, value.Footprint?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)item).ToArray()!, value.LengthMeters, value.WidthMeters, value.DimensionUnknownReason, value.Source, value.SourceFileId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator AsBuiltSlabInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.AsBuiltSlabInputFact? value)
        => value is null ? null! : new(value.Key, value.PlannedSequence, value.Footprint?.Select(item => (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)item).ToArray()!, value.LengthMeters, value.WidthMeters, value.DimensionUnknownReason, value.Source, value.SourceFileId);
}
public sealed record AsBuiltLayoutInput(Guid SourcePlanId, AsBuiltSlabInput[] Slabs, string Reason)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.AsBuiltLayoutInputFact?(AsBuiltLayoutInput? value)
        => value is null ? null! : new(value.SourcePlanId, value.Slabs?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.AsBuiltSlabInputFact)item).ToArray()!, value.Reason);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator AsBuiltLayoutInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.AsBuiltLayoutInputFact? value)
        => value is null ? null! : new(value.SourcePlanId, value.Slabs?.Select(item => (global::RoadGuardSystem.DTOs.Projects.AsBuiltSlabInput)item).ToArray()!, value.Reason);
}
public sealed record PavementGeometryPreview(PavementLayoutPreview? Plan, SlabGeometrySnapshot[] Slabs,
    string[] Warnings, double DisplayToleranceMeters, double RenderedGapAreaSquareMeters = 0,
    double RenderedOverlapAreaSquareMeters = 0)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementGeometryPreviewFact?(PavementGeometryPreview? value)
        => value is null ? null! : new((global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementLayoutPreviewFact?)value.Plan, value.Slabs?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SlabGeometrySnapshotFact)item).ToArray()!, value.Warnings, value.DisplayToleranceMeters, value.RenderedGapAreaSquareMeters, value.RenderedOverlapAreaSquareMeters);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator PavementGeometryPreview?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.PavementGeometryPreviewFact? value)
        => value is null ? null! : new((global::RoadGuardSystem.DTOs.Projects.PavementLayoutPreview?)value.Plan, value.Slabs?.Select(item => (global::RoadGuardSystem.DTOs.Projects.SlabGeometrySnapshot)item).ToArray()!, value.Warnings, value.DisplayToleranceMeters, value.RenderedGapAreaSquareMeters, value.RenderedOverlapAreaSquareMeters);
}
