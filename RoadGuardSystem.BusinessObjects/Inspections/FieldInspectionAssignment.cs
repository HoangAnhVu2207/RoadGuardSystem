using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionAssignment
{
    private FieldInspectionAssignment()
    {
    }

    public Guid Id { get; private set; }

    public Guid FieldInspectionTaskId { get; private set; }

    public Guid AssignedToUserId { get; private set; }

    public Guid AssignedByUserId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public FieldInspectionAssignmentStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public static FieldInspectionAssignment Create(
        Guid id,
        Guid fieldInspectionTaskId,
        Guid assignedToUserId,
        Guid assignedByUserId,
        DateTimeOffset assignedAt,
        DateTimeOffset? endedAt,
        FieldInspectionAssignmentStatus status,
        string? reason)
    {
        if (id == Guid.Empty || fieldInspectionTaskId == Guid.Empty || assignedToUserId == Guid.Empty || assignedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Assignment and actor ids must not be empty.");
        }

        if (!Enum.IsDefined(status) || status == FieldInspectionAssignmentStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Assignment status must be specified.");
        }

        if (endedAt < assignedAt)
        {
            throw new ArgumentException("Assignment end time must not precede assignment time.", nameof(endedAt));
        }

        var normalizedReason = NormalizeOptional(reason, nameof(reason), 1_000);
        if (status == FieldInspectionAssignmentStatus.Active && (endedAt is not null || normalizedReason is not null))
        {
            throw new ArgumentException("An active assignment cannot have an end time or reason.");
        }

        if (status != FieldInspectionAssignmentStatus.Active && (endedAt is null || normalizedReason is null))
        {
            throw new ArgumentException("Ended or rejected assignments require an end time and reason.");
        }

        return new FieldInspectionAssignment
        {
            Id = id,
            FieldInspectionTaskId = fieldInspectionTaskId,
            AssignedToUserId = assignedToUserId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = assignedAt.ToUniversalTime(),
            EndedAt = endedAt?.ToUniversalTime(),
            Status = status,
            Reason = normalizedReason
        };
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }
}
