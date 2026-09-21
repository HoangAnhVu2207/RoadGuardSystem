using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Surveys;

/// <summary>
/// Persists already-authorized survey planning commands with one durable audit and idempotency outcome.
/// Authorization and HTTP error mapping remain in Services/API.
/// </summary>
public sealed class SurveyPlanningPersistenceService : ISurveyPlanningRepository
{
    private const string PlanOperation = "SurveyPlanCreated";
    private const string RequestOperation = "SurveyRequestCreated";
    private const string PostponementOperation = "SurveyPlanPostponed";
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public SurveyPlanningPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public async Task<SurveyPlanPersistenceResult> CreatePlanAsync(
        SurveyPlanCreationPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty ||
            request.RoadSectionId == Guid.Empty || request.OperationId == Guid.Empty)
        {
            return new(SurveyPlanPersistenceStatus.InvalidInput);
        }

        try
        {
            var outcome = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                PlanOperation,
                request.IdempotencyKey,
                request.RequestFingerprint,
                async operationCancellationToken =>
                {
                    await EnsureProjectAndRoadSectionAsync(
                        request.ProjectId,
                        request.RoadSectionId,
                        operationCancellationToken);
                    var plan = SurveyPlan.Create(
                        Guid.NewGuid(),
                        request.ProjectId,
                        request.RoadSectionId,
                        request.PlannedStartAt,
                        request.PlannedEndAt,
                        request.SurveyType,
                        SurveyPlanStatus.Planned,
                        request.OutputRequirements);
                    _context.SurveyPlans.Add(plan);
                    var now = DateTimeOffset.UtcNow;
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(),
                        request.ActorUserId,
                        now,
                        "survey_plan_created",
                        "SurveyPlan",
                        plan.Id,
                        null,
                        PlanAuditSnapshot(plan),
                        "Survey plan created",
                        "P1-22.persistence",
                        request.CorrelationId,
                        ["projectId", "roadSectionId", "plannedStartAt", "plannedEndAt", "surveyType", "outputRequirementsHash"]));
                    var view = ToPlanView(plan);
                    return (request.OperationId, JsonSerializer.Serialize(view));
                },
                cancellationToken);
            return new(
                outcome.Status == IdempotencyOperationStatus.Executed
                    ? SurveyPlanPersistenceStatus.Success
                    : outcome.Status == IdempotencyOperationStatus.Replayed
                        ? SurveyPlanPersistenceStatus.Replayed
                        : SurveyPlanPersistenceStatus.IdempotentConflict,
                Deserialize<SurveyPlanPersistenceView>(outcome.OutcomeJson));
        }
        catch (ScopeNotFoundException)
        {
            return new(SurveyPlanPersistenceStatus.NotFound);
        }
        catch (ScopeConflictException)
        {
            return new(SurveyPlanPersistenceStatus.ScopeConflict);
        }
        catch (ArgumentException)
        {
            return new(SurveyPlanPersistenceStatus.InvalidInput);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return new(SurveyPlanPersistenceStatus.Conflict);
        }
    }

    public async Task<SurveyRequestPersistenceResult> CreateRequestAsync(
        SurveyRequestCreationPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty ||
            request.RoadSectionId == Guid.Empty || request.OperationId == Guid.Empty)
        {
            return new(SurveyRequestPersistenceStatus.InvalidInput);
        }

        try
        {
            var outcome = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                RequestOperation,
                request.IdempotencyKey,
                request.RequestFingerprint,
                async operationCancellationToken =>
                {
                    await EnsureProjectAndRoadSectionAsync(
                        request.ProjectId,
                        request.RoadSectionId,
                        operationCancellationToken);
                    if (request.SurveyPlanId is { } planId)
                    {
                        var plan = await _context.SurveyPlans.SingleOrDefaultAsync(
                            candidate => candidate.Id == planId,
                            operationCancellationToken) ?? throw new ScopeNotFoundException();
                        if (plan.ProjectId != request.ProjectId || plan.RoadSectionId != request.RoadSectionId ||
                            plan.SurveyType != request.SurveyType)
                        {
                            throw new ScopeConflictException();
                        }
                    }

                    var now = DateTimeOffset.UtcNow;
                    var surveyRequest = SurveyRequest.Create(
                        Guid.NewGuid(),
                        request.ProjectId,
                        request.RoadSectionId,
                        request.SurveyPlanId,
                        request.ActorUserId,
                        request.SurveyType,
                        SurveyRequestStatus.NewAssigned,
                        now,
                        request.DueAt,
                        request.OutputRequirements);
                    _context.SurveyRequests.Add(surveyRequest);
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(),
                        request.ActorUserId,
                        now,
                        "survey_request_created",
                        "SurveyRequest",
                        surveyRequest.Id,
                        null,
                        RequestAuditSnapshot(surveyRequest),
                        "Survey request created before operator assignment",
                        "P1-22.persistence",
                        request.CorrelationId,
                        ["projectId", "roadSectionId", "surveyPlanId", "surveyType", "requestedAt", "dueAt", "outputRequirementsHash"]));
                    var view = ToRequestView(surveyRequest);
                    return (request.OperationId, JsonSerializer.Serialize(view));
                },
                cancellationToken);
            return new(
                outcome.Status == IdempotencyOperationStatus.Executed
                    ? SurveyRequestPersistenceStatus.Success
                    : outcome.Status == IdempotencyOperationStatus.Replayed
                        ? SurveyRequestPersistenceStatus.Replayed
                        : SurveyRequestPersistenceStatus.IdempotentConflict,
                Deserialize<SurveyRequestPersistenceView>(outcome.OutcomeJson));
        }
        catch (ScopeNotFoundException)
        {
            return new(SurveyRequestPersistenceStatus.NotFound);
        }
        catch (ScopeConflictException)
        {
            return new(SurveyRequestPersistenceStatus.ScopeConflict);
        }
        catch (ArgumentException)
        {
            return new(SurveyRequestPersistenceStatus.InvalidInput);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return new(SurveyRequestPersistenceStatus.Conflict);
        }
    }

    public async Task<SurveyPlanPostponementPersistenceResult> PostponePlanAsync(
        SurveyPlanPostponementPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty ||
            request.SurveyPlanId == Guid.Empty || request.OperationId == Guid.Empty)
        {
            return new(SurveyPlanPostponementPersistenceStatus.InvalidInput);
        }

        try
        {
            var outcome = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                PostponementOperation,
                request.IdempotencyKey,
                request.RequestFingerprint,
                async operationCancellationToken =>
                {
                    var plan = await _context.SurveyPlans.SingleOrDefaultAsync(
                        candidate => candidate.Id == request.SurveyPlanId,
                        operationCancellationToken) ?? throw new ScopeNotFoundException();
                    if (plan.ProjectId != request.ProjectId)
                    {
                        throw new ScopeConflictException();
                    }

                    var now = DateTimeOffset.UtcNow;
                    plan.Postpone(request.NewPlannedStartAt);
                    var postponement = SurveyPlanPostponement.Create(
                        Guid.NewGuid(),
                        plan.Id,
                        now,
                        request.Reason,
                        request.NewPlannedStartAt);
                    _context.SurveyPlanPostponements.Add(postponement);
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(),
                        request.ActorUserId,
                        now,
                        "survey_plan_postponed",
                        "SurveyPlan",
                        plan.Id,
                        null,
                        JsonSerializer.Serialize(new
                        {
                            planId = plan.Id,
                            status = plan.Status,
                            newPlannedStartAt = request.NewPlannedStartAt,
                            reasonHash = Hash(request.Reason)
                        }),
                        "Survey plan postponed",
                        "P1-22.persistence",
                        request.CorrelationId,
                        ["planId", "status", "newPlannedStartAt", "reasonHash"]));
                    var view = new SurveyPlanPostponementPersistenceView(
                        plan.Id,
                        plan.Status,
                        request.NewPlannedStartAt?.ToUniversalTime(),
                        postponement.Id);
                    return (request.OperationId, JsonSerializer.Serialize(view));
                },
                cancellationToken);
            return new(
                outcome.Status == IdempotencyOperationStatus.Executed
                    ? SurveyPlanPostponementPersistenceStatus.Success
                    : outcome.Status == IdempotencyOperationStatus.Replayed
                        ? SurveyPlanPostponementPersistenceStatus.Replayed
                        : SurveyPlanPostponementPersistenceStatus.IdempotentConflict,
                Deserialize<SurveyPlanPostponementPersistenceView>(outcome.OutcomeJson));
        }
        catch (ScopeNotFoundException)
        {
            return new(SurveyPlanPostponementPersistenceStatus.NotFound);
        }
        catch (ScopeConflictException)
        {
            return new(SurveyPlanPostponementPersistenceStatus.ScopeConflict);
        }
        catch (ArgumentException)
        {
            return new(SurveyPlanPostponementPersistenceStatus.InvalidInput);
        }
        catch (InvalidOperationException)
        {
            return new(SurveyPlanPostponementPersistenceStatus.Conflict);
        }
    }

    private async Task EnsureProjectAndRoadSectionAsync(
        Guid projectId,
        Guid roadSectionId,
        CancellationToken cancellationToken)
    {
        var scope = await (
                from project in _context.Projects.AsNoTracking()
                join roadSection in _context.RoadSections.AsNoTracking()
                    on project.Id equals roadSection.ProjectId
                where project.Id == projectId && roadSection.Id == roadSectionId
                select new { project.Id })
            .SingleOrDefaultAsync(cancellationToken);
        if (scope is null)
        {
            var projectExists = await _context.Projects.AsNoTracking()
                .AnyAsync(project => project.Id == projectId, cancellationToken);
            throw projectExists ? new ScopeConflictException() : new ScopeNotFoundException();
        }
    }

    private static SurveyPlanPersistenceView ToPlanView(SurveyPlan plan) => new(
        plan.Id,
        plan.ProjectId,
        plan.RoadSectionId,
        plan.PlannedStartAt,
        plan.PlannedEndAt,
        plan.SurveyType,
        plan.Status,
        plan.OutputRequirements);

    private static SurveyRequestPersistenceView ToRequestView(SurveyRequest request) => new(
        request.Id,
        request.ProjectId,
        request.RoadSectionId,
        request.SurveyPlanId,
        request.RequestedByUserId,
        request.SurveyType,
        request.Status,
        request.RequestedAt,
        request.DueAt,
        request.OutputRequirements);

    private static string PlanAuditSnapshot(SurveyPlan plan) => JsonSerializer.Serialize(new
    {
        projectId = plan.ProjectId,
        roadSectionId = plan.RoadSectionId,
        plannedStartAt = plan.PlannedStartAt,
        plannedEndAt = plan.PlannedEndAt,
        surveyType = plan.SurveyType,
        outputRequirementsHash = Hash(plan.OutputRequirements)
    });

    private static string RequestAuditSnapshot(SurveyRequest request) => JsonSerializer.Serialize(new
    {
        projectId = request.ProjectId,
        roadSectionId = request.RoadSectionId,
        surveyPlanId = request.SurveyPlanId,
        surveyType = request.SurveyType,
        requestedAt = request.RequestedAt,
        dueAt = request.DueAt,
        outputRequirementsHash = Hash(request.OutputRequirements)
    });

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Stored survey planning outcome is invalid.");

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private sealed class ScopeNotFoundException : Exception;

    private sealed class ScopeConflictException : Exception;
}
