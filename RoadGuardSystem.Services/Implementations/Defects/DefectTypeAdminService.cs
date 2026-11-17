using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Repositories.Defects;

namespace RoadGuardSystem.Services.Implementations.Defects;

public sealed record DefectTypeAdminResponse(int Status, string? Code = null,
    DefectTypeAdminDto[]? Items = null, DefectTypeAdminDto? Item = null);

public sealed class DefectTypeAdminService(IDefectTypeAdminRepository repository)
{
    public async Task<DefectTypeAdminResponse> ListAsync(Guid actor, UserRoleCode role, CancellationToken token)
        => role == UserRoleCode.Supervisor ? Map(await repository.ListAsync(actor, token)) : new(403, "access_forbidden");

    public async Task<DefectTypeAdminResponse> WriteAsync(Guid actor, UserRoleCode role, string? code, string? name,
        string? description, bool isActive, string? reason, string? key, string? etag, bool creating, CancellationToken token)
    {
        if (role != UserRoleCode.Supervisor) return new(403, "access_forbidden");
        if (string.IsNullOrWhiteSpace(key) || (!creating && string.IsNullOrWhiteSpace(etag)))
            return new(428, "precondition_required");

        var normalized = code?.Trim().ToUpperInvariant();
        name = name?.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        reason = reason?.Trim();
        if (normalized is not { Length: >= 1 and <= 80 } || normalized.Any(character =>
                character is not (>= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-')) ||
            name is not { Length: >= 1 and <= 150 } || description is { Length: > 2000 } ||
            reason is not { Length: >= 1 and <= 1000 } || key.Length > 150 || key.Any(c => c is < '!' or > '~') ||
            (!creating && (etag is not { Length: 66 } || etag[0] != '"' || etag[^1] != '"' ||
                etag[1..^1].Any(c => c is not (>= 'a' and <= 'f' or >= '0' and <= '9')))))
            return new(400, "validation_error");

        return Map(await repository.WriteAsync(actor, new(normalized, name, description, isActive, reason,
            creating ? null : etag![1..^1], key), token));
    }

    private static DefectTypeAdminResponse Map(DefectTypeAdminResult result)
        => new(result.Status, result.Code, result.Items?.Select(ToDto).ToArray(), result.Item is null ? null : ToDto(result.Item));

    private static DefectTypeAdminDto ToDto(DefectTypeAdminFact row)
        => new(row.Code, row.Name, row.Description, row.IsActive, row.Version);
}