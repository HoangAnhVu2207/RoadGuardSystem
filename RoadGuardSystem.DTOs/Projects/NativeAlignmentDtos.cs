namespace RoadGuardSystem.DTOs.Projects;

public sealed record NativeAlignmentPrimitive(string Kind, GeometryPoint Start, GeometryPoint End,
    GeometryPoint? Center = null, double? Radius = null, double? StartAngleRadians = null, double? SweepRadians = null);
public sealed record NativeAlignmentInput(Guid CrsProfileRevisionId, NativeAlignmentPrimitive[] Primitives, int SpatialSrid = 0);
public sealed record NativeArcRange(double FromOffsetMeters, double ToOffsetMeters, double Radius);
