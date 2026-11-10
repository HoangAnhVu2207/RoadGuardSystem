using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Services.Projects;

public sealed class GeometryWorkflowService(IGeometryWorkflowRepository repository, IProjectScopeGuard guard) : IGeometryWorkflowService
{
    public async Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role, GeometryWorkflowCommand command, CancellationToken cancellationToken)
    {
        var c = command with { Role = role }; var ct = cancellationToken;
        var read = c.Action is "submission-get" or "draft-get" or "draft-readiness" or "draft-preview" or "set-preview" or "geometry-get" or "set-get" or "set-list" or "package" or "profile-get" or "profile-list" or "system-list";
        var operatorPackage = c.Action == "package" && role == UserRoleCode.DroneOperator;
        if (c.ActorId == Guid.Empty || (c.Action is "confirm" or "reject" ? role != UserRoleCode.Supervisor : read ? !operatorPackage && role is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager) : role != UserRoleCode.ProjectManager)) return new(403, "access_forbidden");
        if (await guard.AuthorizeAsync(c.ActorId, role, c.ProjectId, ct) is null) return new(403, "access_forbidden");
        if (c.Action == "set-list" && c.Input is SegmentSetListQuery q && (q.PageSize is < 1 or > 100 || q.Status is not (null or "DRAFT" or "PUBLISHED" or "SUPERSEDED"))) return new(400, "validation_error");
        if (operatorPackage && (c.RouteVersionId is not Guid routeId || c.SetId is not Guid setId || !await repository.CanReadAssignedGeometryAsync(c.ActorId, c.ProjectId, routeId, setId, ct))) return new(403, "access_forbidden");
        var write = c.Action is "draft-submit" or "draft-create" or "draft-edit" or "confirm" or "reject" or "set-create" or "set-edit" or "publish" or "profile-create" or "system-create";
        if (write && (string.IsNullOrWhiteSpace(c.Key) || c.Key.Length > 200)) return new(428, "validation_error");
        if (c.Action is "draft-submit" or "draft-edit" or "confirm" or "reject" or "set-edit" or "publish" && string.IsNullOrWhiteSpace(c.ExpectedVersion)) return new(428, "validation_error");
        try
        {
            if (c.Action == "profile-create") CrsProfileEngine.Validate((RoadGuardSystem.DTOs.Projects.CrsProfileInput)c.Input!);
            var input = c.Input is RoadGuardSystem.DTOs.Projects.GeometryRejectInput rejection
                ? rejection.Reason?.Trim()
                : RoadGuardSystem.DTOs.BoundaryFactMappings.ToFacts(c.Input);
            if (c.Action == "reject" && (input is not string reason || reason.Length is < 1 or > 2000))
                return new(422, "road_geometry_rejection_reason_required");
            var result = await repository.ExecuteAsync(c with { Input = input },
                (input, srid) => GeometryEngine.Preview(input, srid),
                (id, line, origin, definition) => GeometryEngine.Segments(id, line, origin, definition), ct);
            result = result with { Value = RoadGuardSystem.DTOs.BoundaryFactMappings.ToWire(result.Value) };
            if (result.Value is RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometrySubmissionViewFact submission)
                result = result with
                {
                    Value = new RoadGuardSystem.DTOs.Projects.GeometrySubmissionView(
                    submission.ProjectId, submission.DraftId, submission.RoadSectionId, submission.Status, submission.Version,
                    submission.SubmittedContentVersion, submission.SubmittedBy, submission.SubmittedAt, submission.ConfirmedRouteVersionId,
                        Review(submission.CurrentReview), Review(submission.PreviousRejection))
                };
            if (result.Value is SegmentSetPage page)
                result = result with { Value = new RoadGuardSystem.DTOs.Projects.SegmentSetPageDto(page.Items.Select(x => new RoadGuardSystem.DTOs.Projects.SegmentSetSummaryDto(x.Id, x.RouteVersionId, x.Status, x.Version)).ToArray(), page.NextCursor) };
            if (c.Action == "draft-readiness" && result.Value is RoadGuardSystem.DTOs.Projects.GeometryDraftInput draft)
                return result with { Value = CrsProfileEngine.Readiness(draft, draft.ResolvedCrsProfile) };
            return result;
        }
        catch (GeometryValidationException e) { return new(422, e.Code); }
    }
    private static RoadGuardSystem.DTOs.Projects.GeometryReviewRoundView? Review(
        RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryReviewRoundViewFact? round)
        => round is null ? null : new(round.Number, round.ContentVersion, round.Outcome, round.SubmittedBy,
            round.SubmittedAt, round.DecidedBy, round.DecidedAt, round.RejectionReason);
}
