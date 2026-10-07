namespace RoadGuardSystem.DTOs.Reports;

public sealed record OwnReportEvidenceDto(Guid Id, Guid FileId, string FileVersion, Guid? SupplementId,
    DateTimeOffset? CapturedAt, string LocationSource, ReportEvidenceLocationDto? Location)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportEvidenceFact?(OwnReportEvidenceDto? value)
        => value is null ? null! : new(value.Id, value.FileId, value.FileVersion, value.SupplementId, value.CapturedAt, value.LocationSource, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.ReportEvidenceLocationFact?)value.Location);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator OwnReportEvidenceDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportEvidenceFact? value)
        => value is null ? null! : new(value.Id, value.FileId, value.FileVersion, value.SupplementId, value.CapturedAt, value.LocationSource, (global::RoadGuardSystem.DTOs.Reports.ReportEvidenceLocationDto?)value.Location);
}
public sealed record OwnReportSupplementDto(Guid Id, string Description, DateTimeOffset CreatedAt)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportSupplementFact?(OwnReportSupplementDto? value)
        => value is null ? null! : new(value.Id, value.Description, value.CreatedAt);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator OwnReportSupplementDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportSupplementFact? value)
        => value is null ? null! : new(value.Id, value.Description, value.CreatedAt);
}
public sealed record OwnReportUpdateDto(Guid Id, string Summary, DateTimeOffset PublishedAt, IReadOnlyList<Guid> EvidenceIds)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportUpdateFact?(OwnReportUpdateDto? value)
        => value is null ? null! : new(value.Id, value.Summary, value.PublishedAt, value.EvidenceIds);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator OwnReportUpdateDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportUpdateFact? value)
        => value is null ? null! : new(value.Id, value.Summary, value.PublishedAt, value.EvidenceIds);
}
public sealed record OwnReportDto(Guid Id, string Description, DateTimeOffset CreatedAt, string Version,
    IReadOnlyList<OwnReportEvidenceDto> Evidence, string RoutingStatus,
    IReadOnlyList<OwnReportUpdateDto> PublicUpdates, IReadOnlyList<OwnReportSupplementDto> Supplements)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportFact?(OwnReportDto? value)
        => value is null ? null! : new(value.Id, value.Description, value.CreatedAt, value.Version, value.Evidence?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportEvidenceFact)item).ToArray()!, value.RoutingStatus, value.PublicUpdates?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportUpdateFact)item).ToArray()!, value.Supplements?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportSupplementFact)item).ToArray()!);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator OwnReportDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportFact? value)
        => value is null ? null! : new(value.Id, value.Description, value.CreatedAt, value.Version, value.Evidence?.Select(item => (global::RoadGuardSystem.DTOs.Reports.OwnReportEvidenceDto)item).ToArray()!, value.RoutingStatus, value.PublicUpdates?.Select(item => (global::RoadGuardSystem.DTOs.Reports.OwnReportUpdateDto)item).ToArray()!, value.Supplements?.Select(item => (global::RoadGuardSystem.DTOs.Reports.OwnReportSupplementDto)item).ToArray()!);
}
public sealed record OwnReportPageDto(IReadOnlyList<OwnReportDto> Items, string? NextCursor)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportPageFact?(OwnReportPageDto? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportFact)item).ToArray()!, value.NextCursor);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator OwnReportPageDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.OwnReportPageFact? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.DTOs.Reports.OwnReportDto)item).ToArray()!, value.NextCursor);
}
