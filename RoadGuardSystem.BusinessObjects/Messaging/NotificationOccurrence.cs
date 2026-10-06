using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RoadGuardSystem.BusinessObjects.Messaging;

public sealed record NotificationLeaseAttempt(Guid Fence, int AttemptNumber, DateTimeOffset AcquiredAtUtc, DateTimeOffset ExpiresAtUtc);

public sealed class NotificationOccurrence
{
    private NotificationOccurrence() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid SourceEventId { get; private set; }
    public string OccurrenceKey { get; private set; } = "";
    public string ContentFingerprint { get; private set; } = "";
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public Guid? LeaseFence { get; private set; }
    public DateTimeOffset? LeaseAcquiredAtUtc { get; private set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public Guid? CompletionFence { get; private set; }
    private readonly List<NotificationLeaseAttempt> _leaseHistory = [];
    public IReadOnlyCollection<NotificationLeaseAttempt> LeaseHistory => _leaseHistory.AsReadOnly();
    public static NotificationOccurrence Create(Guid id, NotificationEventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.Kind == NotificationEventKind.WeeklyReviewPending)
            throw new ArgumentException("A weekly occurrence requires its calendar period.", nameof(envelope));
        return Build(id, envelope, null);
    }
    public static NotificationOccurrence CreateWeekly(Guid id, NotificationEventEnvelope envelope, DateTimeOffset scheduledAtUtc)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.Kind != NotificationEventKind.WeeklyReviewPending)
            throw new ArgumentException("Only the weekly review event has this calendar identity.", nameof(envelope));
        NotificationCalendarPolicy.ValidateWeeklyPeriod(scheduledAtUtc);
        return Build(id, envelope, scheduledAtUtc.ToUniversalTime());
    }
    private static NotificationOccurrence Build(Guid id, NotificationEventEnvelope envelope, DateTimeOffset? scheduled)
    {
        NotificationDomainGuard.Id(id);
        var canonical = string.Join("|", "notification-occurrence.v1", envelope.ProjectId.ToString("N"),
            envelope.MessageType, ((byte)envelope.SourceKind).ToString(CultureInfo.InvariantCulture),
            envelope.SourceId.ToString("N"), scheduled.HasValue ? "calendar" : envelope.OriginEventId.ToString("N"),
            scheduled?.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture) ?? "event");
        var content = string.Join("|", canonical, envelope.SourceRevisionId?.ToString("N") ?? "none",
            envelope.ResponsibleUserId?.ToString("N") ?? "resolve-current",
            scheduled?.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture)
                ?? envelope.OccurredAtUtc.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture));
        return new NotificationOccurrence { Id = id, ProjectId = envelope.ProjectId, SourceEventId = envelope.EventId,
            ScheduledAtUtc = scheduled, OccurrenceKey = Hash(canonical), ContentFingerprint = Hash(content) };
    }
    private static string Hash(string canonical) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    public void AcquireLease(Guid fence, DateTimeOffset now, TimeSpan duration)
    {
        NotificationDomainGuard.Id(fence); NotificationDomainGuard.Timestamp(now);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        var at = now.ToUniversalTime();
        if (CompletedAtUtc.HasValue) throw new InvalidOperationException("A completed occurrence cannot be leased again.");
        if (LeaseExpiresAtUtc > at) throw new InvalidOperationException("The occurrence already has an unexpired lease.");
        if (_leaseHistory.Any(x => x.Fence == fence)) throw new InvalidOperationException("Every lease acquisition requires a new fence.");
        if (_leaseHistory.Count > 0 && at < _leaseHistory[^1].AcquiredAtUtc)
            throw new ArgumentException("Lease history cannot be backdated.", nameof(now));
        var expires = at.Add(duration);
        AttemptCount = checked(AttemptCount + 1); LeaseFence = fence; LeaseAcquiredAtUtc = at; LeaseExpiresAtUtc = expires;
        _leaseHistory.Add(new NotificationLeaseAttempt(fence, AttemptCount, at, expires));
    }
    public void Complete(Guid fence, DateTimeOffset now)
    {
        NotificationDomainGuard.Id(fence); NotificationDomainGuard.Timestamp(now);
        if (CompletedAtUtc.HasValue)
        {
            if (CompletionFence == fence) return;
            throw new InvalidOperationException("Completion belongs to another lease.");
        }
        var at = now.ToUniversalTime();
        if (LeaseFence != fence || LeaseAcquiredAtUtc is null || at < LeaseAcquiredAtUtc || LeaseExpiresAtUtc is null || at >= LeaseExpiresAtUtc)
            throw new InvalidOperationException("A current unexpired lease fence is required.");
        CompletedAtUtc = at; CompletionFence = fence; LeaseFence = null; LeaseAcquiredAtUtc = null; LeaseExpiresAtUtc = null;
    }
}
