namespace RoadGuardSystem.DTOs.Projects;

public sealed record PavementWidthInterval(decimal FromOffsetMeters, decimal ToOffsetMeters, decimal WidthMeters);
public sealed record PavementLayoutInput(decimal StripWidthMeters, decimal SlabLengthMeters, PavementWidthInterval[] WidthProfile);
public sealed record PlannedSlabCell(int Sequence, int LongitudinalRow, int Strip, decimal FromOffsetMeters,
    decimal ToOffsetMeters, decimal FromLateralMeters, decimal ToLateralMeters, bool IsTerminalResidual,
    bool IsWidthTransitionFragment);
public sealed record PavementLayoutPreview(decimal AnalyticLengthMeters, decimal StripWidthMeters,
    decimal SlabLengthMeters, PlannedSlabCell[] Cells, string[] Warnings);
public sealed record PavementPlanCreateInput(PavementLayoutInput Layout, double DisplayToleranceMeters);
public sealed record SlabGeometrySnapshot(string Key, int? PlannedSequence, GeometryPoint[] Footprint,
    double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,
    Guid? SourceFileId = null, string ProvenanceStatus = "CLAIMED_CUSTOM");
public sealed record AsBuiltSlabInput(string Key, int? PlannedSequence, GeometryPoint[] Footprint,
    double? LengthMeters, double? WidthMeters, string? DimensionUnknownReason, string Source,
    Guid? SourceFileId = null);
public sealed record AsBuiltLayoutInput(Guid SourcePlanId, AsBuiltSlabInput[] Slabs, string Reason);
public sealed record PavementGeometryPreview(PavementLayoutPreview? Plan, SlabGeometrySnapshot[] Slabs,
    string[] Warnings, double DisplayToleranceMeters, double RenderedGapAreaSquareMeters = 0,
    double RenderedOverlapAreaSquareMeters = 0);
