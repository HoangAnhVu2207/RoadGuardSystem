using System.Globalization;
using System.Text;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public sealed class ProjectListService(IProjectListRepository repository, TimeProvider clock) : IProjectListService
{
    public async Task<ProjectListResult> ListAsync(
        Guid actorUserId, UserRoleCode actorRole, string? search, string? status,
        string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || actorRole == UserRoleCode.Unknown)
            return new(ProjectListStatus.Unauthorized);
        if (actorRole is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager or UserRoleCode.DroneOperator or UserRoleCode.RepairCrew))
            return new(ProjectListStatus.Forbidden);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        ProjectStatus? projectStatus = status?.Trim().ToUpperInvariant() switch
        {
            null or "" => null,
            "PLANNING" => ProjectStatus.Planning,
            "ACTIVE" => ProjectStatus.Active,
            "CLOSED" => ProjectStatus.Closed,
            _ => (ProjectStatus)0
        };
        if (limit is < 1 or > 100 || search?.Length > 255 || projectStatus == (ProjectStatus)0 ||
            !TryDecodeCursor(cursor, out var afterCreatedAt, out var afterId))
            return new(ProjectListStatus.InvalidInput);

        var asOf = clock.GetUtcNow();
        var rows = await repository.ListAsync(actorUserId, actorRole, DateOnly.FromDateTime(asOf.UtcDateTime),
            search, projectStatus, afterCreatedAt, afterId, limit + 1, cancellationToken);
        var items = rows.Take(limit).Select(row => new ProjectListItemResponseDto(
            row.ProjectId, row.ProjectCode, row.Name, row.Status.ToString().ToUpperInvariant(),
            row.PrimaryProjectManagerUserId, row.PrimaryProjectManagerName)).ToArray();
        var next = rows.Count > limit
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{rows[limit - 1].CreatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}|{rows[limit - 1].ProjectId:D}"))
            : null;
        return new(ProjectListStatus.Success, new(items, next, asOf));
    }

    private static bool TryDecodeCursor(string? cursor, out DateTimeOffset? createdAt, out Guid? id)
    {
        createdAt = null;
        id = null;
        if (string.IsNullOrWhiteSpace(cursor)) return true;
        if (cursor.Length > 256) return false;
        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split('|');
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                !Guid.TryParseExact(parts[1], "D", out var parsedId) || parsedId == Guid.Empty)
                return false;
            createdAt = new DateTimeOffset(ticks, TimeSpan.Zero);
            id = parsedId;
            return true;
        }
        catch (FormatException) { return false; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}
