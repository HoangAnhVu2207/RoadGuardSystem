using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public sealed class PrimaryProjectManagerPersistenceService : IPrimaryProjectManagerRepository
{
    private const string Operation = "PrimaryProjectManagerReassigned";
    private static readonly string[] AuditFields = ["previous_project_manager_user_id", "current_project_manager_user_id"];
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public PrimaryProjectManagerPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public async Task<PrimaryProjectManagerFacts?> GetFactsAsync(
        Guid projectId,
        Guid replacementProjectManagerUserId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects.AsNoTracking()
            .Where(candidate => candidate.Id == projectId)
            .Select(candidate => new { candidate.Id, candidate.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (project is null)
        {
            return null;
        }

        var replacementIsEligible = await _context.Users.AsNoTracking().AnyAsync(
            user => user.Id == replacementProjectManagerUserId &&
                    user.Status == UserStatus.Active &&
                    user.RoleCode == UserRoleCode.ProjectManager,
            cancellationToken);
        var current = await _context.ProjectMembers.AsNoTracking()
            .Where(member => member.ProjectId == projectId && member.IsPrimary && member.Status == ProjectMemberStatus.Active)
            .Select(member => new { member.Id, member.UserId, member.ValidFrom, member.RowVersion })
            .SingleOrDefaultAsync(cancellationToken);
        return new PrimaryProjectManagerFacts(
            project.Id,
            ProjectExists: true,
            ProjectIsClosed: project.Status == ProjectStatus.Closed,
            replacementIsEligible,
            current?.Id,
            current?.UserId,
            current?.ValidFrom,
            current?.RowVersion);
    }

    public async Task<PrimaryProjectManagerWriteResult> ReassignAsync(
        PrimaryProjectManagerWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty ||
            request.ReplacementProjectManagerUserId == Guid.Empty || request.OperationId == Guid.Empty ||
            request.ExpectedCurrentMembershipRowVersion.Length == 0 || string.IsNullOrWhiteSpace(request.Reason))
        {
            return new(PrimaryProjectManagerWriteStatus.InvalidInput);
        }

        var fingerprint = Fingerprint(request);
        try
        {
            var operation = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                Operation,
                request.OperationId.ToString("N"),
                fingerprint,
                token => PersistAsync(request, token),
                cancellationToken);
            if (operation.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(PrimaryProjectManagerWriteStatus.IdempotentConflict);
            }

            var outcome = JsonSerializer.Deserialize<PrimaryProjectManagerOutcome>(operation.OutcomeJson)
                ?? throw new InvalidOperationException("Stored primary-project-manager outcome is invalid.");
            return new(
                operation.Status == IdempotencyOperationStatus.Executed
                    ? PrimaryProjectManagerWriteStatus.Success
                    : PrimaryProjectManagerWriteStatus.Replayed,
                outcome.PreviousMembershipId,
                outcome.CurrentMembershipId,
                outcome.CurrentRowVersion);
        }
        catch (PrimaryMembershipNotFoundException)
        {
            return new(PrimaryProjectManagerWriteStatus.NotFound);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return new(PrimaryProjectManagerWriteStatus.StaleConcurrency);
        }
    }

    private async Task<(Guid OperationId, string OutcomeJson)> PersistAsync(
        PrimaryProjectManagerWriteRequest request,
        CancellationToken cancellationToken)
    {
        var previous = await _context.ProjectMembers.SingleOrDefaultAsync(
            member => member.ProjectId == request.ProjectId && member.IsPrimary && member.Status == ProjectMemberStatus.Active,
            cancellationToken) ?? throw new PrimaryMembershipNotFoundException();
        if (!previous.RowVersion.SequenceEqual(request.ExpectedCurrentMembershipRowVersion))
        {
            throw new DbUpdateConcurrencyException();
        }

        previous.EndPrimaryAssignment(request.EffectiveFrom.AddDays(-1));
        var current = ProjectMember.CreatePrimaryProjectManager(
            Guid.NewGuid(),
            request.ProjectId,
            request.ReplacementProjectManagerUserId,
            request.EffectiveFrom);
        _context.ProjectMembers.Add(current);
        var occurredAt = request.OccurredAt.ToUniversalTime();
        _context.AuditLogs.Add(AuditLog.Create(
            Guid.NewGuid(), request.ActorUserId, occurredAt, "project_primary_pm_reassigned", "Project",
            request.ProjectId,
            JsonSerializer.Serialize(new { previous_project_manager_user_id = previous.UserId }),
            JsonSerializer.Serialize(new { current_project_manager_user_id = current.UserId }),
            request.Reason.Trim(), "PROJECT_API", request.CorrelationId, AuditFields));
        _context.OutboxMessages.Add(OutboxMessage.Create(
            Guid.NewGuid(), "project.primary_pm_reassigned", occurredAt, request.CorrelationId,
            JsonSerializer.Serialize(new { request.ProjectId, previousMembershipId = previous.Id, currentMembershipId = current.Id })));
        await _context.SaveChangesAsync(cancellationToken);
        var outcome = new PrimaryProjectManagerOutcome(previous.Id, current.Id, current.RowVersion);
        return (current.Id, JsonSerializer.Serialize(outcome));
    }

    private static string Fingerprint(PrimaryProjectManagerWriteRequest request)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            request.ProjectId,
            request.ReplacementProjectManagerUserId,
            request.EffectiveFrom,
            reason = request.Reason.Trim(),
            expectedRowVersion = Convert.ToBase64String(request.ExpectedCurrentMembershipRowVersion)
        });
        return Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
    }

    private sealed record PrimaryProjectManagerOutcome(Guid PreviousMembershipId, Guid CurrentMembershipId, byte[] CurrentRowVersion);
    private sealed class PrimaryMembershipNotFoundException : Exception;
}
