using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Projects;
namespace RoadGuardSystem.Services.Projects;

public sealed class PavementWorkflowService(IPavementWorkflowRepository repository, IProjectScopeGuard guard) : IPavementWorkflowService
{
    public async Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role, PavementWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var read = command.Action is "layout-get" or "manifest" or "page" or "impact-get" or "impact-list";
        var write = command.Action is "plan-create" or "asbuilt-create" or "publish" or "impact-create" or "impact-decide";
        if (!read && !write) return new(400, "validation_error");
        // No FIELD segment-set pin exists in the legacy task schema. Crew access stays denied
        // until H3 supplies its actual task-bound producer, rather than project-wide admission.
        if (command.ActorId == Guid.Empty || command.ProjectId == Guid.Empty ||
            (read ? role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) : role != UserRoleCode.ProjectManager))
            return new(403, "access_forbidden");
        if (write && (string.IsNullOrWhiteSpace(command.Key) || command.Key.Length > 150 || command.Key.Any(c => c is < '!' or > '~')))
            return new(428, "idempotency_key_required");
        if (command.Action is "page" or "publish" or "asbuilt-create" && string.IsNullOrWhiteSpace(command.ExpectedContentHash))
            return new(428, "content_hash_required");
        try
        {
            var result = await repository.ExecuteAsync(role, command with { Input = RoadGuardSystem.DTOs.BoundaryFactMappings.ToFacts(command.Input) },
                async token => await guard.AuthorizeAsync(command.ActorId, role, command.ProjectId, token) is not null,
                (metadata, legacy, input) => PavementFootprintEngine.Planned(metadata?.NativeAlignment is { } native
                    ? NativeAlignment.Create(native) : NativeAlignment.FromLegacy(legacy), input),
                (snapshot, input) => PavementFootprintEngine.AsBuilt(snapshot, input),
                (input, srid) => GeometryEngine.Preview(input, srid),
                (snapshot, query, hash) => GeometryMapPageEngine.Page(snapshot, query.Layer, hash, query.Limit, query.Bbox, query.Cursor),
                cancellationToken);
            return result with { Value = RoadGuardSystem.DTOs.BoundaryFactMappings.ToWire(result.Value) };
        }
        catch (GeometryValidationException e)
        {
            return new(e.Code switch
            {
                "geometry_version_mismatch" or "geometry_layer_unavailable" => 409,
                "geometry_layer_not_found" => 404,
                "geometry_cursor_invalid" or "geometry_page_invalid" => 400,
                _ => 422
            }, e.Code);
        }
        catch (ArgumentException) { return new(422, "pavement_layout_invalid"); }
    }
}
