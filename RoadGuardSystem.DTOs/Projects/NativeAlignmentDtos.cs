namespace RoadGuardSystem.DTOs.Projects;

public sealed record NativeAlignmentPrimitive(string Kind, GeometryPoint Start, GeometryPoint End,
    GeometryPoint? Center = null, double? Radius = null, double? StartAngleRadians = null, double? SweepRadians = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentPrimitiveFact?(NativeAlignmentPrimitive? value)
        => value is null ? null! : new(value.Kind, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)value.Start, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact)value.End, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPointFact?)value.Center, value.Radius, value.StartAngleRadians, value.SweepRadians);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator NativeAlignmentPrimitive?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentPrimitiveFact? value)
        => value is null ? null! : new(value.Kind, (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)value.Start, (global::RoadGuardSystem.DTOs.Projects.GeometryPoint)value.End, (global::RoadGuardSystem.DTOs.Projects.GeometryPoint?)value.Center, value.Radius, value.StartAngleRadians, value.SweepRadians);
}
public sealed record NativeAlignmentInput(Guid CrsProfileRevisionId, NativeAlignmentPrimitive[] Primitives, int SpatialSrid = 0)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentInputFact?(NativeAlignmentInput? value)
        => value is null ? null! : new(value.CrsProfileRevisionId, value.Primitives?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentPrimitiveFact)item).ToArray()!, value.SpatialSrid);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator NativeAlignmentInput?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.NativeAlignmentInputFact? value)
        => value is null ? null! : new(value.CrsProfileRevisionId, value.Primitives?.Select(item => (global::RoadGuardSystem.DTOs.Projects.NativeAlignmentPrimitive)item).ToArray()!, value.SpatialSrid);
}
public sealed record NativeArcRange(double FromOffsetMeters, double ToOffsetMeters, double Radius);
