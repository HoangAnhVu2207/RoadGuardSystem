namespace RoadGuardSystem.Repositories.Defects;

public sealed record DefectTypeAdminFact(string Code, string Name, string? Description, bool IsActive, string Version);
public sealed record DefectTypeAdminWrite(string Code, string Name, string? Description, bool IsActive,
    string Reason, string? ExpectedVersion, string Key);
public sealed record DefectTypeAdminResult(int Status, string? Code = null,
    DefectTypeAdminFact[]? Items = null, DefectTypeAdminFact? Item = null);

public interface IDefectTypeAdminRepository
{
    Task<DefectTypeAdminResult> ListAsync(Guid actor, CancellationToken token);
    Task<DefectTypeAdminResult> WriteAsync(Guid actor, DefectTypeAdminWrite command, CancellationToken token);
}